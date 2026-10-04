using System;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Players;

namespace BaseballSim.Engine.Pitching
{
    /// <summary>
    /// 스트라이크 존 기하: 존 판정, 공략 구역 분류, 경계 거리, 타자 기준 몸쪽/바깥쪽
    /// </summary>
    public sealed class StrikeZone
    {
        private readonly StrikeZoneConfig _config;

        public StrikeZone(StrikeZoneConfig config)
        {
            _config = config;
        }

        public StrikeZoneConfig Config => _config;

        /// <summary>존 크기 대비 정규화 거리: max(|x|/반폭, |z−중심|/반높이). 1 이하가 존 안</summary>
        public double NormalizedDistance(PlateLocation location)
        {
            double u = Math.Abs(location.X) / _config.HalfWidthM;
            double v = Math.Abs(location.Z - _config.CenterHeightM) / _config.HalfHeightM;
            return Math.Max(u, v);
        }

        public bool IsInZone(PlateLocation location)
        {
            return NormalizedDistance(location) <= 1.0;
        }

        public AttackRegion Classify(PlateLocation location)
        {
            double n = NormalizedDistance(location);
            if (n <= _config.HeartLimit)
            {
                return AttackRegion.Heart;
            }

            if (n <= _config.ShadowLimit)
            {
                return AttackRegion.Shadow;
            }

            if (n <= _config.ChaseLimit)
            {
                return AttackRegion.Chase;
            }

            return AttackRegion.Waste;
        }

        /// <summary>
        /// 존 경계로부터의 부호 있는 거리 (m). 존 밖이면 양수, 존 안이면 음수(가장 가까운 경계까지)
        /// </summary>
        public double SignedEdgeDistance(PlateLocation location)
        {
            double dx = Math.Abs(location.X) - _config.HalfWidthM;
            double dz = Math.Abs(location.Z - _config.CenterHeightM) - _config.HalfHeightM;
            if (dx <= 0 && dz <= 0)
            {
                return Math.Max(dx, dz);
            }

            double ox = Math.Max(dx, 0);
            double oz = Math.Max(dz, 0);
            return Math.Sqrt(ox * ox + oz * oz);
        }

        /// <summary>
        /// 타자 기준 몸쪽 방향 거리 (m, +가 몸쪽). 우타자는 3루 쪽(−x)에 선다.
        /// </summary>
        public static double InsideAmount(PlateLocation location, Hand battingHand)
        {
            return battingHand == Hand.Right ? -location.X : location.X;
        }

        /// <summary>타자 기준 바깥쪽 방향의 x 부호</summary>
        public static double AwaySign(Hand battingHand)
        {
            return battingHand == Hand.Right ? 1.0 : -1.0;
        }

        /// <summary>몸에 맞을 수 있는 위치인지</summary>
        public bool IsInHitByPitchArea(PlateLocation location, Hand battingHand)
        {
            return InsideAmount(location, battingHand) >= _config.HitByPitchInsideLineM
                && location.Z >= _config.HitByPitchMinHeightM
                && location.Z <= _config.HitByPitchMaxHeightM;
        }
    }
}
