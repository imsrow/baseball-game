using BaseballSim.Engine.Control;

namespace BaseballSim.Engine.AI
{
    /// <summary>
    /// AI 타자: 인지 위치 기준 스윙 확률로 스윙 여부를 정한다.
    /// </summary>
    public sealed class AiBattingDecision : IBattingDecision
    {
        public Decision<BatterAction> DecideSwing(BattingContext context)
        {
            double p = SwingDecisionModel.SwingProbability(context.Config, context.Batter.Batting.Eye, context.Perceived,
                context.EffectiveStuffZ, context.Balls, context.Strikes, context.SameHand);
            return Decision<BatterAction>.Ready(context.Random.NextDouble() < p ? BatterAction.Swing() : BatterAction.Take());
        }
    }
}
