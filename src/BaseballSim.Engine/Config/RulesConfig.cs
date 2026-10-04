namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 경기 규칙
    /// </summary>
    public sealed class RulesConfig
    {
        public int InningsPerGame { get; set; } = 9;
        public int OutsPerHalfInning { get; set; } = 3;
        public int BallsForWalk { get; set; } = 4;
        public int StrikesForStrikeout { get; set; } = 3;
        public int LineupSize { get; set; } = 9;

        /// <summary>연장 승부치기(2루 주자) 사용</summary>
        public bool UseExtraInningRunner { get; set; } = true;

        /// <summary>
        /// 안전장치: 이 이닝까지 끝나도 승부가 안 나면 무승부 처리 (0이면 무제한)
        /// </summary>
        public int MaxInnings { get; set; } = 30;
    }
}
