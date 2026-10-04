namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// RunUntil() 결과
    /// </summary>
    public sealed class RunResult
    {
        public RunResult(RunStatus status, int steps)
        {
            Status = status;
            Steps = steps;
        }

        public RunStatus Status { get; }

        public int Steps { get; }
    }
}
