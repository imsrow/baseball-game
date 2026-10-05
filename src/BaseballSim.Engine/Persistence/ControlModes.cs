using BaseballSim.Engine.Control;
using BaseballSim.Engine.State;

namespace BaseballSim.Engine.Persistence
{
    /// <summary>
    /// 팀·역할별 담당 방식 (저장용). 컨트롤러 객체는 저장하지 않고, 불러온 뒤 이 정보로 다시 연결한다.
    /// </summary>
    public sealed class ControlModes
    {
        /// <summary>[팀, 역할] 사람 담당 여부</summary>
        public bool[,] Human { get; } = new bool[2, 3];

        /// <summary>팀별 "작전·교체는 AI에게 맡김"</summary>
        public bool[] ManagerDelegatedToAi { get; } = new bool[2];

        /// <summary>팀별 사람 감독 세부 설정 (HumanManagerDecision과 같은 뜻, 구버전 저장은 기본값)</summary>
        public bool[] HumanOffenseTactics { get; } = { true, true };

        public bool[] HumanDefenseTactics { get; } = { true, true };

        public bool[] SubstitutionsByAi { get; } = new bool[2];

        public bool IsHuman(TeamSide side, DecisionRole role) => Human[(int)side, (int)role];

        public static ControlModes From(ControllerSet controllers)
        {
            var modes = new ControlModes();
            foreach (TeamSide side in new[] { TeamSide.Away, TeamSide.Home })
            {
                foreach (DecisionRole role in new[] { DecisionRole.Pitching, DecisionRole.Batting, DecisionRole.Manager })
                {
                    modes.Human[(int)side, (int)role] = controllers.IsHuman(side, role);
                }

                if (controllers.Manager(side) is HumanManagerDecision human)
                {
                    modes.ManagerDelegatedToAi[(int)side] = human.DelegateToAi;
                    modes.HumanOffenseTactics[(int)side] = human.HumanOffenseTactics;
                    modes.HumanDefenseTactics[(int)side] = human.HumanDefenseTactics;
                    modes.SubstitutionsByAi[(int)side] = human.SubstitutionsByAi;
                }
            }

            return modes;
        }
    }
}
