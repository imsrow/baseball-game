using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.State;

namespace BaseballSim.Engine.AI.ManagerAI
{
    /// <summary>
    /// 투수 교체 판단
    /// 1) 반이닝 시작 때 역할 투입: 세이브 상황이면 마무리, 셋업 이닝 리드면 셋업
    /// 2) 피로도·투구 수·상대 타자 수·잡은 아웃 수 기준 교체 (상황에 맞는 역할 우선, 없으면 일반 불펜)
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

            GameState state = context.State;
            ManagerAiConfig mc = context.Config.ManagerAi;
            PitcherGameState current = team.CurrentPitcher;
            Player pitcher = context.Players.Get(current.PlayerId);
            double fatigue = FatigueModel.Fatigue(pitcher.Pitching, current.PitchCount, context.Config.Fatigue);

            int diff = GameSituation.RunDifference(context);
            bool save = state.Inning >= mc.CloserInning && diff >= 1 && diff <= mc.SaveMaxLead;
            bool hold = state.Inning == mc.SetupInning && diff >= 0 && diff <= mc.SaveMaxLead;
            bool isCloser = current.PlayerId == team.CloserId;
            bool isSetup = team.SetupIds.Contains(current.PlayerId);
            int closer = team.AvailableBullpen.Contains(team.CloserId) ? team.CloserId : -1;
            int setup = FirstAvailableSetup(team);
            bool startOfHalf = state.PlateAppearancesThisHalf == 0;

            if (startOfHalf && save && closer >= 0 && !isCloser)
            {
                return closer;
            }

            if (startOfHalf && hold && setup >= 0 && !isCloser && !isSetup && (!current.IsStarter || fatigue > 0))
            {
                return setup;
            }

            bool needChange = current.IsStarter
                ? fatigue >= mc.StarterFatigueThreshold || current.PitchCount >= mc.StarterMaxPitches
                : fatigue >= mc.RelieverFatigueThreshold || current.BattersFaced >= mc.RelieverMaxBattersFaced
                    || current.OutsRecorded >= mc.RelieverMaxOuts;
            if (!needChange)
            {
                return -1;
            }

            if (save && closer >= 0)
            {
                return closer;
            }

            if (hold && setup >= 0)
            {
                return setup;
            }

            foreach (int id in team.AvailableBullpen)
            {
                if (id != team.CloserId && !team.SetupIds.Contains(id))
                {
                    return id;
                }
            }

            return setup >= 0 ? setup : closer;
        }

        private static int FirstAvailableSetup(TeamGameState team)
        {
            foreach (int id in team.SetupIds)
            {
                if (team.AvailableBullpen.Contains(id))
                {
                    return id;
                }
            }

            return -1;
        }
    }
}
