using System;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Randomness;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.Pitching
{
    /// <summary>
    /// 투구 실행: 목표 지점 + 제구 오차 → 실제 위치, 구속 능력치 + 구종 + 피로 → 구속.
    /// 사람 입력이든 AI든 같은 실행 과정을 거친다.
    /// </summary>
    public sealed class PitchExecutor
    {
        private readonly LeagueConfig _config;
        private readonly StrikeZone _zone;

        public PitchExecutor(LeagueConfig config, StrikeZone zone)
        {
            _config = config;
            _zone = zone;
        }

        /// <summary>
        /// 실제 투구 생성
        /// </summary>
        /// <param name="releaseQuality">사람 조작 릴리스 품질(−1~+1). AI는 null(중립)</param>
        public ExecutedPitch Execute(PitchType type, PlateLocation target, PitcherRatings pitcher, double fatigue,
            double? releaseQuality, IRandomSource random)
        {
            PitchConfig pc = _config.Pitch;
            FatigueConfig fc = _config.Fatigue;

            double velocityZ = ScoutScale.ToZ(pitcher.Velocity);
            double velocity = pc.FastballBaseVelocityKmh
                + pc.VelocityKmhPerSd * velocityZ
                + pc.VelocityOffsetKmh.Get(type)
                - fc.VelocityDropKmh * fatigue
                + random.NextGaussian() * pc.VelocityNoiseKmh;

            double sigma = pc.ControlSigmaM
                * Math.Exp(-pc.ControlBeta * ScoutScale.ToZ(pitcher.Control))
                * (1.0 + fc.ControlSigmaIncrease * fatigue);
            if (releaseQuality.HasValue)
            {
                double q = Math.Max(-1.0, Math.Min(1.0, releaseQuality.Value));
                sigma *= Math.Exp(-q * _config.InputModifier.MaxPitchExecutionSigmaLogShift);
            }

            var actual = new PlateLocation(
                target.X + random.NextGaussian() * sigma,
                target.Z + random.NextGaussian() * sigma);

            return new ExecutedPitch
            {
                Type = type,
                Target = target,
                Actual = actual,
                VelocityKmh = velocity,
                EffectiveStuffZ = EffectiveStuffZ(type, pitcher, fatigue),
                Region = _zone.Classify(actual),
                IsInZone = _zone.IsInZone(actual),
            };
        }

        /// <summary>실효 구위 z = 구종 구위 z + 구속 기여 − 피로 감소</summary>
        public double EffectiveStuffZ(PitchType type, PitcherRatings pitcher, double fatigue)
        {
            PitchRating rating = pitcher.Find(type);
            double stuffZ = rating != null ? ScoutScale.ToZ(rating.Stuff) : ScoutScale.ToZ(ScoutScale.Min);
            double velocityWeight = PitchTypeInfo.FamilyOf(type) == PitchFamily.Fastball
                ? _config.Pitch.FastballVelocityStuffWeight
                : _config.Pitch.OffspeedVelocityStuffWeight;
            return stuffZ + velocityWeight * ScoutScale.ToZ(pitcher.Velocity) - _config.Fatigue.StuffZDrop * fatigue;
        }
    }
}
