namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// 진행 멈춤 조건. 결정 지점에 도착할 때마다(결정을 내리기 직전) 평가한다.
    /// </summary>
    public interface IStopCondition
    {
        bool ShouldStop(GameEngine engine);
    }
}
