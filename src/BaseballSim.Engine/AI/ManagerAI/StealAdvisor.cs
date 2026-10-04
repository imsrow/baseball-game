using System;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Fielding;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.State;

namespace BaseballSim.Engine.AI.ManagerAI
{
    /// <summary>
    /// 도루 시도 판단: 다음 베이스가 비어 있고, 예상 성공 확률이 기준 이상이면 투구마다 일정 확률로 시도
    /// </summary>
    public static class StealAdvisor
    {
        /// <summary>도루할 주자의 출발 베이스 (시도 안 하면 0)</summary>
        public static int Advise(ManagerContext context)
        {
            GameState state = context.State;
            ManagerAiConfig mc = context.Config.ManagerAi;
            if (Math.Abs(GameSituation.RunDifference(context)) >= mc.StealMaxRunDifference)
            {
                return 0;
            }

            int from;
            double factor;
            if (state.Bases[0] != null && state.Bases[1] == null)
            {
                from = 1;
                factor = 1.0;
            }
            else if (state.Bases[1] != null && state.Bases[2] == null && state.Outs < context.Config.Rules.OutsPerHalfInning - 1)
            {
                from = 2;
                factor = mc.StealThirdAttemptFactor;
            }
            else
            {
                return 0;
            }

            TeamGameState defense = context.Opponent;
            Player runner = context.Players.Get(state.Bases[from - 1].PlayerId);
            Player pitcher = context.Players.Get(defense.CurrentPitcherId);
            Player catcher = context.Players.Get(defense.PlayerIdAt(Position.Catcher));
            double success = new StealModel(context.Config).SuccessProbability(runner.Batting, from + 1, pitcher.Pitching, catcher, null);
            if (success < mc.StealMinSuccess)
            {
                return 0;
            }

            return context.Random.NextDouble() < mc.StealAttemptPerPitch * factor ? from : 0;
        }
    }
}
