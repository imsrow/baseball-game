using UnityEngine;

namespace BaseballProto.Input
{
    /// <summary>
    /// 스윙 입력: 입력 시각과 그 순간의 커서 위치 (홈플레이트 평면, x: 1루 쪽 +, y: 높이, m)
    /// </summary>
    public readonly struct SwingInput
    {
        public SwingInput(double time, Vector2 cursorPlate)
        {
            Time = time;
            CursorPlate = cursorPlate;
        }

        public double Time { get; }

        public Vector2 CursorPlate { get; }
    }
}
