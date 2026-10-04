using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Randomness;
using BaseballSim.Engine.Simulation;
using BaseballSim.Engine.State;
using BaseballSim.Engine.Stats;
using BaseballSim.Engine.Teams;

namespace BaseballSim.Harness
{
    /// <summary>
    /// 시드 하나로 리그 생성 → 일정 생성 → 전 경기 순수 시뮬레이션 → 이벤트 로그로 기록 집계
    /// </summary>
    public static class SeasonRunner
    {
        // 리그 생성 시드 파생용 인덱스 (경기 시드와 겹치지 않게)
        private const ulong LeagueSeedIndex = 0xFFFF_FFFFUL;

        public static SeasonResult Run(LeagueConfig config, ulong seed)
        {
            var watch = Stopwatch.StartNew();
            SeasonConfig season = config.Season;
            List<LeagueTeam> teams = new LeagueGenerator(SeedMixer.Derive(seed, LeagueSeedIndex)).Generate(season);
            var schedule = ScheduleGenerator.Generate(season.TeamCount, season.GamesPerOpponent, season.SeriesLength);
            var stats = new StatsAggregator();

            int gameId = 0;
            int extraInnings = 0;
            foreach (var day in schedule)
            {
                foreach ((int home, int away) in day)
                {
                    gameId++;
                    GameSetup setup = BuildSetup(gameId, teams[away], teams[home], season);
                    GameState final = GameSimulator.Play(setup, config, SeedMixer.Derive(seed, (ulong)gameId), stats);
                    RecordRelievers(teams[away], final.Away);
                    RecordRelievers(teams[home], final.Home);
                    if (final.Inning > config.Rules.InningsPerGame)
                    {
                        extraInnings++;
                    }
                }
            }

            return new SeasonResult
            {
                Seed = seed,
                Games = gameId,
                ExtraInningGames = extraInnings,
                Stats = stats,
                ElapsedSeconds = watch.Elapsed.TotalSeconds,
            };
        }

        private static GameSetup BuildSetup(int gameId, LeagueTeam away, LeagueTeam home, SeasonConfig season)
        {
            return new GameSetup
            {
                GameId = gameId,
                Away = TeamSetup(away, season),
                Home = TeamSetup(home, season),
            };
        }

        private static GameTeamSetup TeamSetup(LeagueTeam team, SeasonConfig season)
        {
            int starter = team.Rotation[team.GamesPlayed % season.RotationSize];
            team.GamesPlayed++;
            return new GameTeamSetup
            {
                Team = team.Team,
                Lineup = team.Lineup,
                StartingPitcherId = starter,
                Bullpen = BullpenOrder(team),
                Bench = team.Bench,
            };
        }

        /// <summary>
        /// 불펜 기용 순서: 최근에 덜 던진 투수 먼저, 같으면 기본 순서(낮은 등급 먼저).
        /// (A단계 하네스용 간이 휴식 모델. 정식 불펜 운용은 B단계 ManagerAI)
        /// </summary>
        private static List<int> BullpenOrder(LeagueTeam team)
        {
            return team.Bullpen
                .Select((id, index) => (id, index))
                .OrderBy(x => team.LastRelieverAppearance.TryGetValue(x.id, out int last) ? last : -1)
                .ThenBy(x => x.index)
                .Select(x => x.id)
                .ToList();
        }

        private static void RecordRelievers(LeagueTeam team, TeamGameState state)
        {
            foreach (PitcherGameState pitcher in state.Pitchers)
            {
                if (!pitcher.IsStarter)
                {
                    team.LastRelieverAppearance[pitcher.PlayerId] = team.GamesPlayed;
                }
            }
        }
    }
}
