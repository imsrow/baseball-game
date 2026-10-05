using System;
using System.Collections.Generic;
using System.Linq;
using BaseballSim.Engine.AI;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.BalanceProbe
{
    /// <summary>
    /// 사람처럼 던지는 투수. 엔진에는 Unity와 같은 경로(GameEngine.Submit)로 제출한다.
    /// 구종은 레퍼토리 사용 비율대로, 목표는 시나리오별 분포, ReleaseQuality는 0.5~1.0 (게이지를 대체로 잘 맞춘 경우).
    /// </summary>
    public sealed class HumanLikePitcher
    {
        private const double MinRelease = 0.5;

        // 사람이 테두리를 노릴 때의 손 떨림 (m)
        private const double EdgeJitterM = 0.06;

        private readonly Random _random;
        private readonly AiPitchingDecision _ai = new AiPitchingDecision();

        public HumanLikePitcher(Random random)
        {
            _random = random;
        }

        public PitchCall Choose(Scenario scenario, PitchingContext context)
        {
            StrikeZoneConfig zone = context.Config.StrikeZone;
            double release = scenario == Scenario.EdgePerfectMaxCap ? 1.0 : MinRelease + _random.NextDouble() * (1.0 - MinRelease);
            if (scenario == Scenario.AiTargetHumanRelease)
            {
                PitchCall call = _ai.DecidePitch(context).Value;
                call.ReleaseQuality = release;
                return call;
            }

            PlateLocation target;
            switch (scenario)
            {
                case Scenario.HumanEdge:
                case Scenario.EdgePerfectMaxCap:
                    target = EdgeTarget(zone);
                    break;
                case Scenario.HumanHeart:
                    target = new PlateLocation(Signed() * zone.HalfWidthM * zone.HeartLimit,
                        zone.CenterHeightM + Signed() * zone.HalfHeightM * zone.HeartLimit);
                    break;
                default:
                    target = new PlateLocation(Signed() * zone.HalfWidthM, zone.BottomM + _random.NextDouble() * (zone.TopM - zone.BottomM));
                    break;
            }

            return new PitchCall(PickType(context.Pitcher.Pitching.Repertoire), target, release);
        }

        /// <summary>존 테두리 위의 임의 점 + 약간의 흔들림</summary>
        private PlateLocation EdgeTarget(StrikeZoneConfig zone)
        {
            double w = zone.HalfWidthM * 2, h = zone.TopM - zone.BottomM;
            double t = _random.NextDouble() * 2 * (w + h);
            double x, z;
            if (t < w)
            {
                x = -zone.HalfWidthM + t;
                z = zone.TopM;
            }
            else if (t < w + h)
            {
                x = zone.HalfWidthM;
                z = zone.TopM - (t - w);
            }
            else if (t < 2 * w + h)
            {
                x = zone.HalfWidthM - (t - w - h);
                z = zone.BottomM;
            }
            else
            {
                x = -zone.HalfWidthM;
                z = zone.BottomM + (t - 2 * w - h);
            }

            return new PlateLocation(x + (_random.NextDouble() - 0.5) * EdgeJitterM, z + (_random.NextDouble() - 0.5) * EdgeJitterM);
        }

        private PitchType PickType(List<PitchRating> repertoire)
        {
            double pick = _random.NextDouble() * repertoire.Sum(r => r.Usage);
            foreach (PitchRating rating in repertoire)
            {
                pick -= rating.Usage;
                if (pick <= 0)
                {
                    return rating.Type;
                }
            }

            return repertoire[repertoire.Count - 1].Type;
        }

        private double Signed()
        {
            return _random.NextDouble() * 2 - 1;
        }
    }
}
