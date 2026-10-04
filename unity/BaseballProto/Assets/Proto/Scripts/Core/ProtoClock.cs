using UnityEngine.InputSystem.LowLevel;

namespace BaseballProto.Core
{
    /// <summary>
    /// 판정용 시계. 입력 이벤트 타임스탬프와 같은 시간축(Time.realtimeSinceStartup 기준, 초)을 쓴다.
    /// 프레임 시각이 아니라 이 시계로 공 도달 시각과 입력 시각을 비교한다.
    /// </summary>
    public static class ProtoClock
    {
        public static double Now => InputState.currentTime;
    }
}
