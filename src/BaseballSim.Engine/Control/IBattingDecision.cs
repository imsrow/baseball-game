namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 타자 결정 (스윙 여부)
    /// </summary>
    public interface IBattingDecision
    {
        Decision<BatterAction> DecideSwing(BattingContext context);
    }
}
