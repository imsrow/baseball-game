namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// 기본 멈춤 조건 모음 (C단계에서 상황별 조건을 추가한다)
    /// </summary>
    public static class StopConditions
    {
        /// <summary>경기 끝까지 (사람 입력이 필요하면 그 전에 멈춘다)</summary>
        public static IStopCondition EndOfGame => new NeverStop();

        private sealed class NeverStop : IStopCondition
        {
            public bool ShouldStop(GameEngine engine)
            {
                return false;
            }
        }
    }
}
