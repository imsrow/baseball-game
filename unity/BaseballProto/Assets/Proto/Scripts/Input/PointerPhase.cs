namespace BaseballProto.Input
{
    /// <summary>
    /// 포인터 이벤트 종류
    /// </summary>
    public enum PointerPhase
    {
        Down = 0,
        Move = 1,
        Up = 2,

        /// <summary>키보드 스윙 키 (에디터·PC 테스트용)</summary>
        SwingKey = 3,
    }
}
