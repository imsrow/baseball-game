namespace BaseballSim.Engine.Pitching
{
    /// <summary>
    /// Statcast 공략 구역
    /// </summary>
    public enum AttackRegion
    {
        /// <summary>존 한가운데 (존 크기 67% 이내)</summary>
        Heart = 0,

        /// <summary>존 경계 부근 (67% ~ 133%)</summary>
        Shadow = 1,

        /// <summary>유인구 구역 (133% ~ 200%)</summary>
        Chase = 2,

        /// <summary>완전히 빠진 공 (200% 초과)</summary>
        Waste = 3,
    }
}
