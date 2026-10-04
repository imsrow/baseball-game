using System;

namespace BaseballSim.Engine.Randomness
{
    /// <summary>
    /// 난수원 편의 함수
    /// </summary>
    public static class RandomExtensions
    {
        /// <summary>확률 p로 true</summary>
        public static bool Chance(this IRandomSource random, double probability)
        {
            return random.NextDouble() < probability;
        }

        /// <summary>N(mean, sd)</summary>
        public static double Gaussian(this IRandomSource random, double mean, double standardDeviation)
        {
            return mean + random.NextGaussian() * standardDeviation;
        }

        /// <summary>[min, max) 균등 실수</summary>
        public static double Range(this IRandomSource random, double min, double max)
        {
            return min + (max - min) * random.NextDouble();
        }

        /// <summary>가중치 배열에서 인덱스 하나를 고른다. 가중치 합이 0 이하면 0을 돌려준다.</summary>
        public static int WeightedIndex(this IRandomSource random, double[] weights)
        {
            double total = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                total += Math.Max(0, weights[i]);
            }

            if (total <= 0)
            {
                return 0;
            }

            double roll = random.NextDouble() * total;
            for (int i = 0; i < weights.Length; i++)
            {
                roll -= Math.Max(0, weights[i]);
                if (roll < 0)
                {
                    return i;
                }
            }

            return weights.Length - 1;
        }
    }
}
