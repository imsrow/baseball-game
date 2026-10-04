using BaseballSim.Engine.Control;

namespace BaseballProto.Quality
{
    /// <summary>
    /// 투구 입력 판정 결과 (엔진에 넘길 PitchCall + 디버그 표시값)
    /// </summary>
    public sealed class PitchJudgement
    {
        /// <summary>게이지 정지 값 (시간 초과면 null)</summary>
        public double? GaugeValue { get; set; }

        /// <summary>스위트 중심과의 차이</summary>
        public double GaugeError { get; set; }

        public double ReleaseScore { get; set; }

        public PitchCall Call { get; set; }
    }
}
