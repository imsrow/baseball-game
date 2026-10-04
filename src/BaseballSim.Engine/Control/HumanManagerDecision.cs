namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 사람 입력 감독: 항상 Pending. UI가 GameEngine.Submit(ManagerOrders)로 답한다.
    /// </summary>
    public sealed class HumanManagerDecision : IManagerDecision
    {
        public static readonly HumanManagerDecision Instance = new HumanManagerDecision();

        public Decision<ManagerOrders> DecideOffense(ManagerContext context)
        {
            return Decision<ManagerOrders>.Pending;
        }

        public Decision<ManagerOrders> DecidePrePitch(ManagerContext context)
        {
            return Decision<ManagerOrders>.Pending;
        }

        public Decision<ManagerOrders> DecideDefense(ManagerContext context)
        {
            return Decision<ManagerOrders>.Pending;
        }
    }
}
