namespace BaseballSim.Engine.Pitching
{
    /// <summary>
    /// 실제로 던져진 공 (엔진이 결정한 결과)
    /// </summary>
    public sealed class ExecutedPitch
    {
        public PitchType Type { get; set; }

        /// <summary>투수가 노린 지점</summary>
        public PlateLocation Target { get; set; }

        /// <summary>실제 통과 위치</summary>
        public PlateLocation Actual { get; set; }

        public double VelocityKmh { get; set; }

        /// <summary>실효 구위 z (구위 + 구속 기여 − 피로)</summary>
        public double EffectiveStuffZ { get; set; }

        public AttackRegion Region { get; set; }

        public bool IsInZone { get; set; }
    }
}
