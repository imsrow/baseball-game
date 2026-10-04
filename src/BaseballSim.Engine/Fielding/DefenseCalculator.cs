using BaseballSim.Engine.Config;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 수비 도구 × 포지션 숙련도 → 실효 수비 능력치
    /// </summary>
    public static class DefenseCalculator
    {
        /// <summary>숙련도에 따른 도구 발휘 배율</summary>
        public static double ProficiencyMultiplier(int proficiency, DefenseConfig config)
        {
            int prof = proficiency == FielderRatings.Unrated ? config.DefaultProficiency : proficiency;
            if (prof <= config.FullProficiency)
            {
                double t = (double)(prof - ScoutScale.Min) / (config.FullProficiency - ScoutScale.Min);
                return config.MultiplierAtMin + (1.0 - config.MultiplierAtMin) * t;
            }
            else
            {
                double t = (double)(prof - config.FullProficiency) / (ScoutScale.Max - config.FullProficiency);
                return 1.0 + (config.MultiplierAtMax - 1.0) * t;
            }
        }

        /// <summary>실효 = 20 + (도구 − 20) × 배율</summary>
        public static double EffectiveRating(int tool, int proficiency, DefenseConfig config)
        {
            return ScoutScale.Min + (tool - ScoutScale.Min) * ProficiencyMultiplier(proficiency, config);
        }
    }
}
