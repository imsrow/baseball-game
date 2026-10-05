using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Players;

namespace BaseballSim.BalanceProbe
{
    /// <summary>
    /// 타구속도 × 발사각 구간별 안타율·장타율 (타구 = 인플레이로 타석이 끝난 공. 실책 출루는 아웃 취급).
    /// 강한 라인드라이브(161 km/h+, 10~25도)의 아웃은 비거리·처리 수비수로 따로 본다.
    /// </summary>
    public sealed class BattedBallGrid
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        // km/h: 80 / 90 / 95 / 100 / 105 mph 경계
        private static readonly double[] EvEdges = { 129, 145, 153, 161, 169 };
        private static readonly string[] EvLabels = { "<129", "129-145", "145-153", "153-161", "161-169", "169+" };

        private static readonly double[] LaEdges = { 10, 25, 35, 50 };
        private static readonly string[] LaLabels = { "LA <10", "LA 10-25", "LA 25-35", "LA 35-50", "LA 50+" };

        // 강한 라인드라이브 아웃 비거리 구간 (m)
        private static readonly double[] DistanceEdges = { 40, 60, 80, 95, 110 };
        private static readonly string[] DistanceLabels = { "<40", "40-60", "60-80", "80-95", "95-110", "110+" };

        private const double StrongEvKmh = 161;
        private const double LineDriveMinDeg = 10;
        private const double LineDriveMaxDeg = 25;

        private readonly Cell[,] _cells = new Cell[LaLabels.Length, EvLabels.Length];
        private readonly int[] _strongLinerOutDistance = new int[DistanceLabels.Length];
        private readonly Dictionary<Position, int> _strongLinerOutFielder = new Dictionary<Position, int>();
        private int _strongLiners;
        private int _strongLinerOuts;
        private double _outHangSum, _outDistanceSum, _hitHangSum, _hitDistanceSum;

        private struct Cell
        {
            public int Balls;
            public int Hits;
            public int TotalBases;
        }

        public void Add(PitchEvent ev)
        {
            if (ev.BattedBall == null || !ev.PlateAppearanceOutcome.HasValue || ev.IsBunt)
            {
                return;
            }

            BattedBallData b = ev.BattedBall;
            int bases = OutcomeRules.TotalBases(ev.PlateAppearanceOutcome.Value);
            int evBin = Bin(b.ExitVelocityKmh, EvEdges);
            int la = Bin(b.LaunchAngleDeg, LaEdges);
            _cells[la, evBin].Balls++;
            if (bases > 0)
            {
                _cells[la, evBin].Hits++;
                _cells[la, evBin].TotalBases += bases;
            }

            if (b.ExitVelocityKmh >= StrongEvKmh && b.LaunchAngleDeg >= LineDriveMinDeg && b.LaunchAngleDeg < LineDriveMaxDeg)
            {
                _strongLiners++;
                if (bases > 0)
                {
                    _hitHangSum += b.HangTimeS;
                    _hitDistanceSum += b.DistanceM;
                }
                else
                {
                    _strongLinerOuts++;
                    _outHangSum += b.HangTimeS;
                    _outDistanceSum += b.DistanceM;
                    _strongLinerOutDistance[Bin(b.DistanceM, DistanceEdges)]++;
                    if (b.FieldedBy.HasValue)
                    {
                        _strongLinerOutFielder.TryGetValue(b.FieldedBy.Value, out int n);
                        _strongLinerOutFielder[b.FieldedBy.Value] = n + 1;
                    }
                }
            }
        }

        public string Table()
        {
            var text = new StringBuilder();
            text.AppendLine("    EV x LA: AVG / SLG (balls)   EV km/h ->");
            text.Append("    ").Append(string.Format(Invariant, "{0,-9}", ""));
            foreach (string label in EvLabels)
            {
                text.Append(string.Format(Invariant, " | {0,-19}", label));
            }

            text.AppendLine();
            for (int la = 0; la < LaLabels.Length; la++)
            {
                text.Append("    ").Append(string.Format(Invariant, "{0,-9}", LaLabels[la]));
                for (int e = 0; e < EvLabels.Length; e++)
                {
                    Cell c = _cells[la, e];
                    text.Append(c.Balls == 0
                        ? " | " + "-".PadRight(19)
                        : string.Format(Invariant, " | {0:.000} / {1:0.000} ({2,5})", Ratio(c.Hits, c.Balls),
                            Ratio(c.TotalBases, c.Balls), c.Balls));
                }

                text.AppendLine();
            }

            text.AppendLine(string.Format(Invariant, "    strong liners (EV {0}+, LA {1}-{2}): {3}, outs {4} ({5:0.0}%)", StrongEvKmh,
                LineDriveMinDeg, LineDriveMaxDeg, _strongLiners, _strongLinerOuts, 100.0 * Ratio(_strongLinerOuts, _strongLiners)));
            int strongHits = _strongLiners - _strongLinerOuts;
            text.AppendLine(string.Format(Invariant, "      avg hang/dist: outs {0:0.00} s {1:0} m, hits {2:0.00} s {3:0} m",
                Ratio(_outHangSum, _strongLinerOuts), Ratio(_outDistanceSum, _strongLinerOuts), Ratio(_hitHangSum, strongHits),
                Ratio(_hitDistanceSum, strongHits)));
            text.Append("      out distance m:");
            for (int i = 0; i < DistanceLabels.Length; i++)
            {
                text.Append(string.Format(Invariant, "  {0} {1}", DistanceLabels[i], _strongLinerOutDistance[i]));
            }

            text.AppendLine();
            text.Append("      fielded by:");
            foreach (KeyValuePair<Position, int> pair in _strongLinerOutFielder.OrderByDescending(p => p.Value))
            {
                text.Append(string.Format(Invariant, "  {0} {1}", pair.Key, pair.Value));
            }

            text.AppendLine();
            return text.ToString();
        }

        private static int Bin(double value, double[] edges)
        {
            int i = 0;
            while (i < edges.Length && value >= edges[i])
            {
                i++;
            }

            return i;
        }

        private static double Ratio(double numerator, double denominator)
        {
            return denominator > 0 ? numerator / denominator : 0.0;
        }
    }
}
