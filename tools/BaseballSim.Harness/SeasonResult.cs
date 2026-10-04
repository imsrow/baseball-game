using BaseballSim.Engine.Stats;

namespace BaseballSim.Harness
{
    /// <summary>
    /// 시즌 한 번(시드 하나)의 결과
    /// </summary>
    public sealed class SeasonResult
    {
        public ulong Seed { get; set; }

        public int Games { get; set; }

        public int ExtraInningGames { get; set; }

        public StatsAggregator Stats { get; set; }

        public double ElapsedSeconds { get; set; }

        /// <summary>팀당 경기 수 기준 분모 (경기 수 × 2)</summary>
        public int TeamGames => Games * 2;
    }
}
