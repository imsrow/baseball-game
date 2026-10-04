using BaseballSim.Engine.AI.ManagerAI;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.State;

namespace BaseballSim.Engine.AI
{
    /// <summary>
    /// AI 컨트롤러 생성
    /// </summary>
    public static class AiControllers
    {
        public static readonly AiPitchingDecision Pitching = new AiPitchingDecision();
        public static readonly AiBattingDecision Batting = new AiBattingDecision();
        public static readonly ManagerAi Manager = new ManagerAi();

        /// <summary>양 팀 모든 역할 AI (순수 시뮬레이션)</summary>
        public static ControllerSet CreateAllAi()
        {
            var set = new ControllerSet();
            set.AssignAll(Pitching, Batting, Manager);
            return set;
        }

        /// <summary>한 팀·역할을 AI로 되돌린다</summary>
        public static void AssignAi(ControllerSet set, TeamSide side, DecisionRole role)
        {
            switch (role)
            {
                case DecisionRole.Pitching:
                    set.Assign(side, (IPitchingDecision)Pitching);
                    break;
                case DecisionRole.Batting:
                    set.Assign(side, (IBattingDecision)Batting);
                    break;
                default:
                    set.Assign(side, (IManagerDecision)Manager);
                    break;
            }
        }
    }
}
