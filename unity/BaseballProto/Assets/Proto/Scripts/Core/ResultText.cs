using BaseballSim.Engine.Events;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.State;

namespace BaseballProto.Core
{
    /// <summary>
    /// 화면 표시 문자열. WebGL에는 시스템 폰트가 없어 한글이 보이지 않으므로 화면 문구는 영문으로 쓴다.
    /// </summary>
    public static class ResultText
    {
        public static string PitchName(PitchType type)
        {
            switch (type)
            {
                case PitchType.FourSeam: return "4-Seam";
                case PitchType.Sinker: return "Sinker";
                case PitchType.Cutter: return "Cutter";
                case PitchType.Slider: return "Slider";
                case PitchType.Curveball: return "Curve";
                case PitchType.Changeup: return "Change";
                case PitchType.Splitter: return "Splitter";
                default: return type.ToString();
            }
        }

        /// <summary>큰 글씨 결과 문구 (타석 결과가 있으면 그것, 없으면 투구 결과)</summary>
        public static string Headline(PitchEvent ev)
        {
            if (ev == null)
            {
                return string.Empty;
            }

            if (ev.PlateAppearanceOutcome.HasValue)
            {
                return Outcome(ev.PlateAppearanceOutcome.Value);
            }

            return Pitch(ev.Result);
        }

        public static string Pitch(PitchResult result)
        {
            switch (result)
            {
                case PitchResult.Ball: return "BALL";
                case PitchResult.CalledStrike: return "STRIKE (looking)";
                case PitchResult.SwingingStrike: return "SWING & MISS";
                case PitchResult.Foul: return "FOUL";
                case PitchResult.InPlay: return "IN PLAY";
                case PitchResult.HitByPitch: return "HIT BY PITCH";
                default: return result.ToString();
            }
        }

        public static string Outcome(PlateAppearanceOutcome outcome)
        {
            switch (outcome)
            {
                case PlateAppearanceOutcome.Single: return "SINGLE";
                case PlateAppearanceOutcome.Double: return "DOUBLE";
                case PlateAppearanceOutcome.Triple: return "TRIPLE";
                case PlateAppearanceOutcome.HomeRun: return "HOME RUN!";
                case PlateAppearanceOutcome.Walk: return "WALK";
                case PlateAppearanceOutcome.IntentionalWalk: return "INTENTIONAL WALK";
                case PlateAppearanceOutcome.HitByPitch: return "HIT BY PITCH";
                case PlateAppearanceOutcome.Strikeout: return "STRIKEOUT";
                case PlateAppearanceOutcome.GroundOut: return "GROUND OUT";
                case PlateAppearanceOutcome.FlyOut: return "FLY OUT";
                case PlateAppearanceOutcome.LineOut: return "LINE OUT";
                case PlateAppearanceOutcome.PopOut: return "POP OUT";
                case PlateAppearanceOutcome.GroundedIntoDoublePlay: return "DOUBLE PLAY";
                case PlateAppearanceOutcome.FieldersChoice: return "FIELDER'S CHOICE";
                case PlateAppearanceOutcome.ReachedOnError: return "ERROR";
                case PlateAppearanceOutcome.SacrificeFly: return "SAC FLY";
                case PlateAppearanceOutcome.SacrificeBunt: return "SAC BUNT";
                default: return outcome.ToString();
            }
        }

        public static string Scoreboard(GameState state)
        {
            if (state == null)
            {
                return string.Empty;
            }

            string half = state.IsTopHalf ? "TOP" : "BOT";
            string bases = (state.Bases[0] != null ? "1" : "-") + (state.Bases[1] != null ? "2" : "-")
                + (state.Bases[2] != null ? "3" : "-");
            return half + " " + state.Inning + "   GUL " + state.AwayScore + " : " + state.HomeScore + " FOX"
                + "   B" + state.Balls + " S" + state.Strikes + " O" + state.Outs + "   [" + bases + "]";
        }
    }
}
