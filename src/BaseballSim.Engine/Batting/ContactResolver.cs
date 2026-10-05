using System;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Probability;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.Batting
{
    /// <summary>
    /// 스윙했을 때 컨택(헛스윙 여부)과 파울 확률.
    /// 컨택 = log5(타자 삼진회피, 투수 실효 구위, 구역별 리그 컨택률) + 플래툰·2스트라이크·입력 보정
    /// </summary>
    public sealed class ContactResolver
    {
        private readonly LeagueConfig _config;

        public ContactResolver(LeagueConfig config)
        {
            _config = config;
        }

        public double ContactProbability(BatterRatings batter, ExecutedPitch pitch, int strikes, bool sameHand,
            double? timingQuality)
        {
            ContactConfig cc = _config.Contact;
            double league = _config.Environment.ContactRateByRegion.Get(pitch.Region);
            bool twoStrikes = strikes >= _config.Rules.StrikesForStrikeout - 1;

            double batterBeta = cc.AvoidKBeta + (twoStrikes ? cc.TwoStrikeAvoidKExtraBeta : 0);
            double batterRate = RatingToRate.Rate(league, batter.AvoidK, batterBeta);
            double pitcherRate = LogOdds.Logistic(LogOdds.Logit(league) - cc.StuffBeta * pitch.EffectiveStuffZ);
            double p = OddsRatio.Log5(batterRate, pitcherRate, league);

            double shift = 0;
            if (twoStrikes)
            {
                shift += cc.TwoStrikeContactShift;
            }

            if (sameHand)
            {
                shift += _config.Platoon.SameHandContactShift;
            }

            if (timingQuality.HasValue)
            {
                shift += _config.InputModifier.SwingContactLogitShift(timingQuality.Value);
            }

            return LogOdds.Shift(p, shift);
        }

        /// <summary>컨택 중 파울 비율 (구역별 리그 기준값)</summary>
        public double FoulProbability(ExecutedPitch pitch)
        {
            return _config.Environment.FoulRateByRegion.Get(pitch.Region);
        }
    }
}
