namespace BaseballProto.Quality
{
    /// <summary>
    /// 오차 → 점수 공통 계산
    /// </summary>
    public static class QualityMath
    {
        /// <summary>오차 크기가 perfect 이하면 1, zero 이상이면 0, 사이는 직선</summary>
        public static double Score(double error, double perfect, double zero)
        {
            if (error <= perfect)
            {
                return 1.0;
            }

            if (error >= zero || zero <= perfect)
            {
                return 0.0;
            }

            return 1.0 - (error - perfect) / (zero - perfect);
        }

        /// <summary>점수(0~1) → 엔진 품질(−1~+1)</summary>
        public static double ToQuality(double score)
        {
            return ClampUnit(score * 2.0 - 1.0);
        }

        public static double ClampUnit(double value)
        {
            return value < -1.0 ? -1.0 : value > 1.0 ? 1.0 : value;
        }
    }
}
