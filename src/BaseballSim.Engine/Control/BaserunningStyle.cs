namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 팀 주루 성향: 추가 진루·태그업을 시도하는 기준 (필요한 예상 여유 시간)을 바꾼다.
    /// AI 기본은 Normal (기준 그대로)
    /// </summary>
    public enum BaserunningStyle
    {
        Normal = 0,

        /// <summary>공격적: 빠듯해도 한 베이스 더 (주루사도 늘어남)</summary>
        Aggressive = 1,

        /// <summary>신중: 여유가 충분할 때만</summary>
        Cautious = 2,
    }
}
