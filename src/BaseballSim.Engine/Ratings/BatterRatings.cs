namespace BaseballSim.Engine.Ratings
{
    /// <summary>
    /// 타자 능력치 (20~80)
    /// </summary>
    public sealed class BatterRatings
    {
        /// <summary>컨택: 맞혔을 때 정타 비율 (타구속도 편차↓, 빗맞음↓ → BABIP·타율)</summary>
        public int Contact { get; set; } = ScoutScale.Average;

        /// <summary>파워: 타구속도 평균·상한, 발사각 상승</summary>
        public int Power { get; set; } = ScoutScale.Average;

        /// <summary>선구안: 투구 위치 인지 정확도, 존 밖 스윙 억제</summary>
        public int Eye { get; set; } = ScoutScale.Average;

        /// <summary>삼진회피: 스윙당 컨택률, 2스트라이크에서 걷어내기</summary>
        public int AvoidK { get; set; } = ScoutScale.Average;

        /// <summary>갭파워: 라인드라이브·중거리 타구 비율 (주로 2루타)</summary>
        public int Gap { get; set; } = ScoutScale.Average;

        /// <summary>스피드: 주력</summary>
        public int Speed { get; set; } = ScoutScale.Average;

        /// <summary>타구방향 성향: 높을수록 당겨치기, 낮을수록 밀어치기</summary>
        public int PullTendency { get; set; } = ScoutScale.Average;

        /// <summary>도루 스타트: 리드·스타트 반응</summary>
        public int StealJump { get; set; } = ScoutScale.Average;

        /// <summary>번트</summary>
        public int Bunt { get; set; } = ScoutScale.Average;

        /// <summary>주루 센스: 추가 진루 판단, 주루사 억제 (스피드와 별개)</summary>
        public int BaserunningInstinct { get; set; } = ScoutScale.Average;
    }
}
