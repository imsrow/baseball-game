using BaseballSim.Engine.Config;
using BaseballSim.Engine.Probability;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 실책 확률 계산
    /// </summary>
    public sealed class FieldingProbabilities
    {
        private readonly FieldingConfig _config;

        public FieldingProbabilities(FieldingConfig config)
        {
            _config = config;
        }

        /// <summary>뜬공 포구 실책 (난이도 0~1)</summary>
        public double AirBallError(FielderProfile fielder, double difficulty)
        {
            return LogOdds.Shift(_config.AirBallErrorRate,
                -_config.HandsErrorBeta * ScoutScale.ToZ(fielder.Hands) + _config.DifficultyErrorShift * difficulty);
        }

        /// <summary>땅볼 포구 실책 (난이도 0~1)</summary>
        public double GroundBallError(FielderProfile fielder, double difficulty)
        {
            return LogOdds.Shift(_config.GroundBallErrorRate,
                -_config.HandsErrorBeta * ScoutScale.ToZ(fielder.Hands) + _config.DifficultyErrorShift * difficulty);
        }

        /// <summary>송구 실책</summary>
        public double ThrowError(FielderProfile fielder, double distanceM)
        {
            return LogOdds.Shift(_config.ThrowErrorRate,
                -_config.AccuracyErrorBeta * ScoutScale.ToZ(fielder.Accuracy)
                + _config.ThrowErrorPerMeter * (distanceM - _config.ThrowErrorReferenceDistanceM));
        }
    }
}
