using BaseballSim.Engine.Probability;
using Xunit;

namespace BaseballSim.Engine.Tests
{
    public class ProbabilityTests
    {
        [Theory]
        [InlineData(0.22)]
        [InlineData(0.5)]
        [InlineData(0.9)]
        public void Log5_평균끼리면_리그평균(double league)
        {
            Assert.Equal(league, OddsRatio.Log5(league, league, league), 10);
        }

        [Fact]
        public void Log5_타자가_평균이면_투수확률()
        {
            Assert.Equal(0.30, OddsRatio.Log5(0.25, 0.30, 0.25), 10);
            Assert.Equal(0.18, OddsRatio.Log5(0.18, 0.25, 0.25), 10);
        }

        [Fact]
        public void Log5_대칭()
        {
            Assert.Equal(OddsRatio.Log5(0.30, 0.20, 0.25), OddsRatio.Log5(0.20, 0.30, 0.25), 12);
        }

        [Fact]
        public void Log5_알려진값()
        {
            // odds = (0.3/0.7)(0.3/0.7)/(0.25/0.75) = 0.55102 → p = 0.35526
            Assert.Equal(0.355263, OddsRatio.Log5(0.30, 0.30, 0.25), 5);
        }

        [Fact]
        public void Log5_양쪽이_좋으면_더_좋다()
        {
            double p = OddsRatio.Log5(0.30, 0.30, 0.25);
            Assert.True(p > 0.30);
            Assert.True(p < 1.0);
        }

        [Fact]
        public void Combine_요인하나는_그대로_둘은_Log5()
        {
            Assert.Equal(0.31, OddsRatio.Combine(0.25, 0.31), 10);
            Assert.Equal(OddsRatio.Log5(0.31, 0.22, 0.25), OddsRatio.Combine(0.25, 0.31, 0.22), 10);
        }

        [Fact]
        public void 극단값도_0과_1_사이()
        {
            double p = OddsRatio.Log5(0.999999, 0.999999, 0.000001);
            Assert.InRange(p, 0.0, 1.0);
            Assert.False(double.IsNaN(p));
            Assert.False(double.IsNaN(OddsRatio.Log5(0, 1, 0.5)));
        }

        [Fact]
        public void RatingToRate_50은_리그평균()
        {
            Assert.Equal(0.22, RatingToRate.Rate(0.22, 50, 0.3), 10);
        }

        [Fact]
        public void RatingToRate_10점은_로그오즈_베타만큼()
        {
            double beta = 0.3;
            double diff = LogOdds.Logit(RatingToRate.Rate(0.4, 60, beta)) - LogOdds.Logit(0.4);
            Assert.Equal(beta, diff, 10);
            Assert.True(RatingToRate.Rate(0.4, 70, beta) > RatingToRate.Rate(0.4, 60, beta));
            Assert.True(RatingToRate.Rate(0.4, 30, beta) < 0.4);
        }

        [Fact]
        public void Logit_Logistic_역함수()
        {
            foreach (double p in new[] { 0.01, 0.2, 0.5, 0.77, 0.99 })
            {
                Assert.Equal(p, LogOdds.Logistic(LogOdds.Logit(p)), 10);
            }
        }
    }
}
