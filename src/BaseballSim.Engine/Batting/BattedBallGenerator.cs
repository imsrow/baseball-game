using System;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Probability;
using BaseballSim.Engine.Randomness;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.Batting
{
    /// <summary>
    /// 인플레이 타구 생성.
    /// 정타/빗맞음을 log5로 정한 뒤 각 분포에서 타구속도·발사각을 뽑고, 방향은 당겨치기 성향·투구 위치로 정한다.
    /// </summary>
    public sealed class BattedBallGenerator
    {
        // 방향각 샘플링 재시도 횟수 (페어 지역을 벗어나면 다시 뽑는다)
        private const int SprayResampleLimit = 8;

        private readonly LeagueConfig _config;

        public BattedBallGenerator(LeagueConfig config)
        {
            _config = config;
        }

        /// <summary>정타 확률</summary>
        public double SolidContactProbability(BatterRatings batter, ExecutedPitch pitch, bool sameHand, double? timingQuality)
        {
            BattedBallConfig bc = _config.BattedBall;
            double league = _config.Environment.SolidContactRate;
            double batterRate = RatingToRate.Rate(league, batter.Contact, bc.ContactSolidBeta);
            double pitcherRate = LogOdds.Logistic(LogOdds.Logit(league) - bc.StuffSolidBeta * pitch.EffectiveStuffZ);
            double p = OddsRatio.Log5(batterRate, pitcherRate, league);

            double shift = 0;
            if (sameHand)
            {
                shift += _config.Platoon.SameHandSolidShift;
            }

            if (timingQuality.HasValue)
            {
                shift += Math.Max(-1.0, Math.Min(1.0, timingQuality.Value)) * _config.InputModifier.MaxSwingSolidLogitShift;
            }

            return LogOdds.Shift(p, shift);
        }

        public BattedBall Generate(BatterRatings batter, Hand battingHand, ExecutedPitch pitch, bool sameHand,
            double? timingQuality, IRandomSource random)
        {
            BattedBallConfig bc = _config.BattedBall;
            LeagueEnvironment env = _config.Environment;

            double powerZ = ScoutScale.ToZ(batter.Power);
            double gapZ = ScoutScale.ToZ(batter.Gap);
            bool solid = random.NextDouble() < SolidContactProbability(batter, pitch, sameHand, timingQuality);

            double exitVelocity;
            double launchAngle;
            if (solid)
            {
                double laSd = bc.SolidLaunchAngleSdDeg * Math.Exp(-bc.GapLaunchAngleSdBeta * gapZ);
                launchAngle = bc.SolidLaunchAngleMeanDeg + bc.PowerLaunchAngleDegPerSd * powerZ + random.NextGaussian() * laSd;
                exitVelocity = env.SolidExitVelocityKmh
                    + bc.PowerExitVelocityKmhPerSd * powerZ
                    + bc.GapExitVelocityKmhPerSd * gapZ
                    + random.NextGaussian() * bc.SolidExitVelocitySdKmh;
                double deviation = (launchAngle - bc.OptimalLaunchAngleDeg) / bc.LaunchAnglePenaltyScaleDeg;
                exitVelocity -= bc.LaunchAnglePenaltyKmh * deviation * deviation;
            }
            else
            {
                launchAngle = random.NextDouble() < bc.WeakToppedProbability
                    ? random.Gaussian(bc.ToppedLaunchAngleMeanDeg, bc.ToppedLaunchAngleSdDeg)
                    : random.Gaussian(bc.UnderLaunchAngleMeanDeg, bc.UnderLaunchAngleSdDeg);
                exitVelocity = env.WeakExitVelocityKmh
                    + bc.WeakPowerExitVelocityKmhPerSd * powerZ
                    + random.NextGaussian() * bc.WeakExitVelocitySdKmh;
            }

            // 투구 높이와 구종에 따른 발사각 보정
            launchAngle += bc.LocationLaunchAngleDegPerM * (pitch.Actual.Z - _config.StrikeZone.CenterHeightM);
            launchAngle += bc.PitchTypeLaunchAngleOffsetDeg.Get(pitch.Type);

            launchAngle = Math.Max(bc.MinLaunchAngleDeg, Math.Min(bc.MaxLaunchAngleDeg, launchAngle));
            exitVelocity = Math.Max(bc.MinExitVelocityKmh, Math.Min(bc.MaxExitVelocityKmh, exitVelocity));

            return new BattedBall
            {
                ExitVelocityKmh = exitVelocity,
                LaunchAngleDeg = launchAngle,
                SprayAngleDeg = SampleSpray(batter, battingHand, pitch, launchAngle, random),
                IsSolid = solid,
            };
        }

        private double SampleSpray(BatterRatings batter, Hand battingHand, ExecutedPitch pitch, double launchAngle,
            IRandomSource random)
        {
            BattedBallConfig bc = _config.BattedBall;

            // 우타자는 좌측(−)으로 당긴다
            double pullSign = battingHand == Hand.Right ? -1.0 : 1.0;
            double pull = bc.PullBaseDeg
                + bc.PullDegPerSd * ScoutScale.ToZ(batter.PullTendency)
                + bc.InsidePullDegPerM * StrikeZone.InsideAmount(pitch.Actual, battingHand);
            if (launchAngle < bc.GroundBallPullMaxLaunchAngleDeg)
            {
                pull += bc.GroundBallExtraPullDeg;
            }

            double mean = pullSign * pull;
            double spray = 0;
            for (int i = 0; i < SprayResampleLimit; i++)
            {
                spray = mean + random.NextGaussian() * bc.SpraySdDeg;
                if (Math.Abs(spray) <= bc.FairAngleDeg)
                {
                    return spray;
                }
            }

            return Math.Max(-bc.FairAngleDeg, Math.Min(bc.FairAngleDeg, spray));
        }
    }
}
