using BaseballProto.Core;
using BaseballProto.View;

namespace BaseballProto.Feedback
{
    /// <summary>
    /// 손맛 연출 묶음: 진동(IHaptics) + 소리 + 히트스톱 + 화면 흔들림. 설정에서 켜진 것만 쓴다.
    /// </summary>
    public sealed class ImpactFeedback
    {
        private readonly IHaptics _haptics;
        private readonly SoundBank _sounds;
        private readonly PresentationClock _clock;
        private readonly CameraShake _shake;

        public ImpactFeedback(IHaptics haptics, SoundBank sounds, PresentationClock clock, CameraShake shake,
            FeedbackSettings settings)
        {
            _haptics = haptics;
            _sounds = sounds;
            _clock = clock;
            _shake = shake;
            Settings = settings;
        }

        public FeedbackSettings Settings { get; }

        public bool HapticsSupported => _haptics.IsSupported;

        public void Play(ImpactKind kind)
        {
            ImpactSpec spec = ImpactTable.Get(kind);
            if (Settings.Vibration && spec.VibrationMs > 0)
            {
                _haptics.Pulse(spec.VibrationMs, spec.Amplitude);
            }

            if (Settings.Sound)
            {
                _sounds.Play(kind, spec.Volume);
            }

            if (Settings.HitStop && spec.HitStopS > 0f)
            {
                _clock.Freeze(spec.HitStopS);
            }

            if (Settings.Shake)
            {
                _shake.Shake(spec.ShakeM, spec.ShakeS);
            }
        }
    }
}
