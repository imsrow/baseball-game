using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BaseballSim.Engine.Config;

namespace BaseballSim.BalanceProbe
{
    /// <summary>
    /// 밸런스 비교 진입점. 사람처럼 던지는/치는 입력을 AI와 비교하고 구역(Heart/Shadow/Chase/Waste)별 성적을 보여준다.
    /// 사용법: dotnet run -c Release --project tools/BaseballSim.BalanceProbe -- [--games 2000] [--scenarios a,b,f]
    ///         [--swing-caps 1.0] [--location-scale 1.0]
    /// 시나리오: a AI vs AI, b 가장자리, c 존 전체, d AI 목표+사람 릴리스, e 가장자리+제구 상한 최대, f 한가운데,
    ///           g 존 안만 스윙 TQ 0~1, h 존 안만 스윙 TQ 1, i 존 밖도 50% 스윙 TQ 0~1
    /// --swing-caps는 Unity 기본값(1.0)처럼 타격 조작 보정 상한을 바꿔 본다. 엔진 기본값은 0.4
    /// </summary>
    public static class Program
    {
        private const int DefaultGames = 2000;

        public static int Main(string[] args)
        {
            int games = DefaultGames;
            List<Scenario> scenarios = Enum.GetValues(typeof(Scenario)).Cast<Scenario>().ToList();
            double? swingCaps = null;
            double? swingPenalty = null;
            double? locationScale = null;
            bool evLa = false;

            for (int i = 0; i < args.Length; i++)
            {
                string value = i + 1 < args.Length ? args[i + 1] : null;
                switch (args[i])
                {
                    case "--games":
                        games = int.Parse(value, CultureInfo.InvariantCulture);
                        i++;
                        break;
                    case "--scenarios":
                        scenarios = value.Split(',').Select(code => ScenarioInfo.FromCode(code.Trim()[0])).ToList();
                        i++;
                        break;
                    case "--swing-caps":
                        swingCaps = double.Parse(value, CultureInfo.InvariantCulture);
                        i++;
                        break;
                    case "--swing-penalty":
                        swingPenalty = double.Parse(value, CultureInfo.InvariantCulture);
                        i++;
                        break;
                    case "--ev-la":
                        evLa = true;
                        break;
                    case "--location-scale":
                        locationScale = double.Parse(value, CultureInfo.InvariantCulture);
                        i++;
                        break;
                    default:
                        Console.Error.WriteLine("알 수 없는 인자: " + args[i]);
                        return 2;
                }
            }

            LeagueConfig CreateConfig()
            {
                LeagueConfig config = LeagueConfig.CreateDefault();
                if (swingCaps.HasValue)
                {
                    config.InputModifier.MaxSwingContactLogitShift = swingCaps.Value;
                    config.InputModifier.MaxSwingSolidLogitShift = swingCaps.Value;
                }

                // 벌칙 상한을 따로 주지 않으면 보상 상한과 같게 (대칭)
                double? penalty = swingPenalty ?? swingCaps;
                if (penalty.HasValue)
                {
                    config.InputModifier.MaxSwingContactLogitPenalty = penalty.Value;
                    config.InputModifier.MaxSwingSolidLogitPenalty = penalty.Value;
                }

                if (locationScale.HasValue)
                {
                    config.LocationEffectScale = locationScale.Value;
                }

                return config;
            }

            LeagueConfig shown = CreateConfig();
            Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "=== 밸런스 비교 | 시나리오당 {0}경기 | 평균(50) 능력치 팀 | swing caps +{1}/-{2} (contact) +{3}/-{4} (solid) | location scale {5} ===",
                games, shown.InputModifier.MaxSwingContactLogitShift, shown.InputModifier.MaxSwingContactLogitPenalty,
                shown.InputModifier.MaxSwingSolidLogitShift, shown.InputModifier.MaxSwingSolidLogitPenalty,
                shown.LocationEffectScale));
            Console.WriteLine(ProbeStats.Header);
            foreach (Scenario scenario in scenarios)
            {
                ProbeStats stats = ScenarioRunner.Run(scenario, games, CreateConfig);
                Console.WriteLine(stats.Row("(" + ScenarioInfo.Code(scenario) + ") " + ScenarioInfo.Name(scenario)));
                Console.WriteLine(ProbeStats.RegionHeader);
                Console.Write(stats.RegionRows());
                if (evLa)
                {
                    Console.Write(stats.GridTable());
                }
            }

            return 0;
        }
    }
}
