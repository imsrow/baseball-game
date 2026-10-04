using BaseballSim.Engine.Control;

namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// Step() 결과
    /// </summary>
    public sealed class StepResult
    {
        public StepResult(StepStatus status, DecisionKind? processed, int newEvents)
        {
            Status = status;
            Processed = processed;
            NewEvents = newEvents;
        }

        public StepStatus Status { get; }

        /// <summary>이번에 처리한 결정 지점 (입력 대기·경기 종료면 null)</summary>
        public DecisionKind? Processed { get; }

        /// <summary>이번 Step에서 기록된 이벤트 수</summary>
        public int NewEvents { get; }
    }
}
