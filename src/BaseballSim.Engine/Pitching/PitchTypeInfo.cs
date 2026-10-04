namespace BaseballSim.Engine.Pitching
{
    /// <summary>
    /// 구종 정적 정보
    /// </summary>
    public static class PitchTypeInfo
    {
        public const int Count = 7;

        public static readonly PitchType[] All =
        {
            PitchType.FourSeam, PitchType.Sinker, PitchType.Cutter, PitchType.Slider,
            PitchType.Curveball, PitchType.Changeup, PitchType.Splitter,
        };

        public static PitchFamily FamilyOf(PitchType type)
        {
            switch (type)
            {
                case PitchType.FourSeam:
                case PitchType.Sinker:
                case PitchType.Cutter:
                    return PitchFamily.Fastball;
                case PitchType.Slider:
                case PitchType.Curveball:
                    return PitchFamily.Breaking;
                default:
                    return PitchFamily.Offspeed;
            }
        }

        /// <summary>약어 (Statcast 표기)</summary>
        public static string Abbreviation(PitchType type)
        {
            switch (type)
            {
                case PitchType.FourSeam: return "FF";
                case PitchType.Sinker: return "SI";
                case PitchType.Cutter: return "FC";
                case PitchType.Slider: return "SL";
                case PitchType.Curveball: return "CU";
                case PitchType.Changeup: return "CH";
                default: return "FS";
            }
        }
    }
}
