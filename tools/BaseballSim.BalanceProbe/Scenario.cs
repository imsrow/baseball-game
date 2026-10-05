namespace BaseballSim.BalanceProbe
{
    /// <summary>
    /// 비교 시나리오. 글자 코드(a~i)로 고른다.
    /// </summary>
    public enum Scenario
    {
        /// <summary>(a) AI 투수 vs AI 타자 (기준)</summary>
        AiVsAi = 0,

        /// <summary>(b) 사람처럼 던짐: 존 가장자리 위주, ReleaseQuality 0.5~1.0</summary>
        HumanEdge = 1,

        /// <summary>(c) 사람처럼 던짐: 존 전체 고르게, ReleaseQuality 0.5~1.0</summary>
        HumanUniform = 2,

        /// <summary>(d) AI가 고른 목표 + 사람 ReleaseQuality 0.5~1.0 (릴리스 품질만의 영향)</summary>
        AiTargetHumanRelease = 3,

        /// <summary>(e) 가장자리, ReleaseQuality 1.0, 제구 보정 상한 1.5 (TUNE 최대)</summary>
        EdgePerfectMaxCap = 4,

        /// <summary>(f) 사람처럼 던짐: 한가운데(Heart)만, ReleaseQuality 0.5~1.0</summary>
        HumanHeart = 5,

        /// <summary>(g) 사람처럼 침: 존 안 공만 스윙, TimingQuality 0~1</summary>
        BatZoneUniform = 6,

        /// <summary>(h) 사람처럼 침: 존 안 공만 스윙, TimingQuality 1.0</summary>
        BatZonePerfect = 7,

        /// <summary>(i) 사람처럼 침: 존 밖도 50% 스윙, TimingQuality 0~1</summary>
        BatChaseUniform = 8,
    }
}
