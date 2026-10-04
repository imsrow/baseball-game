namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 번트 종류
    /// </summary>
    public enum BuntType
    {
        None = 0,

        /// <summary>희생번트: 주자 진루가 목적</summary>
        Sacrifice = 1,

        /// <summary>기습번트: 타자 출루가 목적</summary>
        ForHit = 2,
    }
}
