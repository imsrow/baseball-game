using System;

namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 그라운드 위 좌표 (m). 홈플레이트 원점, +y는 중견수 방향, +x는 1루·우익수 방향
    /// </summary>
    public struct FieldPoint
    {
        public FieldPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; set; }

        public double Y { get; set; }

        /// <summary>홈플레이트로부터 거리</summary>
        public double DistanceFromHome => Math.Sqrt(X * X + Y * Y);

        public static double Distance(FieldPoint a, FieldPoint b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>홈에서의 거리와 방향각(도, 0이 중견수 방향, +가 1루 쪽)으로 생성</summary>
        public static FieldPoint FromPolar(double distance, double angleDeg)
        {
            double rad = angleDeg * Math.PI / 180.0;
            return new FieldPoint(distance * Math.Sin(rad), distance * Math.Cos(rad));
        }

        public override string ToString()
        {
            return "(" + X.ToString("0.0") + ", " + Y.ToString("0.0") + ")";
        }
    }
}
