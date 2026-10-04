using BaseballSim.Engine.Simulation;

namespace BaseballProto.Core
{
    /// <summary>
    /// 멈춤 조건 없음: RunUntil이 사람 입력 대기 또는 경기 종료에서만 멈춘다
    /// </summary>
    public sealed class NeverStop : IStopCondition
    {
        public static readonly NeverStop Instance = new NeverStop();

        public bool ShouldStop(GameEngine engine)
        {
            return false;
        }
    }
}
