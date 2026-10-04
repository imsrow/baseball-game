using BaseballSim.Engine.Control;

namespace BaseballSim.Engine.AI.ManagerAI
{
    /// <summary>
    /// 감독 AI. 판단 기준 계수는 ManagerAiConfig에 있다.
    /// 투수 교체(불펜 역할), 도루, 번트, 고의4구, 대타, 대주자, 대수비.
    /// </summary>
    public sealed class ManagerAi : IManagerDecision
    {
        public Decision<ManagerOrders> DecideOffense(ManagerContext context)
        {
            var orders = new ManagerOrders();
            ManagerAction pinchHit = SubstitutionAdvisor.PinchHit(context);
            if (pinchHit != null)
            {
                orders.Actions.Add(pinchHit);
            }

            ManagerAction pinchRun = SubstitutionAdvisor.PinchRun(context, pinchHit?.IncomingPlayerId ?? -1);
            if (pinchRun != null)
            {
                orders.Actions.Add(pinchRun);
            }

            return Decision<ManagerOrders>.Ready(orders);
        }

        public Decision<ManagerOrders> DecidePrePitch(ManagerContext context)
        {
            BuntType bunt = BuntAdvisor.Advise(context);
            if (bunt != BuntType.None)
            {
                return Decision<ManagerOrders>.Ready(ManagerOrders.Of(ManagerAction.Bunt(bunt)));
            }

            int stealFrom = StealAdvisor.Advise(context);
            if (stealFrom > 0)
            {
                return Decision<ManagerOrders>.Ready(ManagerOrders.Of(ManagerAction.Steal(stealFrom)));
            }

            return Decision<ManagerOrders>.Ready(ManagerOrders.None());
        }

        public Decision<ManagerOrders> DecideDefense(ManagerContext context)
        {
            var orders = new ManagerOrders();
            orders.Actions.AddRange(SubstitutionAdvisor.DefensiveSubs(context));
            int reliever = PitchingChangeAdvisor.Advise(context);
            if (reliever >= 0)
            {
                orders.Actions.Add(ManagerAction.PitchingChange(reliever));
            }

            if (IntentionalWalkAdvisor.Advise(context))
            {
                orders.Actions.Add(ManagerAction.IntentionalWalk());
            }

            return Decision<ManagerOrders>.Ready(orders);
        }
    }
}
