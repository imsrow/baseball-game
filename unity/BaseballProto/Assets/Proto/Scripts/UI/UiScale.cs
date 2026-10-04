using UnityEngine;

namespace BaseballProto.UI
{
    /// <summary>
    /// HUD 좌표계: 세로 720 × 1280 기준 단위. 화면 크기에 맞춰 배율을 정한다.
    /// IMGUI는 좌상단 원점, 입력(화면 좌표)은 좌하단 원점이라 변환한다.
    /// </summary>
    public static class UiScale
    {
        private const float ReferenceWidth = 720f;
        private const float ReferenceHeight = 1280f;

        public static float Factor => Mathf.Max(0.1f, Mathf.Min(Screen.width / ReferenceWidth, Screen.height / ReferenceHeight));

        public static float Width => Screen.width / Factor;

        public static float Height => Screen.height / Factor;

        /// <summary>입력 화면 좌표 → HUD 좌표</summary>
        public static Vector2 FromScreen(Vector2 screen)
        {
            float s = Factor;
            return new Vector2(screen.x / s, (Screen.height - screen.y) / s);
        }

        /// <summary>HUD 사각형 → IMGUI 픽셀 사각형</summary>
        public static Rect ToGui(Rect rect)
        {
            float s = Factor;
            return new Rect(rect.x * s, rect.y * s, rect.width * s, rect.height * s);
        }

        public static int Font(float size)
        {
            return Mathf.Max(8, Mathf.RoundToInt(size * Factor));
        }
    }
}
