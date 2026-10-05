using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BaseballSim.Engine.Events;

namespace BaseballSim.BalanceProbe
{
    /// <summary>
    /// 안타 종류(1·2·3루타) 비율: 뜬 안타는 낙하 거리 구간별, 땅볼 안타·홈런은 따로.
    /// 짧게 떨어졌는데 2루타 이상이 된 타구는 몇 개를 자세히 보여준다.
    /// </summary>
    public sealed class HitTypeTable
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        private static readonly double[] DistanceEdges = { 60, 80, 95 };
        private static readonly string[] RowLabels = { "air <60m", "air 60-80", "air 80-95", "air 95+", "ground", "home run" };
        private const int GroundRow = 4;
        private const int HomeRunRow = 5;

        /// <summary>이 거리 미만에 떨어진 장타를 예시로 남긴다</summary>
        private const double ShortExtraBaseM = 80;
        private const int MaxExamples = 12;

        private readonly int[,] _counts = new int[RowLabels.Length, 4];
        private readonly List<string> _examples = new List<string>();
        private int _shortExtraBase;
        private int _shortExtraBaseWithRunners;

        public void Add(PitchEvent ev)
        {
            if (ev.BattedBall == null || !ev.PlateAppearanceOutcome.HasValue || ev.IsBunt)
            {
                return;
            }

            int bases = OutcomeRules.TotalBases(ev.PlateAppearanceOutcome.Value);
            if (bases <= 0)
            {
                return;
            }

            BattedBallData b = ev.BattedBall;
            int row;
            double landing = 0;
            if (bases == 4)
            {
                row = HomeRunRow;
            }
            else if (b.HasLanding)
            {
                landing = Math.Sqrt(b.LandingX * b.LandingX + b.LandingY * b.LandingY);
                row = 0;
                while (row < DistanceEdges.Length && landing >= DistanceEdges[row])
                {
                    row++;
                }
            }
            else
            {
                row = GroundRow;
            }

            _counts[row, bases - 1]++;

            if (b.HasLanding && bases >= 2 && landing < ShortExtraBaseM)
            {
                _shortExtraBase++;
                bool runners = ev.RunnerOnFirst >= 0 || ev.RunnerOnSecond >= 0 || ev.RunnerOnThird >= 0;
                if (runners)
                {
                    _shortExtraBaseWithRunners++;
                }

                if (_examples.Count < MaxExamples)
                {
                    double end = Math.Sqrt(b.EndX * b.EndX + b.EndY * b.EndY);
                    _examples.Add(string.Format(Invariant,
                        "      {0,-7} EV {1,5:0} LA {2,4:0} spray {3,4:0} | land {4,5:0.0} m @ {5:0.00} s | picked {6,5:0.0} m @ {7:0.00} s by {8} (arrive {9:0.00} s) | runners {10}{11}{12} outs {13}{14}",
                        ev.PlateAppearanceOutcome.Value, b.ExitVelocityKmh, b.LaunchAngleDeg, b.SprayAngleDeg, landing,
                        b.LandingTimeS, end, b.FieldedTimeS, b.FieldedBy, b.FielderArrivalS,
                        ev.RunnerOnFirst >= 0 ? "1" : "-", ev.RunnerOnSecond >= 0 ? "2" : "-", ev.RunnerOnThird >= 0 ? "3" : "-",
                        ev.OutsBefore, ev.IsError ? " ERROR" : ""));
                }
            }
        }

        public string Table()
        {
            var text = new StringBuilder();
            text.AppendLine("    hit types by landing distance:   1B / 2B / 3B  (% of row)");
            int[] total = new int[4];
            for (int r = 0; r < RowLabels.Length; r++)
            {
                int n = _counts[r, 0] + _counts[r, 1] + _counts[r, 2] + _counts[r, 3];
                for (int k = 0; k < 4; k++)
                {
                    total[k] += _counts[r, k];
                }

                if (r == HomeRunRow)
                {
                    text.AppendLine(string.Format(Invariant, "    {0,-10} {1,6}", RowLabels[r], n));
                    continue;
                }

                text.AppendLine(string.Format(Invariant, "    {0,-10} {1,6} | {2,5:0.0}% {3,5:0.0}% {4,5:0.0}%", RowLabels[r], n,
                    Pct(_counts[r, 0], n), Pct(_counts[r, 1], n), Pct(_counts[r, 2], n)));
            }

            int hits = total[0] + total[1] + total[2] + total[3];
            text.AppendLine(string.Format(Invariant,
                "    all hits {0}: 1B {1:0.0}%  2B {2:0.0}%  3B {3:0.0}%  HR {4:0.0}%   (MLB approx: 2B 20%, 3B 2%)",
                hits, Pct(total[0], hits), Pct(total[1], hits), Pct(total[2], hits), Pct(total[3], hits)));
            text.AppendLine(string.Format(Invariant, "    extra-base hits landing < {0} m: {1} ({2} with runners on). examples:",
                ShortExtraBaseM, _shortExtraBase, _shortExtraBaseWithRunners));
            foreach (string line in _examples)
            {
                text.AppendLine(line);
            }

            return text.ToString();
        }

        private static double Pct(int count, int total)
        {
            return total > 0 ? 100.0 * count / total : 0.0;
        }
    }
}
