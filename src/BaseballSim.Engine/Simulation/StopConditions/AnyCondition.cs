namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// 여러 조건 중 하나라도 만족하면 멈춘다 (모든 조건을 매번 평가해 기준점을 함께 잡는다)
    /// </summary>
    public sealed class AnyCondition : IStopCondition
    {
        private readonly IStopCondition[] _conditions;

        public AnyCondition(IStopCondition[] conditions)
        {
            _conditions = conditions;
        }

        public bool ShouldStop(GameEngine engine)
        {
            bool stop = false;
            foreach (IStopCondition condition in _conditions)
            {
                stop |= condition.ShouldStop(engine);
            }

            return stop;
        }
    }
}
