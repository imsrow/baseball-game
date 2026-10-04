using BaseballSim.Engine.Pitching;

namespace BaseballSim.Engine.Batting
{
    /// <summary>
    /// 타자가 인지한 투구 (선구안에 따른 인지 오차 적용 후)
    /// </summary>
    public sealed class PerceivedPitch
    {
        public PlateLocation Location { get; set; }

        public AttackRegion Region { get; set; }

        public bool IsInZone { get; set; }
    }
}
