namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 직접 플레이 진행 관련 설정 (사람 감독 확인 시점, 멈춤 조건)
    /// </summary>
    public sealed class InteractionConfig
    {
        /// <summary>사람 감독이 교체 제안을 거절한 뒤, 같은 투수에 대해 다시 묻기까지 상대할 타자 수</summary>
        public int DeclinedChangeRepromptBatters { get; set; } = 3;

        /// <summary>득점권 위기·찬스 멈춤 조건이 적용되는 최대 점수 차</summary>
        public int ClutchMaxRunDifference { get; set; } = 3;
    }
}
