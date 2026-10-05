using System;
using BaseballSim.Engine.Control;

namespace BaseballSim.BalanceProbe
{
    /// <summary>
    /// 사람처럼 치는 타자. 실제 투구 위치를 보고 존 안이면 스윙한다 (선구안이 완벽한 사람에 가깝다).
    /// 타이밍 방향·커서 상하 보정은 중립(null)으로 두고 TimingQuality만 준다.
    /// </summary>
    public sealed class HumanLikeBatter
    {
        private const double ChaseSwingProbability = 0.5;

        private readonly Random _random;

        public HumanLikeBatter(Random random)
        {
            _random = random;
        }

        public BatterAction Choose(Scenario scenario, BattingContext context)
        {
            bool swing = context.Actual.IsInZone
                || (scenario == Scenario.BatChaseUniform && _random.NextDouble() < ChaseSwingProbability);
            if (!swing)
            {
                return BatterAction.Take();
            }

            double timing = scenario == Scenario.BatZonePerfect ? 1.0 : _random.NextDouble();
            return BatterAction.Swing(timing);
        }
    }
}
