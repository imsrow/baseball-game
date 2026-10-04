namespace BaseballProto.Core
{
    /// <summary>
    /// 투구 하나의 진행 단계 (HUD 표시·입력 허용 판단용)
    /// </summary>
    public enum DuelPhase
    {
        Idle = 0,

        /// <summary>투수 준비 (타격 모드: 커서 이동 가능)</summary>
        Windup = 1,

        /// <summary>공이 날아오는 중 (타격 모드: 스윙 입력 대기)</summary>
        InFlight = 2,

        /// <summary>투구 선택·조준·게이지 (투구 모드)</summary>
        Aiming = 3,

        /// <summary>결과 연출</summary>
        Result = 4,
    }
}
