using BaseballSim.Engine.Control;
using BaseballSim.Engine.State;

namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// 사람 입력을 기다리는 결정
    /// </summary>
    public sealed class PendingDecision
    {
        public PendingDecision(DecisionKind kind, TeamSide side, object context)
        {
            Kind = kind;
            Side = side;
            Context = context;
        }

        public DecisionKind Kind { get; }

        public TeamSide Side { get; }

        /// <summary>PitchingContext / BattingContext / ManagerContext</summary>
        public object Context { get; }

        public PitchingContext PitchingContext => Context as PitchingContext;

        public BattingContext BattingContext => Context as BattingContext;

        public ManagerContext ManagerContext => Context as ManagerContext;
    }
}
