using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.Probability
{
    /// <summary>
    /// 20~80 능력치를 개인 확률로 변환.
    /// logit(개인) = logit(리그 평균) + β × z,  z = (능력치 − 50) / 10
    /// </summary>
    public static class RatingToRate
    {
        public static double Rate(double leagueRate, double rating, double beta)
        {
            return LogOdds.Logistic(LogOdds.Logit(leagueRate) + beta * ScoutScale.ToZ(rating));
        }
    }
}
