using System;
using UnityEngine;

namespace BaseballProto.Feedback
{
    /// <summary>
    /// 코드로 만든 효과음 (에셋 없음). 소리 모양만 잡은 프로토타입용.
    /// </summary>
    public sealed class SoundBank
    {
        private const int SampleRate = 44100;

        private readonly AudioSource _source;
        private readonly AudioClip _crack;
        private readonly AudioClip _thud;
        private readonly AudioClip _whoosh;
        private readonly AudioClip _pop;

        public SoundBank(GameObject host)
        {
            _source = host.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;

            var random = new System.Random(7);
            _crack = Make("crack", 0.09f, t => Noise(random) * Decay(t, 0.012f) + Tone(t, 2300f) * Decay(t, 0.02f) * 0.5f);
            _thud = Make("thud", 0.12f, t => (Noise(random) * 0.4f + Tone(t, 220f)) * Decay(t, 0.03f));
            _whoosh = Make("whoosh", 0.22f, t => Noise(random) * Window(t, 0.22f) * 0.35f);
            _pop = Make("pop", 0.08f, t => (Tone(t, 160f) + Noise(random) * 0.3f) * Decay(t, 0.015f));
        }

        public void Play(ImpactKind kind, float volume)
        {
            AudioClip clip;
            switch (kind)
            {
                case ImpactKind.SolidContact:
                case ImpactKind.HomeRun:
                    clip = _crack;
                    break;
                case ImpactKind.WeakContact:
                case ImpactKind.Foul:
                    clip = _thud;
                    break;
                case ImpactKind.Whiff:
                    clip = _whoosh;
                    break;
                default:
                    clip = _pop;
                    break;
            }

            _source.PlayOneShot(clip, volume);
        }

        private static AudioClip Make(string name, float seconds, Func<float, float> wave)
        {
            int count = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[count];
            for (int i = 0; i < count; i++)
            {
                data[i] = Mathf.Clamp(wave(i / (float)SampleRate), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Noise(System.Random random)
        {
            return (float)(random.NextDouble() * 2.0 - 1.0);
        }

        private static float Tone(float t, float frequency)
        {
            return Mathf.Sin(2f * Mathf.PI * frequency * t);
        }

        private static float Decay(float t, float timeConstant)
        {
            return Mathf.Exp(-t / timeConstant);
        }

        private static float Window(float t, float length)
        {
            return Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / length));
        }
    }
}
