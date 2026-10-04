using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.Players
{
    /// <summary>
    /// 선수. 능력치 묶음과 기본 정보만 가진다 (경기 중 상태는 State 쪽에 둔다).
    /// </summary>
    public sealed class Player
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public BatSide Bats { get; set; } = BatSide.Right;

        public Hand Throws { get; set; } = Hand.Right;

        public Position PrimaryPosition { get; set; } = Position.DesignatedHitter;

        public BatterRatings Batting { get; set; } = new BatterRatings();

        /// <summary>투수 능력치 (야수는 null)</summary>
        public PitcherRatings Pitching { get; set; }

        public FielderRatings Fielding { get; set; } = new FielderRatings();

        public bool IsPitcher => Pitching != null;

        /// <summary>상대 투수 손에 따른 실제 타석 방향 (양타는 반대편)</summary>
        public Hand BattingHandAgainst(Hand pitcherThrows)
        {
            switch (Bats)
            {
                case BatSide.Left: return Hand.Left;
                case BatSide.Right: return Hand.Right;
                default: return pitcherThrows == Hand.Right ? Hand.Left : Hand.Right;
            }
        }

        public override string ToString()
        {
            return Name + "#" + Id;
        }
    }
}
