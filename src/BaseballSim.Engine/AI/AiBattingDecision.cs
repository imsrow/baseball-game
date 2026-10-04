using BaseballSim.Engine.Control;
using BaseballSim.Engine.Pitching;

namespace BaseballSim.Engine.AI
{
    /// <summary>
    /// AI 타자: 인지 위치 기준 스윙 확률로 스윙 여부를 정한다.
    /// 번트 사인이 있으면 스트라이크로 보인 공(Heart·Shadow)에 번트를 대고 나머지는 지켜본다.
    /// </summary>
    public sealed class AiBattingDecision : IBattingDecision
    {
        public Decision<BatterAction> DecideSwing(BattingContext context)
        {
            BuntType sign = context.State.BuntSign;
            if (sign != BuntType.None)
            {
                bool looksLikeStrike = context.Perceived.Region == AttackRegion.Heart
                    || context.Perceived.Region == AttackRegion.Shadow;
                return Decision<BatterAction>.Ready(looksLikeStrike ? BatterAction.Bunt(sign) : BatterAction.Take());
            }

            double p = SwingDecisionModel.SwingProbability(context.Config, context.Batter.Batting.Eye, context.Perceived,
                context.EffectiveStuffZ, context.Balls, context.Strikes, context.SameHand);
            return Decision<BatterAction>.Ready(context.Random.NextDouble() < p ? BatterAction.Swing() : BatterAction.Take());
        }
    }
}
