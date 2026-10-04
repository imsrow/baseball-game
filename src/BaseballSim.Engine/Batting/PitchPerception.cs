using System;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Randomness;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.Batting
{
    /// <summary>
    /// 투구 인지: 실제 위치에 선구안·구위에 따른 오차를 더해 타자가 본 위치를 만든다.
    /// AI 타자는 이 인지 위치로만 스윙 여부를 정한다.
    /// 컨트롤러 종류와 무관하게 매 투구 계산해 난수 소비를 일정하게 유지한다.
    /// </summary>
    public sealed class PitchPerception
    {
        private readonly SwingConfig _config;
        private readonly StrikeZone _zone;

        public PitchPerception(SwingConfig config, StrikeZone zone)
        {
            _config = config;
            _zone = zone;
        }

        /// <summary>선구안·구위에 따른 인지 오차 표준편차 (m)</summary>
        public double PerceptionSigma(int eye, double effectiveStuffZ)
        {
            return _config.PerceptionSigmaM
                * Math.Exp(-_config.EyePerceptionBeta * ScoutScale.ToZ(eye))
                * Math.Exp(_config.StuffPerceptionBeta * effectiveStuffZ);
        }

        public PerceivedPitch Perceive(ExecutedPitch pitch, BatterRatings batter, IRandomSource random)
        {
            double sigma = PerceptionSigma(batter.Eye, pitch.EffectiveStuffZ);
            var location = new PlateLocation(
                pitch.Actual.X + random.NextGaussian() * sigma,
                pitch.Actual.Z + random.NextGaussian() * sigma);
            return new PerceivedPitch
            {
                Location = location,
                Region = _zone.Classify(location),
                IsInZone = _zone.IsInZone(location),
            };
        }
    }
}
