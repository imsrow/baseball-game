using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Fielding;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Probability;
using BaseballSim.Engine.Randomness;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.Batting
{
    /// <summary>
    /// 번트 판정 (단순 확률 모델). 페어 번트의 결과는 물리 수비 대신 확률로 정한다.
    /// 타구속도·발사각·방향은 기록 표시용으로만 생성한다.
    /// </summary>
    public sealed class BuntResolver
    {
        private readonly LeagueConfig _config;

        public BuntResolver(LeagueConfig config)
        {
            _config = config;
        }

        /// <summary>번트 컨택(파울 포함) 확률</summary>
        public double ContactProbability(BatterRatings batter, ExecutedPitch pitch)
        {
            BuntConfig bc = _config.Bunt;
            double shift = bc.BuntBeta * ScoutScale.ToZ(batter.Bunt);
            if (!pitch.IsInZone)
            {
                shift += bc.OutOfZoneContactShift;
            }

            return LogOdds.Shift(bc.ContactRate, shift);
        }

        /// <summary>컨택한 번트 중 파울 확률</summary>
        public double FoulProbability(BatterRatings batter)
        {
            return LogOdds.Shift(_config.Bunt.FoulRate, -_config.Bunt.BuntBeta * ScoutScale.ToZ(batter.Bunt));
        }

        /// <summary>요청한 번트 종류를 상황에 맞게 정리 (주자가 없으면 기습번트)</summary>
        public static BuntType EffectiveType(BuntType requested, PlaySituation situation)
        {
            bool runnerToAdvance = situation.RunnerOn(1) != null || situation.RunnerOn(2) != null;
            if (requested == BuntType.Sacrifice && runnerToAdvance)
            {
                return BuntType.Sacrifice;
            }

            if (requested == BuntType.None && runnerToAdvance && situation.OutsBefore < 2)
            {
                return BuntType.Sacrifice;
            }

            return BuntType.ForHit;
        }

        /// <summary>페어 번트 결과</summary>
        public PlayResult ResolveFair(BuntType requested, BatterRatings batter, PlaySituation situation,
            IRandomSource random, out BattedBall ball)
        {
            BuntConfig bc = _config.Bunt;
            BuntType type = EffectiveType(requested, situation);
            ball = DisplayBall(random);
            var result = new PlayResult
            {
                BallType = BattedBallType.GroundBall,
                FieldedBy = ball.SprayAngleDeg < 0 ? Position.ThirdBase : Position.FirstBase,
            };

            double speedZ = ScoutScale.ToZ(batter.Speed);
            double buntZ = ScoutScale.ToZ(batter.Bunt);
            double hitRate = type == BuntType.Sacrifice ? bc.SacrificeHitRate : bc.BuntForHitRate;
            double hit = LogOdds.Shift(hitRate, bc.SpeedBeta * speedZ + bc.BuntBeta * buntZ);
            if (random.NextDouble() < hit)
            {
                result.Outcome = PlateAppearanceOutcome.Single;
                Advance(situation, result, 1, true);
                return result;
            }

            bool sacrificeWorks = type == BuntType.ForHit
                || random.NextDouble() < LogOdds.Shift(bc.SacrificeSuccessRate, bc.BuntBeta * buntZ);
            if (sacrificeWorks)
            {
                // 타자 1루 아웃, 1·2루 주자 진루 (3루 주자는 밀려날 때만)
                result.OutsRecorded = 1;
                result.Movements.Add(new RunnerMovement(situation.Batter.PlayerId, 0, 1, true));
                for (int b = 3; b >= 1; b--)
                {
                    RunnerProfile runner = situation.RunnerOn(b);
                    if (runner != null)
                    {
                        int to = b == 3 && !situation.IsForced(3) ? 3 : b + 1;
                        result.Movements.Add(new RunnerMovement(runner.PlayerId, b, to, false));
                    }
                }

                bool advanced = situation.RunnerOn(1) != null || situation.RunnerOn(2) != null;
                result.Outcome = advanced && situation.OutsBefore < _config.Rules.OutsPerHalfInning - 1
                    ? PlateAppearanceOutcome.SacrificeBunt
                    : PlateAppearanceOutcome.GroundOut;
                if (situation.OutsBefore + 1 >= _config.Rules.OutsPerHalfInning)
                {
                    result.RunsNullified = true;
                }

                return result;
            }

            if (random.NextDouble() < bc.SacrificeFailLeadRunnerOutShare)
            {
                // 선행 주자 포스아웃, 타자 1루
                int lead = 0;
                for (int b = 1; b <= 3; b++)
                {
                    if (situation.RunnerOn(b) != null && situation.IsForced(b))
                    {
                        lead = b;
                    }
                }

                result.Outcome = PlateAppearanceOutcome.FieldersChoice;
                result.OutsRecorded = 1;
                result.Movements.Add(new RunnerMovement(situation.Batter.PlayerId, 0, 1, false));
                for (int b = 3; b >= 1; b--)
                {
                    RunnerProfile runner = situation.RunnerOn(b);
                    if (runner == null)
                    {
                        continue;
                    }

                    bool forced = situation.IsForced(b);
                    result.Movements.Add(new RunnerMovement(runner.PlayerId, b, forced ? b + 1 : b, b == lead));
                }

                if (situation.OutsBefore + 1 >= _config.Rules.OutsPerHalfInning)
                {
                    result.RunsNullified = true;
                }

                return result;
            }

            // 번트 뜬공: 타자 아웃, 주자 그대로
            result.Outcome = PlateAppearanceOutcome.PopOut;
            result.BallType = BattedBallType.PopUp;
            result.OutsRecorded = 1;
            result.Movements.Add(new RunnerMovement(situation.Batter.PlayerId, 0, 0, true));
            Advance(situation, result, 0, false);
            return result;
        }

        /// <summary>모든 주자를 bases만큼 진루 (includeBatter면 타자는 1루)</summary>
        private static void Advance(PlaySituation situation, PlayResult result, int bases, bool includeBatter)
        {
            for (int b = 3; b >= 1; b--)
            {
                RunnerProfile runner = situation.RunnerOn(b);
                if (runner != null)
                {
                    result.Movements.Add(new RunnerMovement(runner.PlayerId, b, System.Math.Min(4, b + bases), false));
                }
            }

            if (includeBatter)
            {
                result.Movements.Add(new RunnerMovement(situation.Batter.PlayerId, 0, 1, false));
            }
        }

        private BattedBall DisplayBall(IRandomSource random)
        {
            BuntConfig bc = _config.Bunt;
            double side = random.NextDouble() < 0.5 ? -1.0 : 1.0;
            return new BattedBall
            {
                ExitVelocityKmh = random.Range(bc.MinExitVelocityKmh, bc.MaxExitVelocityKmh),
                LaunchAngleDeg = random.Range(-35.0, -15.0),
                SprayAngleDeg = side * random.Range(15.0, 38.0),
                IsSolid = false,
            };
        }
    }
}
