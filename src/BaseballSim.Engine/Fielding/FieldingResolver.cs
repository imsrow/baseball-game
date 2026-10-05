using System;
using System.Collections.Generic;
using BaseballSim.Engine.Batting;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Probability;
using BaseballSim.Engine.Randomness;
using BaseballSim.Engine.Units;

namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 인플레이 타구 판정: 타구 비행/진행 → 수비수 도달·포구 → 송구 → 주자 진루
    /// </summary>
    public sealed class FieldingResolver
    {
        private readonly LeagueConfig _config;
        private readonly FieldingConfig _fc;
        private readonly FieldGeometry _field;
        private readonly BallFlight _flight;
        private readonly BaserunningModel _running;
        private readonly FieldingProbabilities _probabilities;
        private readonly HitAdvancementResolver _hitAdvancement;

        public FieldingResolver(LeagueConfig config, FieldGeometry field)
        {
            _config = config;
            _fc = config.Fielding;
            _field = field;
            _flight = new BallFlight(config.Physics, field, config.Environment);
            _running = new BaserunningModel(config.Baserunning);
            _probabilities = new FieldingProbabilities(config.Fielding);
            _hitAdvancement = new HitAdvancementResolver(config.Fielding, _running, field, _probabilities);
        }

        public BallFlight Flight => _flight;

        public PlayResult Resolve(BattedBall ball, PlaySituation situation, IRandomSource random)
        {
            var result = new PlayResult { BallType = ball.Type };
            if (ball.LaunchAngleDeg < _fc.GroundBallMaxLaunchAngleDeg)
            {
                double horizontal = UnitConversion.KmhToMetersPerSecond(ball.ExitVelocityKmh)
                    * Math.Cos(ball.LaunchAngleDeg * Math.PI / 180.0);
                double retention = Math.Max(_fc.GroundBallMinRetention, _fc.GroundBallSpeedRetention
                    - _fc.GroundBallRetentionLossPerDeg * Math.Max(0, -ball.LaunchAngleDeg));
                var path = new GroundPath(_field.Base(0), 0, ball.SprayAngleDeg, horizontal * retention,
                    _fc.GroundBallDecelerationMps2);
                ResolveGroundPath(path, situation, result, random);
                return result;
            }

            BallPhysicsConfig physics = _config.Physics;
            double lift = ball.IsSolid ? 1.0 : physics.WeakContactLiftMultiplier;
            double carry = Math.Max(physics.CarryNoiseMin,
                Math.Min(physics.CarryNoiseMax, 1.0 + random.NextGaussian() * physics.CarryNoiseSd));
            FlightResult flight = _flight.Simulate(ball.ExitVelocityKmh, ball.LaunchAngleDeg, ball.SprayAngleDeg, lift, carry);
            result.Flight = flight;
            result.HangTimeS = flight.CatchTimeS;
            if (flight.IsHomeRun)
            {
                ResolveHomeRun(situation, result);
                result.BallEndPoint = flight.LandingPoint;
                result.LandingTimeS = flight.LandingTimeS;
                return result;
            }

            ResolveAirBall(ball, flight, situation, result, random);
            return result;
        }

        // ───────────────────────── 뜬공 ─────────────────────────

        private void ResolveAirBall(BattedBall ball, FlightResult flight, PlaySituation situation, PlayResult result,
            IRandomSource random)
        {
            FielderProfile catcher = null;
            double catchProbability = 0;
            double catcherTime = 0;
            // 펜스에 맞는 타구도 그 전에 포구 높이에 있으면 잡을 수 있다 (펜스 앞은 추가 시간)
            if (!flight.HitWall || flight.CatchAtWall)
            {
                foreach (FielderProfile fielder in situation.Defense.All)
                {
                    double timeNeeded = fielder.TimeToReachAirBall(flight.CatchPoint);
                    if (fielder.IsOutfielder && ball.Type == BattedBallType.LineDrive)
                    {
                        timeNeeded += _fc.OutfieldLineDriveExtraReactionS;
                    }

                    if (flight.CatchAtWall)
                    {
                        timeNeeded += _fc.WallCatchExtraS;
                    }

                    double p = LogOdds.Logistic((flight.CatchTimeS - timeNeeded) / _fc.CatchProbabilityScaleS);
                    if (p > catchProbability)
                    {
                        catchProbability = p;
                        catcher = fielder;
                        catcherTime = timeNeeded;
                    }
                }
            }

            if (catcher != null && random.NextDouble() < catchProbability)
            {
                result.FieldedBy = catcher.Position;
                result.BallEndPoint = flight.CatchPoint;
                result.FieldedTimeS = flight.CatchTimeS;
                result.FielderArrivalS = catcherTime;
                if (random.NextDouble() < _probabilities.AirBallError(catcher, 1.0 - catchProbability))
                {
                    ReachedOnError(situation, result);
                    return;
                }

                ResolveCaughtAirBall(ball, flight, catcher, situation, result, random);
                return;
            }

            result.LandingPoint = flight.LandingPoint;
            result.LandingTimeS = flight.LandingTimeS;

            // 내야에 떨어진 타구는 땅볼처럼 계속 굴러간다
            if (!flight.HitWall && flight.LandingPoint.DistanceFromHome < _fc.InfieldInterceptLimitM)
            {
                var path = new GroundPath(flight.LandingPoint, flight.LandingTimeS, ball.SprayAngleDeg,
                    flight.LandingHorizontalSpeedMps * _fc.GroundBallSpeedRetention, _fc.GroundBallDecelerationMps2);
                ResolveGroundPath(path, situation, result, random);
                return;
            }

            // 외야 안타: 떨어진 공은 계속 굴러가고 (펜스에서 멈춤) 야수가 쫓아가 회수한다
            GroundIntercept retrieval;
            if (flight.HitWall)
            {
                retrieval = RetrieveAtWall(flight.LandingPoint, flight.LandingTimeS, situation.Defense.All, random);
            }
            else
            {
                var roll = new GroundPath(flight.LandingPoint, flight.LandingTimeS, ball.SprayAngleDeg,
                    flight.LandingHorizontalSpeedMps * _fc.OutfieldLandingSpeedRetention, _fc.OutfieldRollDecelerationMps2);
                retrieval = RetrieveRollingBall(roll, new List<FielderProfile>(situation.Defense.All), ball.SprayAngleDeg, random);
            }

            result.FieldedBy = retrieval.Fielder.Position;
            result.BallEndPoint = retrieval.Point;
            result.FieldedTimeS = retrieval.BallTimeS;
            result.FielderArrivalS = retrieval.Fielder.TimeToReach(retrieval.Point);
            bool delayed = situation.OutsBefore < _config.Rules.OutsPerHalfInning - 1;
            double batterDelay = _running.Config.BatterRoutineFlyDelayS * catchProbability;
            HitAdvanceOutcome advance = _hitAdvancement.Resolve(situation, retrieval.Fielder, retrieval.Point,
                retrieval.BallTimeS, delayed, result, random, batterDelay);
            result.Outcome = HitOutcome(advance.BatterSafeBase);
        }

        /// <summary>
        /// 굴러가는 공 회수: 펜스 전에 잡으면 그 지점, 아니면 펜스에서 멈춘 공을 처리.
        /// 반환값의 BallTimeS는 공을 집어 든 시각.
        /// </summary>
        private GroundIntercept RetrieveRollingBall(GroundPath path, List<FielderProfile> fielders, double sprayDeg,
            IRandomSource random)
        {
            double fence = _field.FenceDistance(sprayDeg);
            GroundIntercept intercept = FindEarliestIntercept(path, fielders, fence);
            if (intercept != null)
            {
                double pickup = _fc.OutfieldPickupS + _fc.OutfieldChasePickupExtraS * ChaseFactor(intercept, sprayDeg);
                return new GroundIntercept(intercept.Fielder, intercept.Point, intercept.BallTimeS + pickup, intercept.MarginS);
            }

            FieldPoint wall = FieldPoint.FromPolar(fence, sprayDeg);
            double reachWall = path.TimeAt(Math.Max(0, FieldPoint.Distance(path.Origin, wall)));
            if (double.IsInfinity(reachWall))
            {
                reachWall = path.StopTimeS;
            }

            return RetrieveAtWall(wall, reachWall, fielders, random);
        }

        /// <summary>
        /// 야수가 공과 같은 방향으로 달려가 잡았는지: 정면으로 달려 나옴 0, 옆으로 끊음 0.5, 뒤에서 쫓아감 1.
        /// 제자리 근처에서 잡으면 공이 야수 쪽으로 오는 것이라 0
        /// </summary>
        private static double ChaseFactor(GroundIntercept intercept, double sprayDeg)
        {
            double dx = intercept.Point.X - intercept.Fielder.Start.X;
            double dy = intercept.Point.Y - intercept.Fielder.Start.Y;
            double run = Math.Sqrt(dx * dx + dy * dy);
            if (run < intercept.Fielder.ReachM)
            {
                return 0;
            }

            double rad = sprayDeg * Math.PI / 180.0;
            double dot = (dx * Math.Sin(rad) + dy * Math.Cos(rad)) / run;
            return (1.0 + dot) * 0.5;
        }

        private GroundIntercept RetrieveAtWall(FieldPoint wall, double ballAtWallS, IEnumerable<FielderProfile> fielders,
            IRandomSource random)
        {
            FielderProfile best = null;
            double bestTime = double.PositiveInfinity;
            foreach (FielderProfile fielder in fielders)
            {
                double t = fielder.TimeToReach(wall);
                if (t < bestTime)
                {
                    bestTime = t;
                    best = fielder;
                }
            }

            double carom = _fc.WallCaromDelayS + random.NextDouble() * _fc.WallCaromRandomS;
            return new GroundIntercept(best, wall, Math.Max(bestTime, ballAtWallS) + carom, 0);
        }

        private void ResolveCaughtAirBall(BattedBall ball, FlightResult flight, FielderProfile catcher,
            PlaySituation situation, PlayResult result, IRandomSource random)
        {
            result.OutsRecorded = 1;
            result.Movements.Add(new RunnerMovement(situation.Batter.PlayerId, 0, 0, true));
            switch (ball.Type)
            {
                case BattedBallType.PopUp:
                    result.Outcome = PlateAppearanceOutcome.PopOut;
                    break;
                case BattedBallType.LineDrive:
                    result.Outcome = PlateAppearanceOutcome.LineOut;
                    break;
                default:
                    result.Outcome = PlateAppearanceOutcome.FlyOut;
                    break;
            }

            if (situation.OutsBefore + 1 >= _config.Rules.OutsPerHalfInning)
            {
                AddStationaryRunners(situation, result);
                return;
            }

            // 태그업
            var finals = new int[4];
            int limit = 5;
            int bestAttempt = -1;
            double bestMargin = double.PositiveInfinity;
            for (int b = 3; b >= 1; b--)
            {
                RunnerProfile runner = situation.RunnerOn(b);
                if (runner == null)
                {
                    continue;
                }

                int chosen = b;
                int target = b + 1;
                if (target <= Math.Min(4, limit - 1))
                {
                    double arrival = flight.CatchTimeS + _running.Config.TagUpReactionS + _running.TimeToBase(runner, b, target);
                    double throwArrival = flight.CatchTimeS + catcher.TransferS
                        + catcher.ThrowTime(flight.CatchPoint, _field.Base(target));
                    double trueMargin = throwArrival - arrival;
                    double estimate = trueMargin + random.NextGaussian() * _running.EstimateNoise(runner);
                    if (estimate > _running.RequiredMargin(runner, situation.BaserunningStyle))
                    {
                        chosen = target;
                        if (trueMargin < bestMargin)
                        {
                            bestMargin = trueMargin;
                            bestAttempt = b;
                        }
                    }
                }

                finals[b] = chosen;
                limit = chosen == 4 ? 5 : chosen;
            }

            int outRunner = -1;
            if (bestAttempt > 0)
            {
                double distance = FieldPoint.Distance(flight.CatchPoint, _field.Base(finals[bestAttempt]));
                if (random.NextDouble() < _probabilities.ThrowError(catcher, distance))
                {
                    result.IsError = true;
                    for (int b = 1; b <= 3; b++)
                    {
                        if (finals[b] > 0)
                        {
                            finals[b] = Math.Min(4, finals[b] + 1);
                        }
                    }
                }
                else if (bestMargin + random.NextGaussian() * _fc.PlayTimingNoiseS < 0)
                {
                    outRunner = bestAttempt;
                }
            }

            bool runnerScored = false;
            for (int b = 3; b >= 1; b--)
            {
                RunnerProfile runner = situation.RunnerOn(b);
                if (runner == null)
                {
                    continue;
                }

                bool isOut = b == outRunner;
                result.Movements.Add(new RunnerMovement(runner.PlayerId, b, finals[b], isOut));
                if (isOut)
                {
                    result.OutsRecorded++;
                }
                else if (finals[b] == 4)
                {
                    runnerScored = true;
                }
            }

            bool outfieldCatch = PositionInfo.IsOutfield(catcher.Position);
            if (runnerScored && outfieldCatch && ball.Type != BattedBallType.PopUp)
            {
                result.Outcome = PlateAppearanceOutcome.SacrificeFly;
            }
        }

        // ───────────────────────── 땅볼 ─────────────────────────

        private void ResolveGroundPath(GroundPath path, PlaySituation situation, PlayResult result, IRandomSource random)
        {
            var infielders = new List<FielderProfile>();
            var outfielders = new List<FielderProfile>();
            foreach (FielderProfile fielder in situation.Defense.All)
            {
                if (fielder.IsOutfielder)
                {
                    outfielders.Add(fielder);
                }
                else
                {
                    infielders.Add(fielder);
                }
            }

            GroundIntercept intercept = FindIntercept(path, infielders, _fc.InfieldInterceptLimitM);
            if (intercept != null)
            {
                ResolveInfieldPlay(intercept, situation, result, random);
                return;
            }

            // 내야를 빠져나간 땅볼 안타
            GroundIntercept outfield = RetrieveRollingBall(path, outfielders, path.DirectionDeg, random);
            result.FieldedBy = outfield.Fielder.Position;
            result.BallEndPoint = outfield.Point;
            result.FieldedTimeS = outfield.BallTimeS;
            result.FielderArrivalS = outfield.Fielder.TimeToReach(outfield.Point);
            HitAdvanceOutcome advance = _hitAdvancement.Resolve(situation, outfield.Fielder, outfield.Point,
                outfield.BallTimeS, false, result, random);
            result.Outcome = HitOutcome(advance.BatterSafeBase);
        }

        /// <summary>
        /// 각 야수의 포구 지점을 정하고, 가장 먼저 공을 잡는 야수를 고른다.
        /// 1) 타구 경로에서 야수와 가장 가까운 지점(정면)에 먼저 도착할 수 있으면 그 지점에서 여유 있게 처리
        /// 2) 아니면 그 지점부터 경로를 따라가며 쫓아가 잡을 수 있는 첫 지점 (빠듯한 처리)
        /// 3) 공이 야수 앞에서 멈추면 앞으로 달려 나와 가장 이른 지점에서 처리
        /// </summary>
        private GroundIntercept FindIntercept(GroundPath path, List<FielderProfile> fielders, double maxDistanceFromHome)
        {
            GroundIntercept best = null;
            foreach (FielderProfile fielder in fielders)
            {
                GroundIntercept found = FielderIntercept(path, fielder, maxDistanceFromHome);
                if (found != null && (best == null || found.BallTimeS < best.BallTimeS))
                {
                    best = found;
                }
            }

            return best;
        }

        /// <summary>
        /// 외야 회수: 각 야수가 공을 향해 달려 나와 가장 이르게 잡을 수 있는 지점.
        /// (내야 땅볼처럼 정면에서 기다리면 외야에선 공이 굴러올 때까지 서 있게 된다)
        /// </summary>
        private GroundIntercept FindEarliestIntercept(GroundPath path, List<FielderProfile> fielders, double maxDistanceFromHome)
        {
            GroundIntercept best = null;
            double stop = path.StopDistanceM;
            foreach (FielderProfile fielder in fielders)
            {
                GroundIntercept found = null;
                for (double d = 0; d < stop; d += _fc.InterceptScanStepM)
                {
                    if (path.PointAt(d).DistanceFromHome > maxDistanceFromHome)
                    {
                        break;
                    }

                    found = TryIntercept(path, fielder, d, maxDistanceFromHome);
                    if (found != null)
                    {
                        break;
                    }
                }

                if (found == null)
                {
                    FieldPoint stopPoint = path.PointAt(stop);
                    if (stopPoint.DistanceFromHome <= maxDistanceFromHome)
                    {
                        found = new GroundIntercept(fielder, stopPoint, Math.Max(fielder.TimeToReach(stopPoint), path.StopTimeS), 0);
                    }
                }

                if (found != null && (best == null || found.BallTimeS < best.BallTimeS))
                {
                    best = found;
                }
            }

            return best;
        }

        private GroundIntercept FielderIntercept(GroundPath path, FielderProfile fielder, double maxDistanceFromHome)
        {
            double stop = path.StopDistanceM;
            double step = _fc.InterceptScanStepM;
            double rad = path.DirectionDeg * Math.PI / 180.0;
            double projection = (fielder.Start.X - path.Origin.X) * Math.Sin(rad) + (fielder.Start.Y - path.Origin.Y) * Math.Cos(rad);
            double front = Math.Max(0, Math.Min(stop, projection));

            if (front < stop)
            {
                // 정면 처리
                GroundIntercept direct = TryIntercept(path, fielder, front, maxDistanceFromHome);
                if (direct != null)
                {
                    return direct;
                }

                // 쫓아가며 처리
                for (double d = front + step; d < stop; d += step)
                {
                    if (path.PointAt(d).DistanceFromHome > maxDistanceFromHome)
                    {
                        return null;
                    }

                    GroundIntercept chase = TryIntercept(path, fielder, d, maxDistanceFromHome);
                    if (chase != null)
                    {
                        return chase;
                    }
                }
            }
            else
            {
                // 야수 앞에서 공이 멈춤: 달려 나와 처리
                for (double d = 0; d < stop; d += step)
                {
                    GroundIntercept charge = TryIntercept(path, fielder, d, maxDistanceFromHome);
                    if (charge != null)
                    {
                        return new GroundIntercept(fielder, charge.Point, charge.BallTimeS, 0);
                    }
                }
            }

            // 멈춘 공 처리
            FieldPoint stopPoint = path.PointAt(stop);
            if (stopPoint.DistanceFromHome > maxDistanceFromHome)
            {
                return null;
            }

            double t = Math.Max(fielder.TimeToReach(stopPoint), path.StopTimeS);
            return new GroundIntercept(fielder, stopPoint, t, 0);
        }

        private static GroundIntercept TryIntercept(GroundPath path, FielderProfile fielder, double distance, double maxDistanceFromHome)
        {
            FieldPoint point = path.PointAt(distance);
            if (point.DistanceFromHome > maxDistanceFromHome)
            {
                return null;
            }

            double ballTime = path.TimeAt(distance);
            double fielderTime = fielder.TimeToReach(point);
            return fielderTime <= ballTime ? new GroundIntercept(fielder, point, ballTime, ballTime - fielderTime) : null;
        }

        private void ResolveInfieldPlay(GroundIntercept intercept, PlaySituation situation, PlayResult result,
            IRandomSource random)
        {
            FielderProfile fielder = intercept.Fielder;
            FieldPoint point = intercept.Point;
            double difficulty = Math.Max(0, Math.Min(1, 1.0 - intercept.MarginS / _fc.DifficultPlayWindowS));
            result.FieldedBy = fielder.Position;
            result.BallEndPoint = point;
            result.FieldedTimeS = intercept.BallTimeS;
            result.FielderArrivalS = fielder.TimeToReach(point);

            if (random.NextDouble() < _probabilities.GroundBallError(fielder, difficulty))
            {
                ReachedOnError(situation, result);
                return;
            }

            double fieldTime = intercept.BallTimeS;
            double readyTime = fieldTime + fielder.TransferS + _fc.DifficultyTransferExtraS * difficulty;
            double batterArrival = _running.HomeToFirst(situation.Batter);
            double marginAtFirst = batterArrival - BallArrivalAtBase(fielder, point, fieldTime, readyTime, 1);

            RunnerProfile runnerOnFirst = situation.RunnerOn(1);
            bool forceAtSecond = false;
            double marginAtSecond = 0;
            if (runnerOnFirst != null)
            {
                double runnerArrival = _running.TimeToBase(runnerOnFirst, 1, 2);
                marginAtSecond = runnerArrival - BallArrivalAtBase(fielder, point, fieldTime, readyTime, 2);
                forceAtSecond = marginAtSecond > _fc.LeadForceMarginS;
                if (situation.OutsBefore == _config.Rules.OutsPerHalfInning - 1)
                {
                    // 2아웃이면 더 확실한 쪽으로
                    forceAtSecond = forceAtSecond && marginAtSecond > marginAtFirst;
                }
            }

            if (forceAtSecond)
            {
                ResolveForceAtSecond(fielder, point, fieldTime, readyTime, batterArrival, marginAtSecond, situation,
                    result, random);
            }
            else
            {
                ResolveThrowToFirst(fielder, point, marginAtFirst, situation, result, random);
            }
        }

        /// <summary>송구(또는 직접 달려가 밟기)로 공이 베이스에 도착하는 시각</summary>
        private double BallArrivalAtBase(FielderProfile fielder, FieldPoint point, double fieldTime, double readyTime, int baseNumber)
        {
            FieldPoint target = _field.Base(baseNumber);
            double byThrow = readyTime + fielder.ThrowTime(point, target);
            double byFoot = fieldTime + FieldPoint.Distance(point, target) / fielder.SpeedMps;
            return Math.Min(byThrow, byFoot);
        }

        private void ResolveForceAtSecond(FielderProfile fielder, FieldPoint point, double fieldTime, double readyTime,
            double batterArrival, double marginAtSecond, PlaySituation situation, PlayResult result, IRandomSource random)
        {
            int outsAllowed = _config.Rules.OutsPerHalfInning;
            if (marginAtSecond + random.NextGaussian() * _fc.PlayTimingNoiseS <= 0)
            {
                // 선행 주자도 살고 타자도 산 야수선택
                result.Outcome = PlateAppearanceOutcome.FieldersChoice;
                result.Movements.Add(new RunnerMovement(situation.Batter.PlayerId, 0, 1, false));
                AddForcedAdvances(situation, result);
                return;
            }

            result.OutsRecorded = 1;
            result.Movements.Add(new RunnerMovement(situation.RunnerOn(1).PlayerId, 1, 2, true));

            bool batterOut = false;
            int batterBase = 1;
            if (situation.OutsBefore + 1 < outsAllowed)
            {
                Position pivotPosition = fielder.Position == Position.SecondBase || fielder.Position == Position.FirstBase
                    ? Position.Shortstop
                    : Position.SecondBase;
                FielderProfile pivot = situation.Defense.Get(pivotPosition);
                double atSecond = BallArrivalAtBase(fielder, point, fieldTime, readyTime, 2);
                double relay = atSecond + _fc.PivotS + pivot.ThrowTime(_field.Base(2), _field.Base(1));
                double relayDistance = FieldPoint.Distance(_field.Base(2), _field.Base(1));
                if (random.NextDouble() < _probabilities.ThrowError(pivot, relayDistance))
                {
                    result.IsError = true;
                    batterBase = 2;
                }
                else if (batterArrival - relay + random.NextGaussian() * _fc.PlayTimingNoiseS > 0)
                {
                    batterOut = true;
                }
            }

            if (batterOut)
            {
                result.OutsRecorded++;
                result.Movements.Add(new RunnerMovement(situation.Batter.PlayerId, 0, 1, true));
                result.Outcome = PlateAppearanceOutcome.GroundedIntoDoublePlay;
            }
            else
            {
                result.Movements.Add(new RunnerMovement(situation.Batter.PlayerId, 0, batterBase, false));
                result.Outcome = PlateAppearanceOutcome.FieldersChoice;
            }

            // 2루·3루 주자: 포스면 진루, 아니면 상황에 따라
            int outsAfter = situation.OutsBefore + result.OutsRecorded;
            for (int b = 3; b >= 2; b--)
            {
                RunnerProfile runner = situation.RunnerOn(b);
                if (runner == null)
                {
                    continue;
                }

                int to = situation.IsForced(b) ? b + 1 : b;
                if (to == b && b == 3 && outsAfter < outsAllowed
                    && random.NextDouble() < _running.Config.ThirdScoresOnGroundOutProbability)
                {
                    to = 4;
                }

                result.Movements.Add(new RunnerMovement(runner.PlayerId, b, to, false));
            }

            if (outsAfter >= outsAllowed)
            {
                result.RunsNullified = true;
            }
        }

        private void ResolveThrowToFirst(FielderProfile fielder, FieldPoint point, double marginAtFirst,
            PlaySituation situation, PlayResult result, IRandomSource random)
        {
            int outsAllowed = _config.Rules.OutsPerHalfInning;
            double distance = FieldPoint.Distance(point, _field.Base(1));
            if (random.NextDouble() < _probabilities.ThrowError(fielder, distance))
            {
                // 1루 악송구: 타자 2루, 주자 2베이스씩
                result.IsError = true;
                result.Outcome = PlateAppearanceOutcome.ReachedOnError;
                result.Movements.Add(new RunnerMovement(situation.Batter.PlayerId, 0, 2, false));
                for (int b = 3; b >= 1; b--)
                {
                    RunnerProfile runner = situation.RunnerOn(b);
                    if (runner != null)
                    {
                        result.Movements.Add(new RunnerMovement(runner.PlayerId, b, Math.Min(4, b + 2), false));
                    }
                }

                return;
            }

            bool batterOut = marginAtFirst + random.NextGaussian() * _fc.PlayTimingNoiseS > 0;
            if (!batterOut)
            {
                // 내야안타
                result.Outcome = PlateAppearanceOutcome.Single;
                result.Movements.Add(new RunnerMovement(situation.Batter.PlayerId, 0, 1, false));
                AddForcedAdvances(situation, result);
                return;
            }

            result.Outcome = PlateAppearanceOutcome.GroundOut;
            result.OutsRecorded = 1;
            result.Movements.Add(new RunnerMovement(situation.Batter.PlayerId, 0, 1, true));
            int outsAfter = situation.OutsBefore + 1;
            if (outsAfter >= outsAllowed)
            {
                result.RunsNullified = true;
                AddStationaryRunners(situation, result);
                return;
            }

            // 포스 주자는 진루, 비포스 주자는 확률적으로 진루
            bool thirdOccupied = false;
            for (int b = 3; b >= 1; b--)
            {
                RunnerProfile runner = situation.RunnerOn(b);
                if (runner == null)
                {
                    continue;
                }

                int to = b;
                if (situation.IsForced(b))
                {
                    to = b + 1;
                }
                else if (b == 3 && random.NextDouble() < _running.Config.ThirdScoresOnGroundOutProbability)
                {
                    to = 4;
                }
                else if (b == 2 && !thirdOccupied && random.NextDouble() < _running.Config.SecondAdvancesOnGroundOutProbability)
                {
                    to = 3;
                }

                if (to == 3)
                {
                    thirdOccupied = true;
                }

                result.Movements.Add(new RunnerMovement(runner.PlayerId, b, to, false));
            }
        }

        // ───────────────────────── 공통 ─────────────────────────

        private static void ResolveHomeRun(PlaySituation situation, PlayResult result)
        {
            result.Outcome = PlateAppearanceOutcome.HomeRun;
            for (int b = 3; b >= 1; b--)
            {
                RunnerProfile runner = situation.RunnerOn(b);
                if (runner != null)
                {
                    result.Movements.Add(new RunnerMovement(runner.PlayerId, b, 4, false));
                }
            }

            result.Movements.Add(new RunnerMovement(situation.Batter.PlayerId, 0, 4, false));
        }

        /// <summary>실책 출루: 타자 1루, 모든 주자 한 베이스 진루</summary>
        private static void ReachedOnError(PlaySituation situation, PlayResult result)
        {
            result.IsError = true;
            result.Outcome = PlateAppearanceOutcome.ReachedOnError;
            for (int b = 3; b >= 1; b--)
            {
                RunnerProfile runner = situation.RunnerOn(b);
                if (runner != null)
                {
                    result.Movements.Add(new RunnerMovement(runner.PlayerId, b, Math.Min(4, b + 1), false));
                }
            }

            result.Movements.Add(new RunnerMovement(situation.Batter.PlayerId, 0, 1, false));
        }

        /// <summary>타자가 1루로 갈 때 밀려나는 주자만 진루, 나머지는 제자리</summary>
        private static void AddForcedAdvances(PlaySituation situation, PlayResult result)
        {
            for (int b = 3; b >= 1; b--)
            {
                RunnerProfile runner = situation.RunnerOn(b);
                if (runner == null)
                {
                    continue;
                }

                int to = situation.IsForced(b) ? b + 1 : b;
                result.Movements.Add(new RunnerMovement(runner.PlayerId, b, to, false));
            }
        }

        private static void AddStationaryRunners(PlaySituation situation, PlayResult result)
        {
            for (int b = 3; b >= 1; b--)
            {
                RunnerProfile runner = situation.RunnerOn(b);
                if (runner != null)
                {
                    result.Movements.Add(new RunnerMovement(runner.PlayerId, b, b, false));
                }
            }
        }

        private static PlateAppearanceOutcome HitOutcome(int batterBase)
        {
            switch (batterBase)
            {
                case 2: return PlateAppearanceOutcome.Double;
                case 3: return PlateAppearanceOutcome.Triple;
                case 4: return PlateAppearanceOutcome.HomeRun;
                default: return PlateAppearanceOutcome.Single;
            }
        }
    }
}
