using UnityEngine;

namespace BaseballProto.Core
{
    /// <summary>
    /// 연출 이동 한 구간: T0~T1 동안 From → To. 곡선(2차 베지어 제어점), 포물선 높이, 바운드를 더할 수 있다.
    /// </summary>
    public struct MotionSegment
    {
        // Accelerate: 이 비율까지 등가속, 그 뒤 등속
        private const float AccelerateFraction = 0.3f;

        public float T0;
        public float T1;
        public Vector3 From;
        public Vector3 To;
        public MotionEase Ease;

        /// <summary>곡선 경로 제어점 (Curved일 때만)</summary>
        public bool Curved;

        public Vector3 Control;

        /// <summary>포물선 꼭짓점 높이 (직선 위로 더함)</summary>
        public float Arc;

        /// <summary>바운드 횟수와 첫 바운드 높이 (점점 낮아짐)</summary>
        public int Hops;

        public float HopHeight;

        public Vector3 Evaluate(float t)
        {
            float duration = T1 - T0;
            float tau = duration <= 0f ? 1f : Mathf.Clamp01((t - T0) / duration);
            float u = Shape(tau);
            Vector3 p = Curved
                ? (1f - u) * (1f - u) * From + 2f * (1f - u) * u * Control + u * u * To
                : Vector3.LerpUnclamped(From, To, u);
            if (Arc != 0f)
            {
                // 날아가는 공은 시간 기준 포물선 (수평은 등속)
                p.y += 4f * Arc * tau * (1f - tau);
            }

            if (Hops > 0 && HopHeight > 0f)
            {
                p.y += HopHeight * (1f - tau) * Mathf.Abs(Mathf.Sin(Mathf.PI * Hops * tau));
            }

            return p;
        }

        /// <summary>이동 거리 (곡선은 근사)</summary>
        public float Length()
        {
            return Curved ? (Vector3.Distance(From, Control) + Vector3.Distance(Control, To) + Vector3.Distance(From, To)) * 0.5f
                : Vector3.Distance(From, To);
        }

        private float Shape(float tau)
        {
            switch (Ease)
            {
                case MotionEase.EaseOut:
                    return 1f - (1f - tau) * (1f - tau);
                case MotionEase.Smooth:
                    return tau * tau * (3f - 2f * tau);
                case MotionEase.Accelerate:
                    const float a = AccelerateFraction;
                    float total = 1f - a * 0.5f;
                    return tau < a ? tau * tau / (2f * a) / total : (tau - a * 0.5f) / total;
                default:
                    return tau;
            }
        }
    }
}
