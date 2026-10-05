using System.Globalization;
using BaseballSim.Engine.Events;

namespace BaseballProto.Core
{
    /// <summary>
    /// 이번 경기 성적 (사람 쪽 기준: 타격 모드는 내 타격, 투구 모드는 내가 상대한 AI 타자).
    /// 사람 역할을 양 팀 모두에 배정하므로 경기의 모든 타석이 집계 대상이다.
    /// </summary>
    public sealed class GameStatLine
    {
        public int PlateAppearances { get; private set; }

        public int AtBats { get; private set; }

        public int Hits { get; private set; }

        public int Strikeouts { get; private set; }

        public int Walks { get; private set; }

        public double Average => AtBats > 0 ? (double)Hits / AtBats : 0.0;

        public void Reset()
        {
            PlateAppearances = 0;
            AtBats = 0;
            Hits = 0;
            Strikeouts = 0;
            Walks = 0;
        }

        public void Add(GameEvent ev)
        {
            if (ev is IntentionalWalkEvent)
            {
                PlateAppearances++;
                Walks++;
                return;
            }

            if (!(ev is PitchEvent pitch) || !pitch.PlateAppearanceOutcome.HasValue)
            {
                return;
            }

            PlateAppearances++;
            switch (pitch.PlateAppearanceOutcome.Value)
            {
                case PlateAppearanceOutcome.Single:
                case PlateAppearanceOutcome.Double:
                case PlateAppearanceOutcome.Triple:
                case PlateAppearanceOutcome.HomeRun:
                    AtBats++;
                    Hits++;
                    break;
                case PlateAppearanceOutcome.Walk:
                case PlateAppearanceOutcome.IntentionalWalk:
                    Walks++;
                    break;
                case PlateAppearanceOutcome.Strikeout:
                    AtBats++;
                    Strikeouts++;
                    break;
                case PlateAppearanceOutcome.HitByPitch:
                case PlateAppearanceOutcome.SacrificeFly:
                case PlateAppearanceOutcome.SacrificeBunt:
                    break;
                default:
                    AtBats++;
                    break;
            }
        }

        public override string ToString()
        {
            string avg = AtBats > 0 ? Average.ToString(".000", CultureInfo.InvariantCulture) : "---";
            return "PA " + PlateAppearances + "  H " + Hits + "  AVG " + avg + "  K " + Strikeouts + "  BB " + Walks;
        }
    }
}
