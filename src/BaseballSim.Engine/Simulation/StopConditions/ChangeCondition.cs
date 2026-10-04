using System;
using BaseballSim.Engine.State;

namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// 처음 평가 때의 값을 기억해 두고, 값이 바뀌면 멈춘다 (타석·반이닝·이닝 종료)
    /// </summary>
    public sealed class ChangeCondition : IStopCondition
    {
        private readonly Func<GameState, int> _key;
        private int? _start;

        public ChangeCondition(Func<GameState, int> key)
        {
            _key = key;
        }

        public bool ShouldStop(GameEngine engine)
        {
            int value = _key(engine.State);
            if (!_start.HasValue)
            {
                _start = value;
                return false;
            }

            return value != _start.Value;
        }
    }
}
