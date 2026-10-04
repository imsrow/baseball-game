namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 사람 입력 타자: 항상 Pending. UI가 GameEngine.Submit(BatterAction)으로 답한다.
    /// </summary>
    public sealed class HumanBattingDecision : IBattingDecision
    {
        public static readonly HumanBattingDecision Instance = new HumanBattingDecision();

        public Decision<BatterAction> DecideSwing(BattingContext context)
        {
            return Decision<BatterAction>.Pending;
        }
    }
}
