namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 타자 결정: 지켜봄 / 스윙 / 번트. 컨택·타구 결과는 엔진이 정한다.
    /// </summary>
    public sealed class BatterAction
    {
        public BatterAction()
        {
        }

        public BatterAction(BatterActionType type, double? timingQuality = null)
        {
            Type = type;
            TimingQuality = timingQuality;
        }

        public BatterActionType Type { get; set; }

        /// <summary>사람 조작 스윙 타이밍 품질 (−1 ~ +1). AI는 null(중립)</summary>
        public double? TimingQuality { get; set; }

        /// <summary>
        /// 사람 조작 스윙 타이밍 방향 (−1 이름 ~ +1 늦음). 이르면 당겨치기, 늦으면 밀어치기 쪽으로 타구 방향 보정.
        /// AI는 null(중립)
        /// </summary>
        public double? TimingDirection { get; set; }

        /// <summary>
        /// 사람 조작 커서 상하 오차 (−1 커서가 공보다 아래 ~ +1 위). 위면 발사각 낮게, 아래면 높게.
        /// AI는 null(중립)
        /// </summary>
        public double? CursorVerticalOffset { get; set; }

        /// <summary>번트 종류 (번트일 때만). None이면 엔진이 상황에 따라 정한다</summary>
        public BuntType BuntType { get; set; }

        public static BatterAction Take() => new BatterAction(BatterActionType.Take);

        public static BatterAction Swing(double? timingQuality = null) => new BatterAction(BatterActionType.Swing, timingQuality);

        /// <param name="quality">사람 번트 입력 품질 (−1~+1, 커서·타이밍). null이면 보정 없음 (AI)</param>
        public static BatterAction Bunt(BuntType type = BuntType.None, double? quality = null)
        {
            return new BatterAction(BatterActionType.Bunt, quality) { BuntType = type };
        }
    }
}
