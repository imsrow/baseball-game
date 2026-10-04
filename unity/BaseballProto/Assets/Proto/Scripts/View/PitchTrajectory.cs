using BaseballProto.Core;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using UnityEngine;

namespace BaseballProto.View
{
    /// <summary>
    /// 투구 궤적 (연출). 도달 시각·도달 위치는 판정 기준이므로 정확히 지킨다.
    /// 휘는 모양은 (u − u³) 곡선으로, 처음엔 반대쪽에 있다가 끝에서 꺾여 들어오게 보인다.
    /// </summary>
    public sealed class PitchTrajectory
    {
        private const float KmhToMs = 1f / 3.6f;

        // (u − u³)의 최댓값(≈0.385)을 1로 맞추는 배율
        private const float BreakShapeNormalizer = 2.6f;

        private readonly Vector3 _start;
        private readonly Vector3 _end;
        private readonly Vector3 _breakOffset;
        private readonly float _arcHeight;
        private readonly float _speed;
        private readonly float _mittDepth;

        public PitchTrajectory(PlateLocation actual, double velocityKmh, PitchType type, Hand throws, double releaseTime,
            ProtoTuning tuning)
        {
            // 포수 시점: 우투수의 팔 쪽은 3루 쪽(−x)
            float armSign = throws == Hand.Right ? -1f : 1f;
            _start = new Vector3(armSign * tuning.ReleaseSideM, tuning.ReleaseHeightM, tuning.ReleaseDistanceM);
            _end = new Vector3((float)actual.X, (float)actual.Z, 0f);
            Vector2 brk = PitchVisualTable.Break(type) * tuning.BreakScale;
            _breakOffset = new Vector3(armSign * brk.x, brk.y, 0f);
            _arcHeight = tuning.ArcHeightM;
            _mittDepth = tuning.MittDepthM;

            float realFlight = tuning.ReleaseDistanceM / ((float)velocityKmh * KmhToMs) * tuning.FlightDragFactor;
            FlightTime = realFlight * tuning.FlightTimeScale;
            _speed = tuning.ReleaseDistanceM / FlightTime;
            ReleaseTime = releaseTime;
            ArrivalTime = releaseTime + FlightTime;
        }

        public double ReleaseTime { get; }

        /// <summary>홈플레이트(z = 0) 도달 시각. 타이밍 판정 기준</summary>
        public double ArrivalTime { get; }

        public float FlightTime { get; }

        public Vector3 PlatePoint => _end;

        public Vector3 PositionAt(double time)
        {
            float u = (float)((time - ReleaseTime) / FlightTime);
            if (u <= 0f)
            {
                return _start;
            }

            if (u <= 1f)
            {
                Vector3 line = Vector3.Lerp(_start, _end, u);
                float arc = 4f * u * (1f - u) * _arcHeight;
                float bend = (u - u * u * u) * BreakShapeNormalizer;
                return line + Vector3.up * arc - _breakOffset * bend;
            }

            // 홈플레이트 통과 후 포수 미트까지
            float past = (float)(time - ArrivalTime) * _speed;
            return _end + Vector3.back * Mathf.Min(past, _mittDepth);
        }
    }
}
