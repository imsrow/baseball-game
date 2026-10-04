using UnityEngine;

namespace BaseballProto.Feedback
{
    /// <summary>
    /// 연출 켜기/끄기. 플랫폼 기본값: Android는 진동+소리, 그 외(WebGL·에디터)는 소리+히트스톱+화면 흔들림.
    /// HUD에서 바꿔볼 수 있다.
    /// </summary>
    public sealed class FeedbackSettings
    {
        public bool Vibration { get; set; }

        public bool Sound { get; set; } = true;

        public bool HitStop { get; set; }

        public bool Shake { get; set; }

        public static FeedbackSettings ForCurrentPlatform(IHaptics haptics)
        {
            bool android = haptics.IsSupported && Application.platform == RuntimePlatform.Android;
            return new FeedbackSettings
            {
                Vibration = android,
                Sound = true,
                HitStop = !android,
                Shake = !android,
            };
        }
    }
}
