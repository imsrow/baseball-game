using System;
using System.Collections.Generic;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Randomness;

namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 외야로 빠진 안타의 주자·타자 진루 판정.
    /// 각 주자는 앞 주자부터 차례로, 예상 여유 시간(송구 도착 − 주자 도착 + 판단 오차)이
    /// 필요 여유보다 크면 한 베이스씩 더 간다. 야수는 가장 잡기 쉬운 주자 한 명에게 송구한다.
    /// </summary>
    public sealed class HitAdvancementResolver
    {
        private readonly FieldingConfig _fielding;
        private readonly BaserunningModel _running;
        private readonly FieldGeometry _field;
        private readonly FieldingProbabilities _probabilities;

        public HitAdvancementResolver(FieldingConfig fielding, BaserunningModel running, FieldGeometry field,
            FieldingProbabilities probabilities)
        {
            _fielding = fielding;
            _running = running;
            _field = field;
            _probabilities = probabilities;
        }

        /// <param name="runnersDelayed">2아웃 미만 뜬공처럼 주자가 타구 확인 후 출발하는지</param>
        public HitAdvanceOutcome Resolve(PlaySituation situation, FielderProfile fielder, FieldPoint pickPoint,
            double pickTimeS, bool runnersDelayed, PlayResult result, IRandomSource random)
        {
            var finals = new int[4];
            var attempts = new List<AdvanceAttempt>();
            double delay = runnersDelayed ? _running.Config.FlyBallHoldDelayS : 0;
            int limit = 5;

            for (int b = 3; b >= 0; b--)
            {
                RunnerProfile runner = b == 0 ? situation.Batter : situation.RunnerOn(b);
                if (runner == null)
                {
                    continue;
                }

                int minBase = b == 0 ? 1 : (situation.IsForced(b) ? b + 1 : b);
                int maxBase = Math.Min(4, limit - 1);
                minBase = Math.Min(minBase, maxBase);
                double start = b == 0 ? 0 : delay;

                int chosen = minBase;
                double chosenMargin = double.PositiveInfinity;
                for (int target = minBase + 1; target <= maxBase; target++)
                {
                    double arrival = start + _running.TimeToBase(runner, b, target);
                    double throwArrival = pickTimeS + fielder.TransferS + fielder.ThrowTime(pickPoint, _field.Base(target));
                    double trueMargin = throwArrival - arrival;
                    double estimate = trueMargin + random.NextGaussian() * _running.EstimateNoise(runner);
                    if (estimate <= _running.RequiredMargin(runner))
                    {
                        break;
                    }

                    chosen = target;
                    chosenMargin = trueMargin;
                }

                finals[b] = chosen;
                if (chosen > minBase)
                {
                    attempts.Add(new AdvanceAttempt(b, chosen, chosenMargin));
                }

                limit = chosen == 4 ? 5 : chosen;
            }

            int batterSafeBase = finals[0];
            int outRunner = -1;
            if (attempts.Count > 0)
            {
                AdvanceAttempt target = attempts[0];
                foreach (AdvanceAttempt attempt in attempts)
                {
                    if (attempt.TrueMargin < target.TrueMargin)
                    {
                        target = attempt;
                    }
                }

                double throwDistance = FieldPoint.Distance(pickPoint, _field.Base(target.TargetBase));
                if (random.NextDouble() < _probabilities.ThrowError(fielder, throwDistance))
                {
                    // 송구 실책: 모든 주자 한 베이스 추가 진루
                    result.IsError = true;
                    for (int b = 0; b <= 3; b++)
                    {
                        if (finals[b] > 0)
                        {
                            finals[b] = Math.Min(4, finals[b] + 1);
                        }
                    }
                }
                else
                {
                    double actual = target.TrueMargin + random.NextGaussian() * _fielding.PlayTimingNoiseS;
                    if (actual < 0)
                    {
                        outRunner = target.FromBase;
                    }
                }
            }

            bool batterOut = false;
            for (int b = 3; b >= 0; b--)
            {
                RunnerProfile runner = b == 0 ? situation.Batter : situation.RunnerOn(b);
                if (runner == null)
                {
                    continue;
                }

                bool isOut = b == outRunner;
                result.Movements.Add(new RunnerMovement(runner.PlayerId, b, finals[b], isOut));
                if (isOut)
                {
                    result.OutsRecorded++;
                    if (b == 0)
                    {
                        batterOut = true;
                        batterSafeBase = finals[0] - 1;
                    }
                }
            }

            return new HitAdvanceOutcome(batterSafeBase, batterOut);
        }

        private readonly struct AdvanceAttempt
        {
            public AdvanceAttempt(int fromBase, int targetBase, double trueMargin)
            {
                FromBase = fromBase;
                TargetBase = targetBase;
                TrueMargin = trueMargin;
            }

            public int FromBase { get; }

            public int TargetBase { get; }

            public double TrueMargin { get; }
        }
    }
}
