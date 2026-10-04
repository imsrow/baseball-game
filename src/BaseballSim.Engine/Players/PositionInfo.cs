namespace BaseballSim.Engine.Players
{
    /// <summary>
    /// 포지션 관련 정적 정보
    /// </summary>
    public static class PositionInfo
    {
        /// <summary>Position 열거값 개수</summary>
        public const int Count = 10;

        /// <summary>실제 수비를 서는 9개 포지션</summary>
        public static readonly Position[] Fielding =
        {
            Position.Pitcher, Position.Catcher, Position.FirstBase, Position.SecondBase, Position.ThirdBase,
            Position.Shortstop, Position.LeftField, Position.CenterField, Position.RightField,
        };

        /// <summary>투수를 제외한 수비 포지션 (라인업 구성용)</summary>
        public static readonly Position[] NonPitcherFielding =
        {
            Position.Catcher, Position.FirstBase, Position.SecondBase, Position.ThirdBase,
            Position.Shortstop, Position.LeftField, Position.CenterField, Position.RightField,
        };

        public static bool IsInfield(Position position)
        {
            return position == Position.FirstBase || position == Position.SecondBase
                || position == Position.ThirdBase || position == Position.Shortstop
                || position == Position.Pitcher || position == Position.Catcher;
        }

        public static bool IsOutfield(Position position)
        {
            return position == Position.LeftField || position == Position.CenterField || position == Position.RightField;
        }

        /// <summary>약어 (표시용)</summary>
        public static string Abbreviation(Position position)
        {
            switch (position)
            {
                case Position.Pitcher: return "P";
                case Position.Catcher: return "C";
                case Position.FirstBase: return "1B";
                case Position.SecondBase: return "2B";
                case Position.ThirdBase: return "3B";
                case Position.Shortstop: return "SS";
                case Position.LeftField: return "LF";
                case Position.CenterField: return "CF";
                case Position.RightField: return "RF";
                default: return "DH";
            }
        }
    }
}
