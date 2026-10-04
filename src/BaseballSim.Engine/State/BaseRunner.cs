namespace BaseballSim.Engine.State
{
    /// <summary>
    /// 루상의 주자 (실점 책임 투수 포함)
    /// </summary>
    public sealed class BaseRunner
    {
        public BaseRunner()
        {
        }

        public BaseRunner(int playerId, int responsiblePitcherId)
        {
            PlayerId = playerId;
            ResponsiblePitcherId = responsiblePitcherId;
        }

        public int PlayerId { get; set; }

        /// <summary>이 주자가 득점하면 실점이 기록되는 투수</summary>
        public int ResponsiblePitcherId { get; set; }
    }
}
