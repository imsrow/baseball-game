using BaseballSim.Engine.Simulation;
using BaseballSim.Engine.State;

namespace BaseballProto.Core
{
    /// <summary>
    /// 사람 작전 결정 직전에서 멈춤: 타격 모드는 매 투구 전 공격 작전(도루·번트), 투구 모드는 타석 시작 수비 작전(고의4구).
    /// 멈춘 동안 UI가 작전을 고르고 START를 누르면 그 결정 지점을 진행한다 (RunUntil은 Step 전에 멈춤 조건을 본다).
    /// </summary>
    public sealed class TacticsPoint : IStopCondition
    {
        private readonly DuelMode _mode;

        public TacticsPoint(DuelMode mode)
        {
            _mode = mode;
        }

        public bool ShouldStop(GameEngine engine)
        {
            GamePhase phase = engine.State.Phase;
            return _mode == DuelMode.Batting ? phase == GamePhase.OffensePrePitch : phase == GamePhase.DefenseManager;
        }
    }
}
