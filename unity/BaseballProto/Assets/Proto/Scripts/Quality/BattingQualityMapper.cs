using System;
using BaseballProto.Core;
using BaseballProto.Input;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Pitching;

namespace BaseballProto.Quality
{
    /// <summary>
    /// 스윙 입력 → 엔진 입력값.
    /// - TimingQuality(컨택·정타): 타이밍 점수와 커서 점수의 가중 평균을 −1~+1로.
    ///   곱하면 한쪽이 0일 때 다른 쪽이 완벽해도 −1이 되므로 평균으로 합친다
    /// - TimingDirection(타구 방향): 타이밍 오차의 부호와 크기 (이르면 −, 늦으면 +)
    /// - CursorVerticalOffset(발사각): 커서가 공보다 위면 +, 아래면 −
    /// </summary>
    public static class BattingQualityMapper
    {
        private const double MsPerSecond = 1000.0;

        public static BattingJudgement Judge(SwingInput swing, double arrivalTime, PlateLocation ball, ProtoTuning tuning)
        {
            double rawErrorMs = (swing.Time - arrivalTime) * MsPerSecond;
            double errorMs = rawErrorMs - tuning.DisplayLatencyMs;
            double timingScore = QualityMath.Score(Math.Abs(errorMs), tuning.PerfectTimingMs, tuning.ZeroTimingMs);

            double dx = swing.CursorPlate.x - ball.X;
            double dz = swing.CursorPlate.y - ball.Z;
            double ratio = Math.Sqrt(dx * dx + dz * dz) / tuning.CursorRadiusM;
            double cursorScore = QualityMath.Score(ratio, tuning.CursorPerfectRatio, tuning.CursorZeroRatio);

            double weight = Math.Max(0.0, Math.Min(1.0, tuning.TimingWeight));
            double combined = weight * timingScore + (1.0 - weight) * cursorScore;

            var action = BatterAction.Swing(QualityMath.ToQuality(combined));
            action.TimingDirection = QualityMath.ClampUnit(errorMs / tuning.DirectionFullMs);
            action.CursorVerticalOffset = QualityMath.ClampUnit(dz / tuning.CursorRadiusM);

            return new BattingJudgement
            {
                TimingErrorMs = errorMs,
                RawTimingErrorMs = rawErrorMs,
                TimingScore = timingScore,
                CursorDx = dx,
                CursorDz = dz,
                CursorDistanceRatio = ratio,
                CursorScore = cursorScore,
                CombinedScore = combined,
                TimingWeight = weight,
                Action = action,
            };
        }
    }
}
