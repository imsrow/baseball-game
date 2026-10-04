using System;
using System.Collections.Generic;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Randomness;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.AI
{
    /// <summary>
    /// AI 투수: 카운트별 구종 가중치로 구종을 고르고, 존 안/밖 공략 의도에 따라 목표 지점을 정한다.
    /// </summary>
    public sealed class AiPitchingDecision : IPitchingDecision
    {
        public Decision<PitchCall> DecidePitch(PitchingContext context)
        {
            LeagueConfig config = context.Config;
            PitchConfig pc = config.Pitch;
            IRandomSource random = context.Random;
            List<PitchRating> repertoire = context.Pitcher.Pitching.Repertoire;

            var weights = new double[repertoire.Count];
            for (int i = 0; i < repertoire.Count; i++)
            {
                PitchRating pitch = repertoire[i];
                double countFactor = PitchTypeInfo.FamilyOf(pitch.Type) == PitchFamily.Fastball
                    ? pc.FastballWeightByCount.Get(context.Balls, context.Strikes)
                    : 1.0;
                weights[i] = pitch.Usage * countFactor * Math.Exp(pc.StuffUsageBeta * ScoutScale.ToZ(pitch.Stuff));
            }

            PitchType type = repertoire[random.WeightedIndex(weights)].Type;
            bool attackZone = random.NextDouble() < config.Environment.ZoneIntentByCount.Get(context.Balls, context.Strikes);
            PlateLocation target = attackZone
                ? InZoneTarget(type, config, random)
                : OutOfZoneTarget(type, context, random);
            return Decision<PitchCall>.Ready(new PitchCall(type, target));
        }

        private static double VerticalBias(PitchType type, PitchConfig pc)
        {
            switch (PitchTypeInfo.FamilyOf(type))
            {
                case PitchFamily.Fastball: return pc.FastballVerticalBias;
                case PitchFamily.Breaking: return pc.BreakingVerticalBias;
                default: return pc.OffspeedVerticalBias;
            }
        }

        private static PlateLocation InZoneTarget(PitchType type, LeagueConfig config, IRandomSource random)
        {
            StrikeZoneConfig zone = config.StrikeZone;
            PitchConfig pc = config.Pitch;
            double spread = pc.InZoneTargetSpread;
            double x = random.Range(-1.0, 1.0) * zone.HalfWidthM * spread;
            double v = random.Range(-1.0, 1.0) * spread + VerticalBias(type, pc);
            v = Math.Max(-spread, Math.Min(spread, v));
            return new PlateLocation(x, zone.CenterHeightM + v * zone.HalfHeightM);
        }

        private static PlateLocation OutOfZoneTarget(PitchType type, PitchingContext context, IRandomSource random)
        {
            StrikeZoneConfig zone = context.Config.StrikeZone;
            PitchConfig pc = context.Config.Pitch;
            double beyond = random.Range(pc.ChaseTargetMinM, pc.ChaseTargetMaxM);
            bool fastball = PitchTypeInfo.FamilyOf(type) == PitchFamily.Fastball;
            bool vertical = random.NextDouble() < (fastball ? pc.FastballChaseUpProbability : pc.BreakingChaseDownProbability);
            double spread = pc.InZoneTargetSpread;

            if (vertical)
            {
                double z = fastball
                    ? zone.TopM + beyond
                    : zone.BottomM - beyond;
                double x = random.Range(-1.0, 1.0) * zone.HalfWidthM * spread;
                return new PlateLocation(x, z);
            }

            double awayX = StrikeZone.AwaySign(context.BattingHand) * (zone.HalfWidthM + beyond);
            double v = random.Range(-1.0, 1.0) * spread + VerticalBias(type, pc);
            v = Math.Max(-spread, Math.Min(spread, v));
            return new PlateLocation(awayX, zone.CenterHeightM + v * zone.HalfHeightM);
        }
    }
}
