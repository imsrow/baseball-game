namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 리그 성향 프리셋.
    /// Standard만 하네스로 튜닝되어 있고(A단계 기준, 시드 5개 평균), 투고타저·타고투저는 목표치와 대략적인 기준값만 둔 상태다.
    /// </summary>
    public static class LeagueEnvironmentPresets
    {
        /// <summary>표준: AVG .245 / OBP .315 / SLG .400, K 22%, BB 8.5%, HR 3.0%, BABIP .290</summary>
        public static LeagueEnvironment Standard()
        {
            return new LeagueEnvironment
            {
                Name = "Standard",
                IsCalibrated = true,
                Targets = new LeagueTargets(),
                ZoneIntentByCount = new CountTable(
                    0.58, 0.50, 0.32,
                    0.64, 0.55, 0.40,
                    0.72, 0.63, 0.48,
                    0.85, 0.76, 0.65),
                // Statcast 2023 구역별 스윙률 근사: Heart 73%, Shadow 52%, Chase 24%, Waste 6%
                SwingRateByPerceivedRegion = new RegionTable(0.80, 0.55, 0.19, 0.05),
                // 구역별 컨택률 (Statcast 2023 근사에서 출발해 튜닝)
                ContactRateByRegion = new RegionTable(0.91, 0.77, 0.40, 0.28),
                FoulRateByRegion = new RegionTable(0.46, 0.56, 0.58, 0.62),
                SolidContactRate = 0.55,
                // 원본 96.4 mph
                SolidExitVelocityKmh = 155.2,
                // 원본 79.5 mph
                WeakExitVelocityKmh = 128.0,
                CarryFactor = 1.0,
                HitByPitchProbability = 0.55,
            };
        }

        /// <summary>투고타저 (미튜닝): 목표 AVG .235 / OBP .300 / SLG .370</summary>
        public static LeagueEnvironment PitcherFriendly()
        {
            LeagueEnvironment env = Standard();
            env.Name = "PitcherFriendly";
            env.IsCalibrated = false;
            env.Targets.Avg = 0.235;
            env.Targets.Obp = 0.300;
            env.Targets.Slg = 0.370;
            env.Targets.StrikeoutRate = 0.235;
            env.Targets.WalkRate = 0.080;
            env.Targets.HomeRunRate = 0.024;
            env.Targets.Babip = 0.283;
            env.ContactRateByRegion = new RegionTable(0.86, 0.76, 0.56, 0.38);
            env.SolidContactRate = 0.52;
            env.CarryFactor = 0.97;
            return env;
        }

        /// <summary>타고투저 (미튜닝): 목표 AVG .270 / OBP .340 / SLG .440</summary>
        public static LeagueEnvironment HitterFriendly()
        {
            LeagueEnvironment env = Standard();
            env.Name = "HitterFriendly";
            env.IsCalibrated = false;
            env.Targets.Avg = 0.270;
            env.Targets.Obp = 0.340;
            env.Targets.Slg = 0.440;
            env.Targets.StrikeoutRate = 0.19;
            env.Targets.WalkRate = 0.090;
            env.Targets.HomeRunRate = 0.034;
            env.Targets.Babip = 0.310;
            env.ContactRateByRegion = new RegionTable(0.89, 0.81, 0.61, 0.43);
            env.SolidContactRate = 0.59;
            env.CarryFactor = 1.03;
            return env;
        }

        /// <summary>이름으로 프리셋 검색 (대소문자 무시). 없으면 null</summary>
        public static LeagueEnvironment ByName(string name)
        {
            switch ((name ?? string.Empty).ToLowerInvariant())
            {
                case "standard": return Standard();
                case "pitcherfriendly": return PitcherFriendly();
                case "hitterfriendly": return HitterFriendly();
                default: return null;
            }
        }
    }
}
