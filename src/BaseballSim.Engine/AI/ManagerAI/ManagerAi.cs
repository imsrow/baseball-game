using BaseballSim.Engine.Control;

namespace BaseballSim.Engine.AI.ManagerAI
{
    /// <summary>
    /// 감독 AI. 판단 기준 계수는 ManagerAiConfig에 있다.
    /// A단계: 투수 교체만. 도루·번트·고의4구·대타·대주자·대수비는 B단계에서 추가.
    /// </summary>
    public sealed class ManagerAi : IManagerDecision
    {
        public Decision<ManagerOrders> DecideOffense(ManagerContext context)
        {
            return Decision<ManagerOrders>.Ready(ManagerOrders.None());
        }

        public Decision<ManagerOrders> DecideDefense(ManagerContext context)
        {
            int reliever = PitchingChangeAdvisor.Advise(context);
            if (reliever >= 0)
            {
                return Decision<ManagerOrders>.Ready(ManagerOrders.Of(ManagerAction.PitchingChange(reliever)));
            }

            return Decision<ManagerOrders>.Ready(ManagerOrders.None());
        }
    }
}
