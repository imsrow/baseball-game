namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 결정 지점 종류
    /// </summary>
    public enum DecisionKind
    {
        OffenseManager = 0,
        DefenseManager = 1,
        Pitch = 2,
        Swing = 3,

        /// <summary>투구 전 공격 작전 (도루·번트 사인)</summary>
        OffensePrePitch = 4,
    }
}
