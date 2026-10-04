using System;

namespace BaseballSim.Engine.Ratings
{
    /// <summary>
    /// 20~80 스카우트 스케일 정의. 50이 리그 평균, 10점이 1 표준편차.
    /// (스케일 자체의 정의값이므로 튜닝 계수가 아니다)
    /// </summary>
    public static class ScoutScale
    {
        public const int Min = 20;
        public const int Max = 80;
        public const int Average = 50;
        public const int PointsPerStandardDeviation = 10;

        /// <summary>능력치 → 표준점수 z</summary>
        public static double ToZ(double rating)
        {
            return (rating - Average) / PointsPerStandardDeviation;
        }

        /// <summary>표준점수 z → 능력치 (범위 제한)</summary>
        public static int FromZ(double z)
        {
            return Clamp((int)Math.Round(Average + z * PointsPerStandardDeviation));
        }

        public static int Clamp(int rating)
        {
            return Math.Max(Min, Math.Min(Max, rating));
        }
    }
}
