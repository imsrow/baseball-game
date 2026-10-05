using System;
using System.Collections.Generic;
using System.Globalization;
using BaseballProto.Core;
using BaseballProto.Quality;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Events;

namespace BaseballProto.UI
{
    /// <summary>
    /// 디버그 표시 문구: 입력 오차 → 보정값(엔진 입력과 실제 확률 보정량) → 결과
    /// </summary>
    public static class DebugReadout
    {
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static List<string> Build(DuelController duel, double inputLagMs, float fps)
        {
            var lines = new List<string>();
            InputModifierConfig mod = duel.Config.InputModifier;
            lines.Add(F("FPS {0:0}   input lag {1:0.0} ms   flight {2:0.000} s", fps, inputLagMs, duel.LastFlightTimeS));

            if (duel.Mode == DuelMode.Batting)
            {
                AddBatting(lines, duel.LastBatting, mod);
                AddBias(lines, duel.SwingBias, duel.Calibration);
            }
            else
            {
                AddPitching(lines, duel.LastPitching, duel.LastEvent, mod);
            }

            AddResult(lines, duel.LastEvent);
            if (!string.IsNullOrEmpty(duel.Message))
            {
                lines.Add(duel.Message);
            }

            return lines;
        }

        private static void AddBatting(List<string> lines, BattingJudgement j, InputModifierConfig mod)
        {
            if (j == null)
            {
                lines.Add("Swing: (none / take)");
                return;
            }

            BatterAction a = j.Action;
            string when = Math.Abs(j.TimingErrorMs) < 0.5 ? "on time" : j.TimingErrorMs < 0 ? "early" : "late";
            lines.Add(F("Timing {0:+0.0;-0.0} ms ({1})  score {2:0.00}", j.TimingErrorMs, when, j.TimingScore));
            lines.Add(F("Cursor dx {0:+0.000;-0.000} dz {1:+0.000;-0.000} m  d/R {2:0.00}  score {3:0.00}",
                j.CursorDx, j.CursorDz, j.CursorDistanceRatio, j.CursorScore));
            lines.Add(F("Combined {0:0.00} = {1:0.00} x T{2:0.00} + {3:0.00} x C{4:0.00}", j.CombinedScore, j.TimingWeight,
                j.TimingScore, 1.0 - j.TimingWeight, j.CursorScore));
            double q = a.TimingQuality ?? 0;
            double dir = a.TimingDirection ?? 0;
            double cv = a.CursorVerticalOffset ?? 0;
            lines.Add(F("-> TimingQuality {0:+0.00;-0.00}  Dir {1:+0.00;-0.00}  CursorV {2:+0.00;-0.00}", q, dir, cv));
            lines.Add(F("   contact {0:+0.00;-0.00} / solid {1:+0.00;-0.00} logit   pull {2:+0.0;-0.0} deg   LA {3:+0.0;-0.0} deg",
                mod.SwingContactLogitShift(q), mod.SwingSolidLogitShift(q),
                -dir * mod.MaxTimingSprayShiftDeg, -cv * mod.MaxCursorLaunchAngleShiftDeg));
        }

        private static void AddBias(List<string> lines, SwingBiasTracker bias, TimingCalibration calib)
        {
            string calibText = F("calib {0:+0;-0;0} ms{1}", calib.CurrentMs, calib.Measuring ? " (measuring)" : "");
            if (bias.Count == 0)
            {
                lines.Add(calibText);
                return;
            }

            lines.Add(F("Last {0} swings avg: timing {1:+0;-0} ms  dz {2:+0.000;-0.000} m   {3}", bias.Count,
                bias.MeanTimingErrorMs, bias.MeanCursorDz, calibText));
        }

        private static void AddPitching(List<string> lines, PitchJudgement j, PitchEvent ev, InputModifierConfig mod)
        {
            if (j == null)
            {
                lines.Add("Release: (waiting)");
                return;
            }

            double q = j.Call.ReleaseQuality ?? 0;
            string gauge = j.GaugeValue.HasValue ? F("{0:0.000}", j.GaugeValue.Value) : "timeout";
            lines.Add(F("Gauge {0}  err {1:0.000}  score {2:0.00}", gauge, j.GaugeError, j.ReleaseScore));
            lines.Add(F("-> ReleaseQuality {0:+0.00;-0.00}   control sigma x{1:0.00}", q,
                Math.Exp(-q * mod.MaxPitchExecutionSigmaLogShift)));
            if (ev != null)
            {
                double miss = Math.Sqrt(Sq(ev.PlateX - ev.TargetX) + Sq(ev.PlateZ - ev.TargetZ));
                lines.Add(F("Target ({0:0.00},{1:0.00})  actual ({2:0.00},{3:0.00})  miss {4:0.000} m",
                    ev.TargetX, ev.TargetZ, ev.PlateX, ev.PlateZ, miss));
            }
        }

        private static void AddResult(List<string> lines, PitchEvent ev)
        {
            if (ev == null)
            {
                return;
            }

            lines.Add(F("{0} {1:0.0} km/h  ({2:0.00},{3:0.00}) {4}  {5}", ResultText.PitchName(ev.PitchType), ev.VelocityKmh,
                ev.PlateX, ev.PlateZ, ev.IsInZone ? "zone" : "out", ev.Swung ? "swing" : "take"));
            string result = ResultText.Pitch(ev.Result);
            if (ev.BattedBall != null)
            {
                BattedBallData b = ev.BattedBall;
                result += F("  EV {0:0} km/h  LA {1:0} deg  spray {2:+0;-0} deg  {3}{4}  {5:0} m", b.ExitVelocityKmh,
                    b.LaunchAngleDeg, b.SprayAngleDeg, b.Type, b.IsSolid ? " (solid)" : "", b.DistanceM);
            }

            if (ev.PlateAppearanceOutcome.HasValue)
            {
                result += "  => " + ResultText.Outcome(ev.PlateAppearanceOutcome.Value);
            }

            lines.Add(result);
        }

        private static double Sq(double v)
        {
            return v * v;
        }

        private static string F(string format, params object[] args)
        {
            return string.Format(Invariant, format, args);
        }
    }
}
