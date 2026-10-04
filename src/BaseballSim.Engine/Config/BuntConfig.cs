namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 번트 (단순 확률 모델)
    /// </summary>
    public sealed class BuntConfig
    {
        /// <summary>번트 50 타자가 번트를 대 공을 맞힐 확률 (존 안)</summary>
        public double ContactRate { get; set; } = 0.80;

        /// <summary>존 밖 공에 번트를 댈 때 맞힐 확률 로그 오즈 보정</summary>
        public double OutOfZoneContactShift { get; set; } = -1.2;

        /// <summary>맞힌 번트 중 파울 비율</summary>
        public double FoulRate { get; set; } = 0.35;

        /// <summary>번트 능력치 1 표준편차당 컨택·페어·성공 로그 오즈 증가</summary>
        public double BuntBeta { get; set; } = 0.40;

        /// <summary>희생번트 성공률 (타자 아웃, 주자 진루)</summary>
        public double SacrificeSuccessRate { get; set; } = 0.72;

        /// <summary>희생번트 시도가 내야안타가 될 확률</summary>
        public double SacrificeHitRate { get; set; } = 0.05;

        /// <summary>희생번트 실패 중 선행 주자가 잡히는 비율 (나머지는 뜬공 아웃·주자 그대로)</summary>
        public double SacrificeFailLeadRunnerOutShare { get; set; } = 0.70;

        /// <summary>기습번트 안타 확률 (스피드·번트 50)</summary>
        public double BuntForHitRate { get; set; } = 0.40;

        /// <summary>스피드 1 표준편차당 번트 안타 로그 오즈 증가</summary>
        public double SpeedBeta { get; set; } = 0.40;

        /// <summary>표시용 번트 타구속도 범위 (km/h)</summary>
        public double MinExitVelocityKmh { get; set; } = 35.0;

        public double MaxExitVelocityKmh { get; set; } = 70.0;
    }
}
