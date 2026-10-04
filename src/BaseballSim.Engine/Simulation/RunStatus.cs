namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// RunUntil() 종료 사유
    /// </summary>
    public enum RunStatus
    {
        ConditionMet = 0,
        AwaitingInput = 1,
        GameOver = 2,
    }
}
