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

        /// <summary>구원투수가 연속 등판할 수 있는 최대 경기 수 (넘으면 다음 경기 휴식)</summary>
        public int BullpenMaxConsecutiveGames { get; set; } = 2;

        /// <summary>전 경기 이 투구 수 이상이면 다음 경기 휴식</summary>
        public int BullpenHeavyWorkloadPitches { get; set; } = 30;

        /// <summary>셋업 투수 인원</summary>
        public int SetupMenCount { get; set; } = 2;

        public int GamesPerTeam => (TeamCount - 1) * GamesPerOpponent;
    }
}
