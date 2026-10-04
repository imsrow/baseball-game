namespace BaseballSim.Engine.Probability
{
    /// <summary>
    /// 리그 평균 대비 odds ratio(log5) 결합.
    /// odds(결과) = odds(타자) × odds(투수) / odds(리그)
    /// </summary>
    public static class OddsRatio
    {
        /// <summary>
        /// 타자 확률, 투수 확률, 리그 평균 확률을 log5로 결합한다.
        /// 타자와 투수가 모두 리그 평균이면 리그 평균을 돌려준다.
        /// </summary>
        public static double Log5(double batterRate, double pitcherRate, double leagueRate)
        {
            double logit = LogOdds.Logit(batterRate) + LogOdds.Logit(pitcherRate) - LogOdds.Logit(leagueRate);
            return LogOdds.Logistic(logit);
        }

        /// <summary>
        /// 여러 요인(타자, 투수, 포수 등)의 개별 확률을 리그 평균 대비로 결합한다.
        /// 요인이 하나면 그 값, 두 개면 Log5와 같다.
        /// </summary>
        public static double Combine(double leagueRate, params double[] factorRates)
        {
            double leagueLogit = LogOdds.Logit(leagueRate);
            double logit = leagueLogit;
            for (int i = 0; i < factorRates.Length; i++)
            {
                logit += LogOdds.Logit(factorRates[i]) - leagueLogit;
            }

            return LogOdds.Logistic(logit);
        }
    }
}
