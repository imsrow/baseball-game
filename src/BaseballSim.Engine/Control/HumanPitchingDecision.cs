namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 사람 입력 투수: 항상 Pending. UI가 GameEngine.Submit(PitchCall)로 답한다.
    /// </summary>
    public sealed class HumanPitchingDecision : IPitchingDecision
    {
        public static readonly HumanPitchingDecision Instance = new HumanPitchingDecision();

        public Decision<PitchCall> DecidePitch(PitchingContext context)
        {
            return Decision<PitchCall>.Pending;
        }
    }
}
