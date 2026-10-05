namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 사람 조작 입력 품질(−1 ~ +1)이 엔진 확률을 보정할 수 있는 상한.
    /// 실제 입력 → 품질 매핑은 Unity 조작 레이어에서 정한다.
    /// </summary>
    public sealed class InputModifierConfig
    {
        /// <summary>투구 릴리스 품질 ±1일 때 제구 오차 로그 배율 최대 변화</summary>
        public double MaxPitchExecutionSigmaLogShift { get; set; } = 0.30;

        /// <summary>스윙 타이밍 품질 +1(좋은 입력)일 때 컨택 로그 오즈 최대 증가</summary>
        public double MaxSwingContactLogitShift { get; set; } = 0.40;

        /// <summary>스윙 타이밍 품질 −1(나쁜 입력)일 때 컨택 로그 오즈 최대 감소 (양수로 지정)</summary>
        public double MaxSwingContactLogitPenalty { get; set; } = 0.40;

        /// <summary>스윙 타이밍 품질 +1(좋은 입력)일 때 정타 로그 오즈 최대 증가</summary>
        public double MaxSwingSolidLogitShift { get; set; } = 0.40;

        /// <summary>스윙 타이밍 품질 −1(나쁜 입력)일 때 정타 로그 오즈 최대 감소 (양수로 지정)</summary>
        public double MaxSwingSolidLogitPenalty { get; set; } = 0.40;

        /// <summary>번트 입력 품질 +1(좋은 입력)일 때 번트 로그 오즈 최대 증가 (컨택·페어·성공 모두). 사람 입력만, AI는 보정 없음</summary>
        public double MaxBuntLogitShift { get; set; } = 0.40;

        /// <summary>번트 입력 품질 −1(나쁜 입력)일 때 번트 로그 오즈 최대 감소 (양수로 지정)</summary>
        public double MaxBuntLogitPenalty { get; set; } = 0.40;

        /// <summary>스윙 타이밍 방향 ±1일 때 타구 방향각 평균 최대 이동 (도). 이르면 당겨치기 쪽</summary>
        public double MaxTimingSprayShiftDeg { get; set; } = 20.0;

        /// <summary>커서 상하 오차 ±1일 때 발사각 최대 변화 (도). 커서가 공보다 위면 낮게</summary>
        public double MaxCursorLaunchAngleShiftDeg { get; set; } = 15.0;

        /// <summary>스윙 품질(−1~+1) → 컨택 로그 오즈 보정. + 쪽은 보상 상한, − 쪽은 벌칙 상한</summary>
        public double SwingContactLogitShift(double quality)
        {
            return Asymmetric(quality, MaxSwingContactLogitShift, MaxSwingContactLogitPenalty);
        }

        /// <summary>스윙 품질(−1~+1) → 정타 로그 오즈 보정. + 쪽은 보상 상한, − 쪽은 벌칙 상한</summary>
        public double SwingSolidLogitShift(double quality)
        {
            return Asymmetric(quality, MaxSwingSolidLogitShift, MaxSwingSolidLogitPenalty);
        }

        /// <summary>번트 품질(−1~+1) → 번트 로그 오즈 보정 (번트 능력치 보정에 더해짐). + 쪽은 보상 상한, − 쪽은 벌칙 상한</summary>
        public double BuntLogitShift(double quality)
        {
            return Asymmetric(quality, MaxBuntLogitShift, MaxBuntLogitPenalty);
        }

        private static double Asymmetric(double quality, double bonus, double penalty)
        {
            double q = quality < -1.0 ? -1.0 : quality > 1.0 ? 1.0 : quality;
            return q >= 0 ? q * bonus : q * penalty;
        }
    }
}
