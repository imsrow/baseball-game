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

        /// <summary>번트 종류 (번트일 때만). None이면 엔진이 상황에 따라 정한다</summary>
        public BuntType BuntType { get; set; }

        public static BatterAction Take() => new BatterAction(BatterActionType.Take);

        public static BatterAction Swing(double? timingQuality = null) => new BatterAction(BatterActionType.Swing, timingQuality);

        public static BatterAction Bunt(BuntType type = BuntType.None)
        {
            return new BatterAction(BatterActionType.Bunt) { BuntType = type };
        }
    }
}
