namespace BaseballSim.Engine.State
{
    /// <summary>
    /// 경기 중 투수 한 명의 기록·상태
    /// </summary>
    public sealed class PitcherGameState
    {
        public int PlayerId { get; set; }

        public bool IsStarter { get; set; }

        public int PitchCount { get; set; }

        public int BattersFaced { get; set; }

        public int OutsRecorded { get; set; }

        public int RunsAllowed { get; set; }
    }
}
