using BaseballSim.Engine.Config;
using BaseballSim.Engine.Probability;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.Pitching
{
    /// <summary>
    /// 루킹 판정: 존 경계 거리에 따른 스트라이크 확률 + Shadow 구역 포수 프레이밍
    /// </summary>
    public sealed class UmpireJudge
    {
        private readonly StrikeZoneConfig _config;
        private readonly StrikeZone _zone;

        public UmpireJudge(StrikeZoneConfig config, StrikeZone zone)
        {
            _config = config;
            _zone = zone;
        }

        public double CalledStrikeProbability(PlateLocation location, int catcherFraming)
        {
            double edge = _zone.SignedEdgeDistance(location);
            double logit = -edge / _config.UmpireEdgeScaleM;
            if (_zone.Classify(location) == AttackRegion.Shadow)
            {
                logit += _config.FramingBeta * ScoutScale.ToZ(catcherFraming);
            }

            return LogOdds.Logistic(logit);
        }
    }
}
