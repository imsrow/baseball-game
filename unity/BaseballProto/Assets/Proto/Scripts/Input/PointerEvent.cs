using UnityEngine;

namespace BaseballProto.Input
{
    /// <summary>
    /// 타임스탬프가 붙은 입력 이벤트. Time은 ProtoClock과 같은 시간축의 실제 입력 발생 시각이다.
    /// </summary>
    public readonly struct PointerEvent
    {
        public PointerEvent(int fingerId, PointerPhase phase, Vector2 screenPosition, Vector2 delta, double time)
        {
            FingerId = fingerId;
            Phase = phase;
            ScreenPosition = screenPosition;
            Delta = delta;
            Time = time;
        }

        /// <summary>손가락 번호 (키보드는 −1)</summary>
        public int FingerId { get; }

        public PointerPhase Phase { get; }

        /// <summary>화면 좌표 (픽셀, 좌하단 원점)</summary>
        public Vector2 ScreenPosition { get; }

        /// <summary>직전 이벤트 대비 이동량 (픽셀)</summary>
        public Vector2 Delta { get; }

        public double Time { get; }

        public static PointerEvent Key(double time)
        {
            return new PointerEvent(-1, PointerPhase.SwingKey, Vector2.zero, Vector2.zero, time);
        }
    }
}
