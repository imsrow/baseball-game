using System;
using BaseballSim.Engine.State;

namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 팀·역할별 결정 담당자. 경기 상태와 분리되어 있어 어느 결정 지점에서든 즉시 교체할 수 있다.
    /// </summary>
    public sealed class ControllerSet
    {
        private readonly IPitchingDecision[] _pitching = new IPitchingDecision[2];
        private readonly IBattingDecision[] _batting = new IBattingDecision[2];
        private readonly IManagerDecision[] _manager = new IManagerDecision[2];

        public void Assign(TeamSide side, IPitchingDecision controller)
        {
            _pitching[(int)side] = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public void Assign(TeamSide side, IBattingDecision controller)
        {
            _batting[(int)side] = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public void Assign(TeamSide side, IManagerDecision controller)
        {
            _manager[(int)side] = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        public IPitchingDecision Pitching(TeamSide side) => _pitching[(int)side];

        public IBattingDecision Batting(TeamSide side) => _batting[(int)side];

        public IManagerDecision Manager(TeamSide side) => _manager[(int)side];

        /// <summary>해당 팀·역할이 사람 입력인지</summary>
        public bool IsHuman(TeamSide side, DecisionRole role)
        {
            switch (role)
            {
                case DecisionRole.Pitching: return _pitching[(int)side] is HumanPitchingDecision;
                case DecisionRole.Batting: return _batting[(int)side] is HumanBattingDecision;
                default: return _manager[(int)side] is HumanManagerDecision;
            }
        }

        /// <summary>양 팀 모든 역할에 같은 컨트롤러 지정</summary>
        public void AssignAll(IPitchingDecision pitching, IBattingDecision batting, IManagerDecision manager)
        {
            foreach (TeamSide side in new[] { TeamSide.Away, TeamSide.Home })
            {
                Assign(side, pitching);
                Assign(side, batting);
                Assign(side, manager);
            }
        }

        /// <summary>다른 세트의 지정을 그대로 복사 (구간 시뮬 후 원래 담당자 복원용)</summary>
        public void CopyFrom(ControllerSet other)
        {
            Array.Copy(other._pitching, _pitching, 2);
            Array.Copy(other._batting, _batting, 2);
            Array.Copy(other._manager, _manager, 2);
        }

        public ControllerSet Clone()
        {
            var copy = new ControllerSet();
            Array.Copy(_pitching, copy._pitching, 2);
            Array.Copy(_batting, copy._batting, 2);
            Array.Copy(_manager, copy._manager, 2);
            return copy;
        }
    }
}
