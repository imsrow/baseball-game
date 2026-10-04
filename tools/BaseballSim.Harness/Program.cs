using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseballSim.Engine.Config;

namespace BaseballSim.Harness
{
    /// <summary>
    /// 검증 하네스 진입점.
    /// 사용법: dotnet run --project tools/BaseballSim.Harness -- [--seeds 1,2,3,4,5] [--preset Standard]
    ///         [--teams 12] [--games-per-opponent 12]
    /// </summary>
    public static class Program
    {
        private static readonly ulong[] DefaultSeeds = { 1, 2, 3, 4, 5 };

        public static int Main(string[] args)
        {
            LeagueConfig config = LeagueConfig.CreateDefault();
            ulong[] seeds = DefaultSeeds;

            for (int i = 0; i < args.Length; i++)
            {
                string value = i + 1 < args.Length ? args[i + 1] : null;
                switch (args[i])
                {
                    case "--seeds":
                        seeds = value.Split(',').Select(ulong.Parse).ToArray();
                        i++;
                        break;
                    case "--preset":
                        LeagueEnvironment env = LeagueEnvironmentPresets.ByName(value);
                        if (env == null)
                        {
                            Console.Error.WriteLine("알 수 없는 프리셋: " + value);
                            return 2;
                        }

                        config.Environment = env;
                        i++;
                        break;
                    case "--teams":
                        config.Season.TeamCount = int.Parse(value);
                        i++;
                        break;
                    case "--games-per-opponent":
                        config.Season.GamesPerOpponent = int.Parse(value);
                        i++;
                        break;
                    default:
                        Console.Error.WriteLine("알 수 없는 인자: " + args[i]);
                        return 2;
                }
            }

            var results = new SeasonResult[seeds.Length];
            Parallel.For(0, seeds.Length, i => results[i] = SeasonRunner.Run(config, seeds[i]));

            string report = CalibrationReport.Build(config, results.ToList(), out bool passed);
            Console.WriteLine(report);
            return passed ? 0 : 1;
        }
    }
}
