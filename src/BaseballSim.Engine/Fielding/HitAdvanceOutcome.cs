namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 안타 진루 결과 요약: 타자가 안전하게 도달한 베이스와 추가 진루 중 아웃 여부
    /// </summary>
    public struct HitAdvanceOutcome
    {
        public HitAdvanceOutcome(int batterSafeBase, bool batterOut)
        {
            BatterSafeBase = batterSafeBase;
            BatterOut = batterOut;
        }

        /// <summary>타자가 안전하게 도달한 마지막 베이스 (안타 종류 결정, 송구 실책 진루 제외)</summary>
        public int BatterSafeBase { get; }

        /// <summary>추가 진루를 노리다 아웃</summary>
        public bool BatterOut { get; }
    }
}
