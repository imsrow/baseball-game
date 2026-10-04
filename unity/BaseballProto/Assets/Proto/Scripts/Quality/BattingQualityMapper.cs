using System;
using BaseballProto.Core;
using BaseballProto.Input;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Pitching;

namespace BaseballProto.Quality
{
    /// <summary>
    /// 스윙 입력 → 엔진 입력값.
    /// - TimingQuality(컨택·정타): 타이밍 점수 × 커서 점수를 −1~+1로
    /// - TimingDirection(타구 방향): 타이밍 오차의 부호와 크기 (이르면 −, 늦으면 +)
    /// - CursorVerticalOffset(발사각): 커서가 공보다 위면 +, 아래면 −
    /// </summary>
    public static class BattingQualityMapper
    {
        private const double MsPerSecond = 1000.0;

        public static BattingJudgement Judge(SwingInput swing, double arrivalTime, PlateLocation ball, ProtoTuning tuning)
        {
            double errorMs = (swing.Time - arrivalTime) * MsPerSecond - tuning.DisplayLatencyMs;
            double timingScore = QualityMath.Score(Math.Abs(errorMs), tuning.PerfectTimingMs, tuning.ZeroTimingMs);

            double dx = swing.CursorPlate.x - ball.X;
            double dz = swing.CursorPlate.y - ball.Z;
            double ratio = Math.Sqrt(dx * dx + dz * dz) / tuning.CursorRadiusM;
            double cursorScore = QualityMath.Score(ratio, tuning.CursorPerfectRatio, tuning.CursorZeroRatio);

            var action = BatterAction.Swing(QualityMath.ToQuality(timingScore * cursorScore));
            action.TimingDirection = QualityMath.ClampUnit(errorMs / tuning.DirectionFullMs);
            action.CursorVerticalOffset = QualityMath.ClampUnit(dz / tuning.CursorRadiusM);

            return new BattingJudgement
            {
                TimingErrorMs = errorMs,
                TimingScore = timingScore,
                CursorDx = dx,
                CursorDz = dz,
                CursorDistanceRatio = ratio,
                CursorScore = cursorScore,
                Action = action,
            };
        }
    }
}
