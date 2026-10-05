using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Pitching;

namespace BaseballSim.BalanceProbe
{
    /// <summary>
    /// 시나리오 하나의 전체·구역별 집계와 표 출력
    /// </summary>
    public sealed class ProbeStats
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        private static readonly AttackRegion[] Regions = { AttackRegion.Heart, AttackRegion.Shadow, AttackRegion.Chase, AttackRegion.Waste };

        private readonly RegionStats[] _regions = Regions.Select(_ => new RegionStats()).ToArray();
        private int _pa, _ab, _hits, _totalBases, _homeRuns, _strikeouts, _walks, _hitByPitch, _sacFlies;
        private int _pitches, _inZone, _outZone, _swings, _contacts, _outSwings;

        public static string Header =>
            "scenario                               |     PA |  AVG |  OBP |  SLG |   K% |  BB% |  HR% | BABIP | Zone% | Swing% | Contact% | O-Swing%";

        public static string RegionHeader =>
            "    region | pitch% | swing% | whiff%/sw | inplay%/p |  AVG |  SLG |  HR% | EV km/h | LA deg | solid%";

        public void Add(IEnumerable<GameEvent> log)
        {
            foreach (PitchEvent ev in log.OfType<PitchEvent>())
            {
                AddPitch(ev);
            }
        }

        public string Row(string label)
        {
            double avg = Ratio(_hits, _ab);
            double obp = Ratio(_hits + _walks + _hitByPitch, _ab + _walks + _hitByPitch + _sacFlies);
            double babip = Ratio(_hits - _homeRuns, _ab - _strikeouts - _homeRuns + _sacFlies);
            return string.Format(Invariant,
                "{0,-38} | {1,6} | {2:.000} | {3:.000} | {4:.000} | {5,4:0.0} | {6,4:0.0} | {7,4:0.0} | {8,5:.000} | {9,5:0.0} | {10,6:0.0} | {11,8:0.0} | {12,8:0.0}",
                label, _pa, avg, obp, Ratio(_totalBases, _ab), Pct(_strikeouts, _pa), Pct(_walks, _pa), Pct(_homeRuns, _pa), babip,
                Pct(_inZone, _pitches), Pct(_swings, _pitches), Pct(_contacts, _swings), Pct(_outSwings, _outZone));
        }

        public string RegionRows()
        {
            var text = new StringBuilder();
            for (int i = 0; i < Regions.Length; i++)
            {
                RegionStats r = _regions[i];
                text.AppendLine(string.Format(Invariant,
                    "    {0,-6} | {1,6:0.0} | {2,6:0.0} | {3,9:0.0} | {4,9:0.0} | {5:.000} | {6:.000} | {7,4:0.0} | {8,7:0.0} | {9,6:0.0} | {10,6:0.0}",
                    Regions[i], Pct(r.Pitches, _pitches), Pct(r.Swings, r.Pitches), Pct(r.Whiffs, r.Swings), Pct(r.InPlay, r.Pitches),
                    Ratio(r.Hits, r.AtBats), Ratio(r.TotalBases, r.AtBats), Pct(r.HomeRuns, r.PlateAppearances),
                    Ratio(r.ExitVelocitySum, r.BattedBalls), Ratio(r.LaunchAngleSum, r.BattedBalls), Pct(r.SolidBalls, r.BattedBalls)));
            }

            return text.ToString();
        }

        private void AddPitch(PitchEvent ev)
        {
            _pitches++;
            _regions[(int)ev.Region].Add(ev);
            if (ev.IsInZone)
            {
                _inZone++;
            }
            else
            {
                _outZone++;
            }

            if (ev.Swung && !ev.IsBunt)
            {
                _swings++;
                if (!ev.IsInZone)
                {
                    _outSwings++;
                }

                if (ev.Result == PitchResult.Foul || ev.Result == PitchResult.InPlay)
                {
                    _contacts++;
                }
            }

            if (!ev.PlateAppearanceOutcome.HasValue)
            {
                return;
            }

            PlateAppearanceOutcome outcome = ev.PlateAppearanceOutcome.Value;
            _pa++;
            int bases = OutcomeRules.TotalBases(outcome);
            if (OutcomeRules.IsAtBat(outcome))
            {
                _ab++;
            }

            if (bases > 0)
            {
                _hits++;
                _totalBases += bases;
            }

            if (outcome == PlateAppearanceOutcome.HomeRun)
            {
                _homeRuns++;
            }
            else if (outcome == PlateAppearanceOutcome.Strikeout)
            {
                _strikeouts++;
            }
            else if (OutcomeRules.IsWalk(outcome))
            {
                _walks++;
            }
            else if (outcome == PlateAppearanceOutcome.HitByPitch)
            {
                _hitByPitch++;
            }
            else if (outcome == PlateAppearanceOutcome.SacrificeFly)
            {
                _sacFlies++;
            }
        }

        private static double Ratio(double numerator, double denominator)
        {
            return denominator > 0 ? numerator / denominator : 0.0;
        }

        private static double Pct(double numerator, double denominator)
        {
            return 100.0 * Ratio(numerator, denominator);
        }
    }
}
