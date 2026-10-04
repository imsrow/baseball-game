namespace BaseballProto.Input
{
    /// <summary>
    /// 타격 조작 방식
    /// </summary>
    public enum BattingControlMode
    {
        /// <summary>드래그로 커서 이동 + 스윙 버튼 누르는 순간이 스윙 시각</summary>
        DragCursor = 0,

        /// <summary>탭한 위치가 커서, 탭한 순간이 스윙 시각 (한 손가락)</summary>
        TapToSwing = 1,
    }
}
