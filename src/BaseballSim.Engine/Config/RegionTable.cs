using BaseballSim.Engine.Pitching;

namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 공략 구역(Heart/Shadow/Chase/Waste)별 값 표
    /// </summary>
    public sealed class RegionTable
    {
        public RegionTable()
        {
        }

        public RegionTable(double heart, double shadow, double chase, double waste)
        {
            Heart = heart;
            Shadow = shadow;
            Chase = chase;
            Waste = waste;
        }

        public double Heart { get; set; }

        public double Shadow { get; set; }

        public double Chase { get; set; }

        public double Waste { get; set; }

        public double Get(AttackRegion region)
        {
            switch (region)
            {
                case AttackRegion.Heart: return Heart;
                case AttackRegion.Shadow: return Shadow;
                case AttackRegion.Chase: return Chase;
                default: return Waste;
            }
        }
    }
}
