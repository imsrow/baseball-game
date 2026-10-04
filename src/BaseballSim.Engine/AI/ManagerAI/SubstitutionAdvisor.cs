using System;
using System.Collections.Generic;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.State;
using BaseballSim.Engine.Teams;

namespace BaseballSim.Engine.AI.ManagerAI
{
    /// <summary>
    /// 대타·대주자·대수비 판단
    /// </summary>
    public static class SubstitutionAdvisor
    {
        /// <summary>대타: 후반, 점수 차 이내, 벤치 타자가 현재 타자보다 확실히 나을 때 (포수 자리는 벤치에 포수가 있을 때만)</summary>
        public static ManagerAction PinchHit(ManagerContext context)
        {
            GameState state = context.State;
            ManagerAiConfig mc = context.Config.ManagerAi;
            if (state.Inning < mc.LateInning || Math.Abs(GameSituation.RunDifference(context)) > mc.PinchHitMaxRunDifference)
            {
                return null;
            }

            TeamGameState team = context.Team;
            LineupSlot slot = team.Lineup[team.NextBatterIndex];
            if (slot.Position == Position.Catcher && !BenchHas(context, Position.Catcher))
            {
                return null;
            }

            Hand throws = context.Players.Get(context.Opponent.CurrentPitcherId).Throws;
            double current = PlayerValue.BattingZAgainst(context.Players.Get(slot.PlayerId), throws, mc);
            int best = -1;
            double bestValue = double.NegativeInfinity;
            foreach (int id in team.Bench)
            {
                double value = PlayerValue.BattingZAgainst(context.Players.Get(id), throws, mc);
                if (value > bestValue)
                {
                    bestValue = value;
                    best = id;
                }
            }

            return best >= 0 && bestValue - current >= mc.PinchHitMinGapZ ? ManagerAction.PinchHit(best, slot.PlayerId) : null;
        }

        /// <summary>대주자: 후반 접전에서 느린 주자를 훨씬 빠른 벤치 선수로 (앞 주자 우선, 한 명)</summary>
        public static ManagerAction PinchRun(ManagerContext context, int excludedPlayerId)
        {
            GameState state = context.State;
            ManagerAiConfig mc = context.Config.ManagerAi;
            if (state.Inning < mc.PinchRunMinInning || Math.Abs(GameSituation.RunDifference(context)) > mc.CloseRunDifference)
            {
                return null;
            }

            for (int b = 3; b >= 1; b--)
            {
                BaseRunner runner = state.Bases[b - 1];
                if (runner == null || context.Team.LineupIndexOf(runner.PlayerId) < 0)
                {
                    continue;
                }

                int speed = context.Players.Get(runner.PlayerId).Batting.Speed;
                if (speed > mc.PinchRunMaxRunnerSpeed)
                {
                    continue;
                }

                int fastest = -1;
                int fastestSpeed = int.MinValue;
                foreach (int id in context.Team.Bench)
                {
                    int s = context.Players.Get(id).Batting.Speed;
                    if (id != excludedPlayerId && s > fastestSpeed)
                    {
                        fastestSpeed = s;
                        fastest = id;
                    }
                }

                if (fastest >= 0 && fastestSpeed - speed >= mc.PinchRunMinSpeedGap)
                {
                    return ManagerAction.PinchRun(fastest, b);
                }
            }

            return null;
        }

        /// <summary>
        /// 대수비 (반이닝 시작 때만): 수비력이 기준 미만인 포지션 보강, 또는 후반 리드 시 확실히 나은 수비수로 교체
        /// </summary>
        public static List<ManagerAction> DefensiveSubs(ManagerContext context)
        {
            var actions = new List<ManagerAction>();
            GameState state = context.State;
            if (state.PlateAppearancesThisHalf != 0)
            {
                return actions;
            }

            ManagerAiConfig mc = context.Config.ManagerAi;
            DefenseConfig dc = context.Config.Defense;
            bool lateLead = state.Inning >= mc.DefensiveSubMinInning && GameSituation.RunDifference(context) > 0;
            var used = new HashSet<int>();
            foreach (LineupSlot slot in context.Team.Lineup)
            {
                if (slot.Position == Position.DesignatedHitter)
                {
                    continue;
                }

                double current = PlayerValue.DefenseAt(context.Players.Get(slot.PlayerId), slot.Position, dc);
                int best = -1;
                double bestValue = double.NegativeInfinity;
                foreach (int id in context.Team.Bench)
                {
                    if (used.Contains(id))
                    {
                        continue;
                    }

                    double value = PlayerValue.DefenseAt(context.Players.Get(id), slot.Position, dc);
                    if (value > bestValue)
                    {
                        bestValue = value;
                        best = id;
                    }
                }

                bool fix = current < mc.DefensiveFixMinRating && bestValue > current;
                bool upgrade = lateLead && bestValue - current >= mc.DefensiveSubMinGap;
                if (best >= 0 && (fix || upgrade))
                {
                    used.Add(best);
                    actions.Add(ManagerAction.DefensiveSub(best, slot.PlayerId));
                }
            }

            return actions;
        }

        private static bool BenchHas(ManagerContext context, Position position)
        {
            foreach (int id in context.Team.Bench)
            {
                if (context.Players.Get(id).PrimaryPosition == position)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
