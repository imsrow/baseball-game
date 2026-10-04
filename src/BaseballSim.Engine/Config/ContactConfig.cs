namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 스윙 시 컨택(헛스윙 여부)과 파울 판정
    /// </summary>
    public sealed class ContactConfig
    {
        /// <summary>타자 삼진회피 1 표준편차당 컨택 로그 오즈 증가</summary>
        public double AvoidKBeta { get; set; } = 0.30;

        /// <summary>투수 실효 구위 1 표준편차당 컨택 로그 오즈 감소</summary>
        public double StuffBeta { get; set; } = 0.28;

        /// <summary>2스트라이크 보호 스윙의 컨택 로그 오즈 증가</summary>
        public double TwoStrikeContactShift { get; set; } = 0.68;

        /// <summary>2스트라이크에서 삼진회피 능력치 효과 추가분 (β에 더함)</summary>
        public double TwoStrikeAvoidKExtraBeta { get; set; } = 0.10;
    }
}
