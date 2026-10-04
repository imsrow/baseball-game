using System;
using System.Collections.Generic;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Randomness;
using BaseballSim.Engine.State;
using BaseballSim.Engine.Teams;

namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// 경기 시작 구성으로 초기 GameState 생성
    /// </summary>
    public static class GameStateFactory
    {
        public static GameState Create(GameSetup setup, LeagueConfig config, ulong seed)
        {
            Validate(setup.Away, config, "원정");
            Validate(setup.Home, config, "홈");
            return new GameState
            {
                GameId = setup.GameId,
                Away = CreateTeam(setup.Away),
                Home = CreateTeam(setup.Home),
                Random = new Pcg32Random(seed),
            };
        }

        private static TeamGameState CreateTeam(GameTeamSetup setup)
        {
            var team = new TeamGameState
            {
                TeamId = setup.Team.Id,
                CurrentPitcherId = setup.StartingPitcherId,
                AvailableBullpen = new List<int>(setup.Bullpen),
                Bench = new List<int>(setup.Bench),
                CloserId = setup.CloserId,
                SetupIds = new List<int>(setup.SetupIds),
            };
            foreach (LineupSlot slot in setup.Lineup)
            {
                team.Lineup.Add(slot.Clone());
            }

            team.Pitchers.Add(new PitcherGameState { PlayerId = setup.StartingPitcherId, IsStarter = true });
            return team;
        }

        private static void Validate(GameTeamSetup setup, LeagueConfig config, string label)
        {
            if (setup.Lineup.Count != config.Rules.LineupSize)
            {
                throw new ArgumentException(label + " 타순은 " + config.Rules.LineupSize + "명이어야 합니다.");
            }

            var seen = new HashSet<Position>();
            foreach (LineupSlot slot in setup.Lineup)
            {
                if (slot.Position == Position.Pitcher)
                {
                    throw new ArgumentException(label + " 타순에 투수가 있습니다 (지명타자 제도).");
                }

                if (!seen.Add(slot.Position))
                {
                    throw new ArgumentException(label + " 타순에 포지션 중복: " + PositionInfo.Abbreviation(slot.Position));
                }

                if (setup.Team.FindPlayer(slot.PlayerId) == null)
                {
                    throw new ArgumentException(label + " 타순 선수가 로스터에 없습니다: " + slot.PlayerId);
                }
            }

            Player starter = setup.Team.FindPlayer(setup.StartingPitcherId);
            if (starter == null || !starter.IsPitcher)
            {
                throw new ArgumentException(label + " 선발투수가 올바르지 않습니다.");
            }
        }
    }
}
