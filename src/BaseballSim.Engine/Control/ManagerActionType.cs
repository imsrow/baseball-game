namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 감독 작전·교체 종류
    /// </summary>
    public enum ManagerActionType
    {
        PitchingChange = 0,

        /// <summary>도루 시도 (투구 전, 공격)</summary>
        StealAttempt = 1,

        /// <summary>번트 사인 (투구 전, 공격)</summary>
        BuntSign = 2,
    }
}
