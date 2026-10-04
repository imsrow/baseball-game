namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 감독 작전·교체 종류
    /// </summary>
    public enum ManagerActionType
    {
        PitchingChange = 0,

        /// <summary>도루 시도 (투구 전, 공격)</summary>
        StealAttempt = 1,

        /// <summary>번트 사인 (투구 전, 공격)</summary>
        BuntSign = 2,

        /// <summary>고의4구 (타석 시작, 수비)</summary>
        IntentionalWalk = 3,

        /// <summary>대타 (타석 시작, 공격): 현재 타자 자리에 벤치 선수</summary>
        PinchHitter = 4,

        /// <summary>대주자 (타석 시작, 공격): 루상 주자 자리에 벤치 선수</summary>
        PinchRunner = 5,

        /// <summary>대수비 (타석 시작, 수비): 라인업 선수를 같은 포지션의 벤치 선수로</summary>
        DefensiveSubstitution = 6,
    }
}
