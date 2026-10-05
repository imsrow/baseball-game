using System;

namespace BaseballSim.BalanceProbe
{
    /// <summary>
    /// 시나리오 글자 코드·이름·분류
    /// </summary>
    public static class ScenarioInfo
    {
        public static char Code(Scenario scenario)
        {
            return (char)('a' + (int)scenario);
        }

        public static Scenario FromCode(char code)
        {
            int index = char.ToLowerInvariant(code) - 'a';
            if (index < 0 || index > (int)Scenario.BatChaseUniform)
            {
                throw new ArgumentException("알 수 없는 시나리오: " + code);
            }

            return (Scenario)index;
        }

        public static string Name(Scenario scenario)
        {
            switch (scenario)
            {
                case Scenario.AiVsAi: return "AI pitcher vs AI batter";
                case Scenario.HumanEdge: return "pitch: edges, RQ .5-1";
                case Scenario.HumanUniform: return "pitch: whole zone, RQ .5-1";
                case Scenario.AiTargetHumanRelease: return "pitch: AI target, RQ .5-1";
                case Scenario.EdgePerfectMaxCap: return "pitch: edges, RQ 1, sigma cap 1.5";
                case Scenario.HumanHeart: return "pitch: Heart only, RQ .5-1";
                case Scenario.BatZoneUniform: return "bat: zone only, TQ 0-1";
                case Scenario.BatZonePerfect: return "bat: zone only, TQ 1";
                default: return "bat: chase 50%, TQ 0-1";
            }
        }

        /// <summary>사람이 타격하는 시나리오인지 (아니면 사람이 투구하거나 AI끼리)</summary>
        public static bool IsHumanBatting(Scenario scenario)
        {
            return scenario >= Scenario.BatZoneUniform;
        }

        public static bool IsHumanPitching(Scenario scenario)
        {
            return scenario != Scenario.AiVsAi && !IsHumanBatting(scenario);
        }
    }
}
