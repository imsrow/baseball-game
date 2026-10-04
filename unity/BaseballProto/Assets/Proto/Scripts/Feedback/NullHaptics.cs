namespace BaseballProto.Feedback
{
    /// <summary>
    /// 진동 없음 (WebGL·에디터). 대신 소리·히트스톱·화면 흔들림으로 손맛을 낸다.
    /// </summary>
    public sealed class NullHaptics : IHaptics
    {
        public bool IsSupported => false;

        public void Pulse(int milliseconds, int amplitude)
        {
        }
    }
}
