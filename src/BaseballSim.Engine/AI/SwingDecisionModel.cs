using BaseballSim.Engine.Batting;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Probability;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.AI
{
    /// <summary>
    /// AI 타자 스윙 확률. 실제 위치가 아닌 인지 위치(선구안 오차 적용)의 구역을 기준으로 한다.
    /// logit(스윙) = logit(구역별 리그 스윙률) + 카운트 보정 + 선구안·구위·플래툰 보정
    /// </summary>
    public static class SwingDecisionModel
    {
        public static double SwingProbability(LeagueConfig config, int eye, PerceivedPitch perceived,
            double effectiveStuffZ, int balls, int strikes, bool sameHand)
        {
            SwingConfig sc = config.Swing;
            double baseRate = config.Environment.SwingRateByPerceivedRegion.Get(perceived.Region);
            double logit = LogOdds.Logit(baseRate) + sc.CountSwingShift.Get(balls, strikes);
            double eyeZ = ScoutScale.ToZ(eye);
            if (perceived.IsInZone)
            {
                logit += sc.EyeZoneSwingBeta * eyeZ;
            }
            else
            {
                logit += -sc.EyeChaseBeta * eyeZ + sc.StuffChaseBeta * effectiveStuffZ;
                if (sameHand)
                {
                    logit += config.Platoon.SameHandChaseShift;
                }
            }

            return LogOdds.Logistic(logit);
        }
    }
}
