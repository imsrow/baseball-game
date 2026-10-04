using System.Collections.Generic;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Ratings;
using BaseballSim.Engine.Teams;

namespace BaseballProto.Core
{
    /// <summary>
    /// 프로토타입용 가상 팀 두 개로 경기 구성. 능력치는 평균 근처에서 조금씩 다르게.
    /// </summary>
    public static class MatchFactory
    {
        private const int RatingSpread = 12;
        private const int StarterStamina = 70;
        private const int RelieverStamina = 35;
        private const int FielderProficiency = 60;
        private const int RelieverCount = 6;

        private static readonly Position[] LineupPositions =
        {
            Position.CenterField, Position.SecondBase, Position.FirstBase, Position.DesignatedHitter, Position.ThirdBase,
            Position.RightField, Position.LeftField, Position.Catcher, Position.Shortstop,
        };

        private static readonly Position[] BenchPositions = { Position.Catcher, Position.Shortstop, Position.CenterField };

        // 사람이 던져볼 수 있게 선발은 구종을 넉넉히
        private static readonly PitchType[] StarterPitches =
        {
            PitchType.FourSeam, PitchType.Slider, PitchType.Curveball, PitchType.Changeup, PitchType.Splitter,
            PitchType.Sinker, PitchType.Cutter,
        };

        public static GameSetup Create(int gameId, int seed)
        {
            var random = new System.Random(seed);
            return new GameSetup
            {
                GameId = gameId,
                Away = CreateTeam(1, 1000, "Harbor Gulls", "GUL", random),
                Home = CreateTeam(2, 2000, "Ridge Foxes", "FOX", random),
            };
        }

        private static GameTeamSetup CreateTeam(int teamId, int idBase, string name, string shortName, System.Random random)
        {
            var team = new Team { Id = teamId, Name = name };
            var setup = new GameTeamSetup { Team = team };
            for (int i = 0; i < LineupPositions.Length; i++)
            {
                Player p = Hitter(idBase + i, shortName + " " + (i + 1), LineupPositions[i], (BatSide)(i % 3), random);
                team.Roster.Add(p);
                setup.Lineup.Add(new LineupSlot(p.Id, LineupPositions[i]));
            }

            Player starter = Pitcher(idBase + 50, shortName + " SP", StarterStamina, StarterPitches, Hand.Right, random);
            team.Roster.Add(starter);
            setup.StartingPitcherId = starter.Id;
            for (int i = 0; i < RelieverCount; i++)
            {
                Hand throws = i % 2 == 0 ? Hand.Right : Hand.Left;
                Player reliever = Pitcher(idBase + 60 + i, shortName + " RP" + (i + 1), RelieverStamina,
                    new[] { PitchType.FourSeam, PitchType.Slider, PitchType.Changeup }, throws, random);
                team.Roster.Add(reliever);
                setup.Bullpen.Add(reliever.Id);
            }

            setup.CloserId = setup.Bullpen[RelieverCount - 1];
            setup.SetupIds.Add(setup.Bullpen[RelieverCount - 2]);

            foreach (Position position in BenchPositions)
            {
                Player p = Hitter(idBase + 20 + setup.Bench.Count, shortName + " B" + (setup.Bench.Count + 1), position,
                    BatSide.Right, random);
                team.Roster.Add(p);
                setup.Bench.Add(p.Id);
            }

            return setup;
        }

        private static Player Hitter(int id, string name, Position position, BatSide bats, System.Random random)
        {
            var p = new Player
            {
                Id = id,
                Name = name,
                PrimaryPosition = position,
                Bats = bats,
                Batting = new BatterRatings
                {
                    Contact = Vary(random),
                    Power = Vary(random),
                    Eye = Vary(random),
                    AvoidK = Vary(random),
                    Gap = Vary(random),
                    Speed = Vary(random),
                },
            };
            if (position != Position.DesignatedHitter)
            {
                p.Fielding.SetProficiency(position, FielderProficiency);
            }

            return p;
        }

        private static Player Pitcher(int id, string name, int stamina, IList<PitchType> pitches, Hand throws,
            System.Random random)
        {
            var repertoire = new List<PitchRating>();
            for (int i = 0; i < pitches.Count; i++)
            {
                // 첫 구종(포심) 비중을 가장 높게
                double usage = i == 0 ? 0.45 : 0.55 / (pitches.Count - 1);
                repertoire.Add(new PitchRating(pitches[i], Vary(random), usage));
            }

            var p = new Player
            {
                Id = id,
                Name = name,
                PrimaryPosition = Position.Pitcher,
                Throws = throws,
                Pitching = new PitcherRatings
                {
                    Velocity = Vary(random),
                    Control = Vary(random),
                    Stamina = stamina,
                    Repertoire = repertoire,
                },
            };
            p.Fielding.SetProficiency(Position.Pitcher, FielderProficiency);
            return p;
        }

        private static int Vary(System.Random random)
        {
            return ScoutScale.Average + random.Next(-RatingSpread, RatingSpread + 1);
        }
    }
}
