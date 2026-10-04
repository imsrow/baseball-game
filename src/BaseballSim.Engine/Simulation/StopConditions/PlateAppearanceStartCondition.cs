using System;

namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// 상황 조건: 진행을 시작한 타석이 아닌 새 타석 시작 시점에 조건이 맞으면 멈춘다.
    /// (조건이 계속 참이어도 같은 타석에서 바로 다시 멈추지 않는다)
    /// </summary>
    public sealed class PlateAppearanceStartCondition : IStopCondition
    {
        private readonly Func<GameEngine, bool> _predicate;
        private int? _startPlateAppearance;

        public PlateAppearanceStartCondition(Func<GameEngine, bool> predicate)
        {
            _predicate = predicate;
        }

        public bool ShouldStop(GameEngine engine)
        {
            int pa = engine.State.PlateAppearanceNumber;
            if (!_startPlateAppearance.HasValue)
            {
                _startPlateAppearance = pa;
                return false;
            }

            return pa != _startPlateAppearance.Value
                && engine.State.Phase == GamePhase.OffenseManager
                && _predicate(engine);
        }
    }
}
