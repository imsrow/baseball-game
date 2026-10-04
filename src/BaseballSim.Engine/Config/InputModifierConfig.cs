namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 사람 조작 입력 품질(−1 ~ +1)이 엔진 확률을 보정할 수 있는 상한.
    /// 1단계에서는 필드와 상한만 정의한다. 실제 입력 → 품질 매핑은 Unity 조작 단계에서 정한다.
    /// </summary>
    public sealed class InputModifierConfig
    {
        /// <summary>투구 릴리스 품질 ±1일 때 제구 오차 로그 배율 최대 변화</summary>
        public double MaxPitchExecutionSigmaLogShift { get; set; } = 0.30;

        /// <summary>스윙 타이밍 품질 ±1일 때 컨택 로그 오즈 최대 변화</summary>
        public double MaxSwingContactLogitShift { get; set; } = 0.40;

        /// <summary>스윙 타이밍 품질 ±1일 때 정타 로그 오즈 최대 변화</summary>
        public double MaxSwingSolidLogitShift { get; set; } = 0.40;
    }
}
