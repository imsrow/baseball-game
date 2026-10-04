namespace BaseballSim.Harness
{
    /// <summary>
    /// 구원투수 최근 등판 기록
    /// </summary>
    public sealed class ReliefRecord
    {
        /// <summary>마지막 등판 경기 번호 (팀 기준)</summary>
        public int LastGame { get; set; }

        /// <summary>마지막 등판까지 연속 등판 경기 수</summary>
        public int ConsecutiveGames { get; set; }

        /// <summary>마지막 등판 투구 수</summary>
        public int LastPitches { get; set; }
    }
}
