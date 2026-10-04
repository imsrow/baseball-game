namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 감독 결정 (작전·교체)
    /// </summary>
    public interface IManagerDecision
    {
        /// <summary>공격 측 결정 (대타·대주자 등)</summary>
        Decision<ManagerOrders> DecideOffense(ManagerContext context);

        /// <summary>투구 전 공격 작전 (도루 시도·번트 사인). 매 투구 직전에 호출</summary>
        Decision<ManagerOrders> DecidePrePitch(ManagerContext context);

        /// <summary>수비 측 결정 (투수 교체·대수비·고의4구 등)</summary>
        Decision<ManagerOrders> DecideDefense(ManagerContext context);
    }
}
