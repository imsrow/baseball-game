using System;

namespace BaseballProto.Core
{
    /// <summary>
    /// 연출용 시계. 히트스톱 동안 멈춘다. 판정에는 쓰지 않는다 (판정은 ProtoClock).
    /// </summary>
    public sealed class PresentationClock
    {
        private double _frozenUntil;

        /// <summary>이번 프레임 연출 경과 시간 (히트스톱 중 0)</summary>
        public float DeltaTime { get; private set; }

        public bool IsFrozen => ProtoClock.Now < _frozenUntil;

        public void Tick(float unscaledDeltaTime)
        {
            DeltaTime = IsFrozen ? 0f : unscaledDeltaTime;
        }

        public void Freeze(float seconds)
        {
            _frozenUntil = Math.Max(_frozenUntil, ProtoClock.Now + seconds);
        }
    }
}
