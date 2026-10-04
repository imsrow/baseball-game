using System;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;

namespace BaseballSim.Engine.AI.ManagerAI
{
    /// <summary>
    /// 감독 판단용 경기 상황 요약
    /// </summary>
    public static class GameSituation
    {
        /// <summary>결정하는 팀 기준 점수 차 (+면 앞섬)</summary>
        public static int RunDifference(ManagerContext context)
        {
            return context.Team.Runs - context.Opponent.Runs;
        }

        /// <summary>경기 후반 접전인지</summary>
        public static bool IsLateAndClose(ManagerContext context)
        {
            ManagerAiConfig mc = context.Config.ManagerAi;
            return context.State.Inning >= mc.LateInning && Math.Abs(RunDifference(context)) <= mc.CloseRunDifference;
        }
    }
}
