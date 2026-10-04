using System.Collections.Generic;
using BaseballProto.Core;
using BaseballProto.View;
using BaseballSim.Engine.Pitching;
using UnityEngine;

namespace BaseballProto.Input
{
    /// <summary>
    /// 투구 입력: 구종 버튼 → 목표 지점 터치 → 게이지 탭.
    /// 게이지 값은 시간의 함수라서 탭 이벤트 타임스탬프로 정확한 정지 위치를 역산한다.
    /// </summary>
    public sealed class PitchingInput
    {
        private readonly ProtoTuning _tuning;
        private readonly PlateMapper _mapper;
        private readonly List<PitchType> _repertoire = new List<PitchType>();

        public PitchingInput(ProtoTuning tuning, PlateMapper mapper)
        {
            _tuning = tuning;
            _mapper = mapper;
        }

        public PitchingStage Stage { get; private set; }

        public IReadOnlyList<PitchType> Repertoire => _repertoire;

        public PitchType SelectedType { get; private set; }

        public Vector2 Target { get; private set; }

        public double GaugeStartTime { get; private set; }

        /// <summary>게이지 정지 시각 (시간 초과면 null)</summary>
        public double? GaugeStopTime { get; private set; }

        public void Begin(IEnumerable<PitchType> repertoire)
        {
            _repertoire.Clear();
            _repertoire.AddRange(repertoire);
            if (!_repertoire.Contains(SelectedType) && _repertoire.Count > 0)
            {
                SelectedType = _repertoire[0];
            }

            GaugeStopTime = null;
            Stage = PitchingStage.Aim;
        }

        public void End()
        {
            Stage = PitchingStage.Inactive;
        }

        public void SelectType(PitchType type)
        {
            if (Stage == PitchingStage.Aim && _repertoire.Contains(type))
            {
                SelectedType = type;
            }
        }

        /// <summary>게이지 값 (0~1 삼각파). 시작 시 0에서 올라간다</summary>
        public float GaugeValueAt(double time)
        {
            double phase = (time - GaugeStartTime) / _tuning.GaugePeriodS;
            phase -= System.Math.Floor(phase);
            return (float)(phase < 0.5 ? phase * 2.0 : 2.0 - phase * 2.0);
        }

        /// <summary>시간 초과 확인. 초과면 Done(정지 시각 없음)</summary>
        public void CheckTimeout(double now)
        {
            if (Stage == PitchingStage.Gauge && now - GaugeStartTime > _tuning.GaugeTimeoutS)
            {
                GaugeStopTime = null;
                Stage = PitchingStage.Done;
            }
        }

        public void Handle(PointerEvent e)
        {
            bool tap = e.Phase == PointerPhase.Down || e.Phase == PointerPhase.SwingKey;
            if (!tap)
            {
                return;
            }

            if (Stage == PitchingStage.Aim && e.Phase == PointerPhase.Down
                && _mapper.TryScreenToPlate(e.ScreenPosition, out Vector2 plate))
            {
                Target = new Vector2(
                    Mathf.Clamp(plate.x, -_tuning.TargetLimitXM, _tuning.TargetLimitXM),
                    Mathf.Clamp(plate.y, _tuning.TargetMinZM, _tuning.TargetMaxZM));
                GaugeStartTime = e.Time;
                Stage = PitchingStage.Gauge;
                return;
            }

            if (Stage == PitchingStage.Gauge && e.Time > GaugeStartTime)
            {
                GaugeStopTime = e.Time;
                Stage = PitchingStage.Done;
            }
        }
    }
}
