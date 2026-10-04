using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.State;

namespace BaseballSim.Engine.AI.ManagerAI
{
    /// <summary>
    /// 투수 교체 판단: 피로도·투구 수·상대 타자 수·잡은 아웃 수 기준
    /// </summary>
    public static class PitchingChangeAdvisor
    {
        /// <summary>교체가 필요하면 들어올 투수 ID, 아니면 −1</summary>
        public static int Advise(ManagerContext context)
        {
            TeamGameState team = context.Team;
            if (team.AvailableBullpen.Count == 0)
            {
                return -1;
            }

            PitcherGameState current = team.CurrentPitcher;
            Player pitcher = context.Players.Get(current.PlayerId);
            ManagerAiConfig mc = context.Config.ManagerAi;
            double fatigue = FatigueModel.Fatigue(pitcher.Pitching, current.PitchCount, context.Config.Fatigue);

            bool change = current.IsStarter
                ? fatigue >= mc.StarterFatigueThreshold || current.PitchCount >= mc.StarterMaxPitches
                : fatigue >= mc.RelieverFatigueThreshold || current.BattersFaced >= mc.RelieverMaxBattersFaced
                    || current.OutsRecorded >= mc.RelieverMaxOuts;
            return change ? team.AvailableBullpen[0] : -1;
        }
    }
}
