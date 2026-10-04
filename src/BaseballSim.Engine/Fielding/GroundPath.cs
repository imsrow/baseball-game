using System;

namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 땅 위를 구르는 공의 경로. 구름 마찰로 일정하게 감속한다.
    /// x(t) = v0·t − a·t²/2  (멈추는 거리 v0²/2a)
    /// </summary>
    public sealed class GroundPath
    {
        public GroundPath(FieldPoint origin, double startTimeS, double directionDeg, double initialSpeedMps, double decelerationMps2)
        {
            Origin = origin;
            StartTimeS = startTimeS;
            DirectionDeg = directionDeg;
            InitialSpeedMps = Math.Max(0.01, initialSpeedMps);
            DecelerationMps2 = decelerationMps2;
        }

        public FieldPoint Origin { get; }

        public double StartTimeS { get; }

        public double DirectionDeg { get; }

        public double InitialSpeedMps { get; }

        public double DecelerationMps2 { get; }

        /// <summary>공이 멈출 때까지 굴러가는 거리</summary>
        public double StopDistanceM => InitialSpeedMps * InitialSpeedMps / (2.0 * DecelerationMps2);

        /// <summary>공이 멈추는 시각</summary>
        public double StopTimeS => StartTimeS + InitialSpeedMps / DecelerationMps2;

        /// <summary>출발점에서 d만큼 떨어진 지점</summary>
        public FieldPoint PointAt(double distance)
        {
            FieldPoint offset = FieldPoint.FromPolar(distance, DirectionDeg);
            return new FieldPoint(Origin.X + offset.X, Origin.Y + offset.Y);
        }

        /// <summary>d 지점 도달 시각 (멈추기 전 도달 못하면 무한대)</summary>
        public double TimeAt(double distance)
        {
            double v0 = InitialSpeedMps;
            double discriminant = v0 * v0 - 2.0 * DecelerationMps2 * distance;
            if (discriminant < 0)
            {
                return double.PositiveInfinity;
            }

            return StartTimeS + (v0 - Math.Sqrt(discriminant)) / DecelerationMps2;
        }
    }
}
