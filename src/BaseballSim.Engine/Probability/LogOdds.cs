using System;

namespace BaseballSim.Engine.Probability
{
    /// <summary>
    /// 로그 오즈(logit) 변환
    /// </summary>
    public static class LogOdds
    {
        // 0/1 확률에서 무한대가 나오지 않게 하는 수치 보호값
        private const double Epsilon = 1e-9;

        public static double Logit(double probability)
        {
            double p = Math.Min(1.0 - Epsilon, Math.Max(Epsilon, probability));
            return Math.Log(p / (1.0 - p));
        }

        public static double Logistic(double logit)
        {
            return 1.0 / (1.0 + Math.Exp(-logit));
        }

        /// <summary>확률에 로그 오즈 이동량을 더한다</summary>
        public static double Shift(double probability, double logitShift)
        {
            return Logistic(Logit(probability) + logitShift);
        }
    }
}
