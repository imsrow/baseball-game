namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// Step() 결과 상태
    /// </summary>
    public enum StepStatus
    {
        /// <summary>결정 지점 하나를 처리하고 다음 결정 지점에 도착</summary>
        Advanced = 0,

        /// <summary>사람 입력 대기 (Pending 참조)</summary>
        AwaitingInput = 1,

        GameOver = 2,
    }
}
