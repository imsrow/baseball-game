using BaseballSim.Engine.Control;

namespace BaseballProto.Quality
{
    /// <summary>
    /// 스윙 입력 판정 결과 (엔진에 넘길 BatterAction + 디버그 표시값)
    /// </summary>
    public sealed class BattingJudgement
    {
        /// <summary>입력 시각 − 공 도달 시각 (ms, + 늦음)</summary>
        public double TimingErrorMs { get; set; }

        public double TimingScore { get; set; }

        /// <summary>커서 − 공 (m, x: 1루 쪽 +)</summary>
        public double CursorDx { get; set; }

        /// <summary>커서 − 공 (m, + 커서가 위)</summary>
        public double CursorDz { get; set; }

        /// <summary>커서 중심 거리 / 커서 반지름</summary>
        public double CursorDistanceRatio { get; set; }

        public double CursorScore { get; set; }

        /// <summary>타이밍·커서 점수 가중 평균 (0~1). TimingQuality = 2 × 이 값 − 1</summary>
        public double CombinedScore { get; set; }

        /// <summary>합산에 쓴 타이밍 비중</summary>
        public double TimingWeight { get; set; }

        public BatterAction Action { get; set; }
    }
}
