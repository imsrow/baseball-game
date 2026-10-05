namespace BaseballProto.Core
{
    /// <summary>연출 대본의 주자 한 명: 이동 경로와 아웃·퇴장 시각</summary>
    public sealed class RunnerScript
    {
        public RunnerScript(int playerId, MotionTrack track)
        {
            PlayerId = playerId;
            Track = track;
        }

        public int PlayerId { get; }

        public MotionTrack Track { get; }

        /// <summary>최종 베이스 (1~3), 아웃·득점이면 0</summary>
        public int FinalBase { get; set; }

        /// <summary>아웃 판정 시각 (아웃이 아니면 무한대)</summary>
        public float OutTime { get; set; } = float.PositiveInfinity;

        /// <summary>화면에서 사라지는 시각 (아웃·득점)</summary>
        public float HideTime { get; set; } = float.PositiveInfinity;
    }
}
