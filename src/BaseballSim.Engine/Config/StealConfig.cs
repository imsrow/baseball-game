namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 도루 성공 확률 (단순 확률 모델)
    /// logit(성공) = logit(기준 성공률) + 주자(스타트·스피드) − 배터리(퀵모션·포수 송구) + 상황 보정
    /// </summary>
    public sealed class StealConfig
    {
        /// <summary>모든 능력치 50일 때 2루 도루 성공률</summary>
        public double BaseSuccessRate { get; set; } = 0.70;

        public double JumpBeta { get; set; } = 0.35;
        public double SpeedBeta { get; set; } = 0.35;

        /// <summary>투수 퀵모션 1 표준편차당 성공 로그 오즈 감소</summary>
        public double HoldRunnersBeta { get; set; } = 0.25;

        /// <summary>포수 송구 강도(실효) 1 표준편차당 성공 로그 오즈 감소</summary>
        public double CatcherArmBeta { get; set; } = 0.25;

        /// <summary>포수 송구 정확도(실효) 1 표준편차당 성공 로그 오즈 감소</summary>
        public double CatcherAccuracyBeta { get; set; } = 0.10;

        /// <summary>3루 도루 로그 오즈 보정</summary>
        public double StealThirdShift { get; set; } = -0.20;

        /// <summary>변화구·오프스피드(느린 공) 투구 때 로그 오즈 보정</summary>
        public double SlowPitchShift { get; set; } = 0.15;
    }
}
