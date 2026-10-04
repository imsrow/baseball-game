using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.State;

namespace BaseballSim.Engine.AI.ManagerAI
{
    /// <summary>
    /// 고의4구 판단: 1루가 비어 있고 득점권 주자, 후반 접전, 현재 타자가 다음 타자보다 확실히 강할 때
    /// </summary>
    public static class IntentionalWalkAdvisor
    {
        public static bool Advise(ManagerContext context)
        {
            GameState state = context.State;
            if (state.Bases[0] != null || (state.Bases[1] == null && state.Bases[2] == null))
            {
                return false;
            }

            if (!GameSituation.IsLateAndClose(context))
            {
                return false;
            }

            ManagerAiConfig mc = context.Config.ManagerAi;
            TeamGameState offense = context.Opponent;
            Hand throws = context.Players.Get(context.Team.CurrentPitcherId).Throws;
            Player batter = context.Players.Get(offense.Lineup[offense.NextBatterIndex].PlayerId);
            Player next = context.Players.Get(offense.Lineup[(offense.NextBatterIndex + 1) % offense.Lineup.Count].PlayerId);
            double batterZ = PlayerValue.BattingZAgainst(batter, throws, mc);
            double nextZ = PlayerValue.BattingZAgainst(next, throws, mc);
            return batterZ >= mc.IntentionalWalkMinBatterZ && batterZ - nextZ >= mc.IntentionalWalkMinGapZ;
        }
    }
}
