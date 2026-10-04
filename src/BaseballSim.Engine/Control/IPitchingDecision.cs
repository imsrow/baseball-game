namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 투수 결정 (구종·목표 지점)
    /// </summary>
    public interface IPitchingDecision
    {
        Decision<PitchCall> DecidePitch(PitchingContext context);
    }
}
