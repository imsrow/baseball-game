namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 시즌 구조. 팀당 경기 수 = (팀 수 − 1) × 상대팀별 경기 수
    /// </summary>
    public sealed class SeasonConfig
    {
        public int TeamCount { get; set; } = 12;

        public int GamesPerOpponent { get; set; } = 12;

        /// <summary>한 시리즈 연속 경기 수</summary>
        public int SeriesLength { get; set; } = 3;

        /// <summary>선발 로테이션 인원</summary>
        public int RotationSize { get; set; } = 5;

        public int GamesPerTeam => (TeamCount - 1) * GamesPerOpponent;
    }
}
