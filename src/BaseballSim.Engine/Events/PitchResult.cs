namespace BaseballSim.Engine.Events
{
    /// <summary>
    /// 투구 한 개의 결과
    /// </summary>
    public enum PitchResult
    {
        Ball = 0,
        CalledStrike = 1,
        SwingingStrike = 2,
        Foul = 3,
        InPlay = 4,
        HitByPitch = 5,
    }
}
