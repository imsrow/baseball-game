using BaseballSim.Engine.Control;

namespace BaseballSim.Engine.Tests
{
    /// <summary>
    /// 테스트용 감독: 모든 결정 지점에서 입력을 기다린다 (Submit 검증 테스트용)
    /// </summary>
    public sealed class PromptingManager : IManagerDecision
    {
        public Decision<ManagerOrders> DecideOffense(ManagerContext context) => Decision<ManagerOrders>.Pending;

        public Decision<ManagerOrders> DecidePrePitch(ManagerContext context) => Decision<ManagerOrders>.Pending;

        public Decision<ManagerOrders> DecideDefense(ManagerContext context) => Decision<ManagerOrders>.Pending;
    }
}
