using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BaseballSim.Engine.Batting;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Stats;
using BaseballSim.Engine.Units;

namespace BaseballSim.Harness
{
    /// <summary>
    /// 시드별 시즌 결과를 모아 목표치 대비 표를 만든다
    /// </summary>
    public static class CalibrationReport
    {
        public static string Build(LeagueConfig config, IReadOnlyList<SeasonResult> results, out bool allPassed)
        {
            LeagueTargets t = config.Environment.Targets;
            var sb = new StringBuilder();
            int gamesPerTeam = config.Season.GamesPerTeam;
            sb.AppendLine("=== 검증 하네스: " + config.Environment.Name + " 프리셋"
                + (config.Environment.IsCalibrated ? "" : " (미튜닝 표시)") + " | "
                + config.Season.TeamCount + "팀 × 팀당 " + gamesPerTeam + "경기 | 시드 "
                + string.Join(",", results.Select(r => r.Seed)) + " ===");
            sb.AppendLine("시즌당 경기 " + results[0].Games + " | 소요 "
                + string.Join(" / ", results.Select(r => r.ElapsedSeconds.ToString("0.0", CultureInfo.InvariantCulture) + "s")));
            sb.AppendLine();

            var main = new List<MetricRow>
            {
                Row("AVG", results, s => s.League.Avg, t.Avg, t.AvgTolerance, true),
                Row("OBP", results, s => s.League.Obp, t.Obp, t.ObpTolerance, true),
                Row("SLG", results, s => s.League.Slg, t.Slg, t.SlgTolerance, true),
                Row("K%", results, s => s.League.StrikeoutRate, t.StrikeoutRate, t.StrikeoutRateTolerance, false),
                Row("BB%", results, s => s.League.WalkRate, t.WalkRate, t.WalkRateTolerance, false),
                Row("HR%", results, s => s.League.HomeRunRate, t.HomeRunRate, t.HomeRunRateTolerance, false),
            };

            sb.AppendLine("[목표 지표]");
            sb.AppendLine(string.Format("{0,-8}{1,9}{2,9}{3,9}{4,9}{5,9}  {6}", "지표", "목표", "평균", "표준편차", "차이", "허용", "판정"));
            foreach (MetricRow row in main)
            {
                sb.AppendLine(string.Format("{0,-8}{1,9}{2,9}{3,9}{4,9}{5,9}  {6}",
                    row.Name, Fmt(row.Target.Value, row.IsRateStat), Fmt(row.Mean, row.IsRateStat),
                    Fmt(row.StandardDeviation, row.IsRateStat), Signed(row.Mean - row.Target.Value, row.IsRateStat),
                    "±" + Fmt(row.Tolerance.Value, row.IsRateStat), row.Passed ? "통과" : "실패"));
            }

            allPassed = main.All(r => r.Passed);
            sb.AppendLine("종합: " + (allPassed ? "모든 목표 지표 허용 범위 내" : "허용 범위를 벗어난 지표 있음"));
            sb.AppendLine();

            sb.AppendLine("[참고 지표] 판정 없음. 시드 평균 ± 표준편차 (참고값: MLB 2023 근사)");
            AppendAux(sb, "BABIP", results, r => r.Stats.League.Babip, "0.000", t.Babip.ToString(".000", CultureInfo.InvariantCulture));
            AppendAux(sb, "경기당 득점(팀)", results, r => (double)r.Stats.Diagnostics.Runs / r.TeamGames, "0.00", "4.6");
            AppendAux(sb, "타석당 투구 수", results, r => (double)r.Stats.Diagnostics.Pitches / r.Stats.League.PlateAppearances, "0.00",
                t.ReferencePitchesPerPlateAppearance.ToString("0.0", CultureInfo.InvariantCulture));
            AppendAux(sb, "Zone%", results, r => r.Stats.Diagnostics.ZoneRate, "P", "49%");
            AppendAux(sb, "Swing%", results, r => r.Stats.Diagnostics.SwingRate, "P", "47%");
            AppendAux(sb, "Z-Swing%", results, r => r.Stats.Diagnostics.ZoneSwingRate, "P", "67%");
            AppendAux(sb, "O-Swing%", results, r => r.Stats.Diagnostics.ChaseRate, "P", "28%");
            AppendAux(sb, "Contact%", results, r => r.Stats.Diagnostics.ContactRate, "P", "76%");
            AppendAux(sb, "Z-Contact%", results, r => r.Stats.Diagnostics.ZoneContactRate, "P", "85%");
            AppendAux(sb, "O-Contact%", results, r => r.Stats.Diagnostics.ChaseContactRate, "P", "57%");
            AppendAux(sb, "파울/투구", results, r => r.Stats.Diagnostics.FoulRate, "P", "18%");
            AppendAux(sb, "루킹스트라이크/투구", results, r => r.Stats.Diagnostics.CalledStrikeRate, "P", "16%");
            AppendAux(sb, "땅볼%", results, r => r.Stats.Diagnostics.BattedBallTypeRate(BattedBallType.GroundBall), "P", "43%");
            AppendAux(sb, "라인드라이브%", results, r => r.Stats.Diagnostics.BattedBallTypeRate(BattedBallType.LineDrive), "P", "24%");
            AppendAux(sb, "뜬공%", results, r => r.Stats.Diagnostics.BattedBallTypeRate(BattedBallType.FlyBall), "P", "26%");
            AppendAux(sb, "팝업%", results, r => r.Stats.Diagnostics.BattedBallTypeRate(BattedBallType.PopUp), "P", "7%");
            AppendAux(sb, "땅볼 BABIP", results, r => r.Stats.Diagnostics.BabipByType(BattedBallType.GroundBall), "0.000", ".240");
            AppendAux(sb, "라인드라이브 BABIP", results, r => r.Stats.Diagnostics.BabipByType(BattedBallType.LineDrive), "0.000", ".680");
            AppendAux(sb, "뜬공 BABIP", results, r => r.Stats.Diagnostics.BabipByType(BattedBallType.FlyBall), "0.000", ".130");
            AppendAux(sb, "팝업 BABIP", results, r => r.Stats.Diagnostics.BabipByType(BattedBallType.PopUp), "0.000", ".020");
            AppendAux(sb, "평균 타구속도 km/h", results, r => r.Stats.Diagnostics.AverageExitVelocityKmh, "0.0",
                UnitConversion.MphToKmh(88.5).ToString("0.0", CultureInfo.InvariantCulture) + " (88.5 mph)");
            AppendAux(sb, "평균 발사각", results, r => r.Stats.Diagnostics.AverageLaunchAngleDeg, "0.0", "12.5");
            AppendAux(sb, "HBP%", results, r => r.Stats.League.HitByPitchRate, "P", "1.1%");
            AppendAux(sb, "2루타/타석", results, r => Rate(r.Stats.League.Doubles, r.Stats.League.PlateAppearances), "P", "4.4%");
            AppendAux(sb, "3루타/타석", results, r => Rate(r.Stats.League.Triples, r.Stats.League.PlateAppearances), "P", "0.4%");
            AppendAux(sb, "병살/경기(팀)", results, r => (double)r.Stats.League.GroundedIntoDoublePlays / r.TeamGames, "0.00", "0.70");
            AppendAux(sb, "희생플라이/경기(팀)", results, r => (double)r.Stats.League.SacrificeFlies / r.TeamGames, "0.00", "0.25");
            AppendAux(sb, "실책/경기(팀)", results, r => (double)r.Stats.Diagnostics.Errors / r.TeamGames, "0.00",
                t.ReferenceErrorsPerTeamGame.ToString("0.00", CultureInfo.InvariantCulture));
            AppendAux(sb, "투수 교체/경기(팀)", results, r => (double)r.Stats.PitchingChanges / r.TeamGames, "0.00", "3.2");
            AppendAux(sb, "연장 경기 비율", results, r => Rate(r.ExtraInningGames, r.Games), "P", "8%");
            return sb.ToString();
        }

        private static MetricRow Row(string name, IReadOnlyList<SeasonResult> results, Func<StatsAggregator, double> metric,
            double target, double tolerance, bool isRateStat)
        {
            double[] values = results.Select(r => metric(r.Stats)).ToArray();
            return new MetricRow
            {
                Name = name,
                Mean = values.Average(),
                StandardDeviation = StdDev(values),
                Target = target,
                Tolerance = tolerance,
                IsRateStat = isRateStat,
            };
        }

        private static void AppendAux(StringBuilder sb, string name, IReadOnlyList<SeasonResult> results,
            Func<SeasonResult, double> metric, string format, string reference)
        {
            double[] values = results.Select(metric).ToArray();
            double mean = values.Average();
            double sd = StdDev(values);
            string text = format == "P"
                ? (mean * 100).ToString("0.0", CultureInfo.InvariantCulture) + "% ± " + (sd * 100).ToString("0.0", CultureInfo.InvariantCulture)
                : mean.ToString(format, CultureInfo.InvariantCulture) + " ± " + sd.ToString(format, CultureInfo.InvariantCulture);
            sb.AppendLine(string.Format("  {0,-20}{1,-20}참고 {2}", name, text, reference));
        }

        private static double Rate(int numerator, int denominator)
        {
            return denominator == 0 ? 0 : (double)numerator / denominator;
        }

        private static double StdDev(double[] values)
        {
            if (values.Length < 2)
            {
                return 0;
            }

            double mean = values.Average();
            return Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / (values.Length - 1));
        }

        private static string Fmt(double value, bool rateStat)
        {
            return rateStat
                ? value.ToString(".000", CultureInfo.InvariantCulture)
                : (value * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";
        }

        private static string Signed(double value, bool rateStat)
        {
            string sign = value >= 0 ? "+" : "-";
            double abs = Math.Abs(value);
            return sign + (rateStat
                ? abs.ToString(".000", CultureInfo.InvariantCulture)
                : (abs * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%p");
        }
    }
}
