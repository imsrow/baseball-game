using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace BaseballProto.UI
{
    /// <summary>
    /// 화면 안전 영역 (노치·홈 인디케이터 제외, 화면 픽셀, 좌하단 원점).
    /// Android·에디터는 Screen.safeArea, WebGL은 CSS env(safe-area-inset-*)를 jslib로 읽는다.
    /// 값은 화면 크기가 바뀌거나 일정 시간마다만 다시 읽는다.
    /// </summary>
    public static class SafeArea
    {
        private const float RefreshIntervalS = 1f;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void BaseballProtoSafeAreaInsets(float[] insets);

        private static readonly float[] Insets = new float[4];
#endif

        private static Rect s_cached;
        private static int s_width;
        private static int s_height;
        private static float s_nextRefresh;

        public static Rect ScreenRect
        {
            get
            {
                int w = UnityEngine.Screen.width;
                int h = UnityEngine.Screen.height;
                if (w != s_width || h != s_height || Time.unscaledTime >= s_nextRefresh)
                {
                    s_width = w;
                    s_height = h;
                    s_nextRefresh = Time.unscaledTime + RefreshIntervalS;
                    s_cached = Read(w, h);
                }

                return s_cached;
            }
        }

        private static Rect Read(int w, int h)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            BaseballProtoSafeAreaInsets(Insets);
            float top = Insets[0], right = Insets[1], bottom = Insets[2], left = Insets[3];
            return new Rect(left, bottom, Mathf.Max(1f, w - left - right), Mathf.Max(1f, h - top - bottom));
#else
            Rect safe = UnityEngine.Screen.safeArea;
            return safe.width > 0f && safe.height > 0f ? safe : new Rect(0f, 0f, w, h);
#endif
        }
    }
}
