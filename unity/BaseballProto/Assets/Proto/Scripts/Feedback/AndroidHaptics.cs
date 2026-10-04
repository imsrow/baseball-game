#if UNITY_ANDROID
using System;
using UnityEngine;

namespace BaseballProto.Feedback
{
    /// <summary>
    /// Android 진동. API 26 이상은 VibrationEffect로 세기 조절, 그 미만은 길이만.
    /// Handheld.Vibrate 참조가 있어야 Unity가 VIBRATE 권한을 매니페스트에 넣는다 (실패 시 대체 경로로도 사용).
    /// </summary>
    public sealed class AndroidHaptics : IHaptics
    {
        private const int AmplitudeApiLevel = 26;
        private const int MaxAmplitude = 255;

        private readonly AndroidJavaObject _vibrator;
        private readonly AndroidJavaClass _effectClass;
        private readonly int _sdk;

        public AndroidHaptics()
        {
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    _sdk = version.GetStatic<int>("SDK_INT");
                }

                if (_sdk >= AmplitudeApiLevel)
                {
                    _effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("진동 초기화 실패, Handheld.Vibrate로 대체: " + e.Message);
                _vibrator = null;
            }
        }

        public bool IsSupported => true;

        public void Pulse(int milliseconds, int amplitude)
        {
            if (_vibrator == null)
            {
                Handheld.Vibrate();
                return;
            }

            try
            {
                if (_effectClass != null)
                {
                    int clamped = Mathf.Clamp(amplitude, 1, MaxAmplitude);
                    using (AndroidJavaObject effect = _effectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)milliseconds, clamped))
                    {
                        _vibrator.Call("vibrate", effect);
                    }
                }
                else
                {
                    _vibrator.Call("vibrate", (long)milliseconds);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("진동 실패: " + e.Message);
            }
        }
    }
}
#endif
