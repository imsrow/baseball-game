using System;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Players;

namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 구장 기하: 베이스 위치, 방향별 펜스 거리, 야수 기본 위치
    /// </summary>
    public sealed class FieldGeometry
    {
        // 홈 = 0, 1루 = 1, 2루 = 2, 3루 = 3, 홈(득점) = 4
        private readonly FieldPoint[] _bases;
        private readonly FieldConfig _config;

        public FieldGeometry(FieldConfig config)
        {
            _config = config;
            double d = config.BaseDistanceM / Math.Sqrt(2.0);
            _bases = new[]
            {
                new FieldPoint(0, 0),
                new FieldPoint(d, d),
                new FieldPoint(0, 2 * d),
                new FieldPoint(-d, d),
                new FieldPoint(0, 0),
            };
        }

        public FieldConfig Config => _config;

        /// <summary>베이스 위치 (0·4 = 홈, 1~3 = 각 루)</summary>
        public FieldPoint Base(int baseIndex)
        {
            return _bases[baseIndex];
        }

        /// <summary>방향각에 따른 펜스 거리 (구간 선형 보간)</summary>
        public double FenceDistance(double sprayDeg)
        {
            double[] angles = _config.FenceAnglesDeg;
            double[] distances = _config.FenceDistancesM;
            if (sprayDeg <= angles[0])
            {
                return distances[0];
            }

            for (int i = 1; i < angles.Length; i++)
            {
                if (sprayDeg <= angles[i])
                {
                    double t = (sprayDeg - angles[i - 1]) / (angles[i] - angles[i - 1]);
                    return distances[i - 1] + t * (distances[i] - distances[i - 1]);
                }
            }

            return distances[distances.Length - 1];
        }

        /// <summary>야수 기본 수비 위치</summary>
        public FieldPoint FielderStart(Position position)
        {
            int i = (int)position;
            return FieldPoint.FromPolar(_config.FielderDistancesM[i], _config.FielderAnglesDeg[i]);
        }
    }
}
