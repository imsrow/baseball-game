using System.Collections.Generic;
using UnityEngine;

namespace BaseballProto.Core
{
    /// <summary>
    /// 시간 순서로 이어진 이동 구간들. 구간 사이 빈 시간에는 직전 위치에 머문다.
    /// </summary>
    public sealed class MotionTrack
    {
        private readonly List<MotionSegment> _segments = new List<MotionSegment>();

        public MotionTrack(Vector3 start)
        {
            Start = start;
        }

        public Vector3 Start { get; }

        public Vector3 EndPosition => _segments.Count == 0 ? Start : _segments[_segments.Count - 1].To;

        public float EndTime => _segments.Count == 0 ? 0f : _segments[_segments.Count - 1].T1;

        public bool IsEmpty => _segments.Count == 0;

        /// <summary>현재 끝 위치에서 to까지 이동하는 구간을 붙인다 (t0가 이전 끝보다 이르면 이전 끝에 맞춘다)</summary>
        public MotionSegment MoveTo(Vector3 to, float t0, float t1, MotionEase ease = MotionEase.Linear)
        {
            t0 = Mathf.Max(t0, EndTime);
            var segment = new MotionSegment
            {
                T0 = t0,
                T1 = Mathf.Max(t0, t1),
                From = EndPosition,
                To = to,
                Ease = ease,
            };
            return segment;
        }

        public void Add(MotionSegment segment)
        {
            segment.T0 = Mathf.Max(segment.T0, EndTime);
            segment.T1 = Mathf.Max(segment.T0, segment.T1);
            _segments.Add(segment);
        }

        public Vector3 Evaluate(float t)
        {
            Vector3 p = Start;
            for (int i = 0; i < _segments.Count; i++)
            {
                MotionSegment s = _segments[i];
                if (t < s.T0)
                {
                    return s.From;
                }

                if (t <= s.T1)
                {
                    return s.Evaluate(t);
                }

                p = s.To;
            }

            return p;
        }
    }
}
