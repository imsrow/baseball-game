namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 리그 성향(득점 환경). 기준 확률과 목표치를 묶어 프리셋으로 교체할 수 있게 한다.
    /// 능력치 50인 타자·투수가 만났을 때의 확률이 이 값들이다.
    /// </summary>
    public sealed class LeagueEnvironment
    {
        public string Name { get; set; } = "Standard";

        /// <summary>하네스로 목표치에 맞춰 튜닝을 마친 프리셋인지</summary>
        public bool IsCalibrated { get; set; }

        public LeagueTargets Targets { get; set; } = new LeagueTargets();

        /// <summary>카운트별 투수가 존 안을 노릴 확률</summary>
        public CountTable ZoneIntentByCount { get; set; } = CountTable.Uniform(0.55);

        /// <summary>타자가 인지한 구역별 기준 스윙률</summary>
        public RegionTable SwingRateByPerceivedRegion { get; set; } = new RegionTable();

        /// <summary>실제 구역별 스윙당 컨택률</summary>
        public RegionTable ContactRateByRegion { get; set; } = new RegionTable();

        /// <summary>실제 구역별 컨택당 파울 비율</summary>
        public RegionTable FoulRateByRegion { get; set; } = new RegionTable();

        /// <summary>인플레이 타구 중 정타(솔리드 컨택) 비율</summary>
        public double SolidContactRate { get; set; } = 0.55;

        /// <summary>정타 평균 타구속도 (km/h)</summary>
        public double SolidExitVelocityKmh { get; set; } = 155.0;

        /// <summary>빗맞은 타구 평균 타구속도 (km/h)</summary>
        public double WeakExitVelocityKmh { get; set; } = 125.0;

        /// <summary>타구 비거리 보정. 1보다 크면 공기저항이 줄어 멀리 날아간다</summary>
        public double CarryFactor { get; set; } = 1.0;

        /// <summary>몸쪽 몸맞는 선을 넘은 투구가 실제로 몸에 맞을 확률</summary>
        public double HitByPitchProbability { get; set; } = 0.5;
    }
}
