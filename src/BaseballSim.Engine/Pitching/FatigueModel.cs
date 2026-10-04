using System;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.Pitching
{
    /// <summary>
    /// 경기 내 투수 피로도 (0 ~ 1)
    /// </summary>
    public static class FatigueModel
    {
        /// <summary>피로 없이 던질 수 있는 투구 수</summary>
        public static double ComfortPitches(PitcherRatings ratings, FatigueConfig config)
        {
            return config.ComfortPitchesBase + config.ComfortPitchesPerSd * ScoutScale.ToZ(ratings.Stamina);
        }

        public static double Fatigue(PitcherRatings ratings, int pitchCount, FatigueConfig config)
        {
            double over = pitchCount - ComfortPitches(ratings, config);
            if (over <= 0)
            {
                return 0;
            }

            return Math.Min(1.0, over / config.FadePitches);
        }
    }
}
