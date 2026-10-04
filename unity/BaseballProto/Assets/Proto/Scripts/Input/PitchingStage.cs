namespace BaseballProto.Input
{
    /// <summary>
    /// 투구 입력 단계
    /// </summary>
    public enum PitchingStage
    {
        Inactive = 0,

        /// <summary>구종 선택(버튼) + 목표 지점 터치 대기</summary>
        Aim = 1,

        /// <summary>게이지 왕복 중, 탭으로 멈춤</summary>
        Gauge = 2,

        Done = 3,
    }
}
