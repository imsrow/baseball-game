using System;
using BaseballProto.Core;
using BaseballProto.Input;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Pitching;

namespace BaseballProto.Quality
{
    /// <summary>
    /// 게이지 정확도 → ReleaseQuality(−1~+1). 정지 시각의 게이지 값을 타임스탬프로 계산한다.
    /// </summary>
    public static class ReleaseQualityMapper
    {
        public static PitchJudgement Judge(PitchingInput input, ProtoTuning tuning)
        {
            var target = new PlateLocation(input.Target.x, input.Target.y);
            if (!input.GaugeStopTime.HasValue)
            {
                return new PitchJudgement
                {
                    GaugeValue = null,
                    GaugeError = 1.0,
                    ReleaseScore = 0.0,
                    Call = new PitchCall(input.SelectedType, target, -1.0),
                };
            }

            double value = input.GaugeValueAt(input.GaugeStopTime.Value);
            double error = Math.Abs(value - tuning.GaugeSweetCenter);
            double score = QualityMath.Score(error, tuning.GaugePerfectBand, tuning.GaugeZeroBand);
            return new PitchJudgement
            {
                GaugeValue = value,
                GaugeError = error,
                ReleaseScore = score,
                Call = new PitchCall(input.SelectedType, target, QualityMath.ToQuality(score)),
            };
        }
    }
}
