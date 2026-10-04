namespace BaseballProto.Feedback
{
    /// <summary>
    /// 현재 플랫폼에 맞는 진동 구현 선택 (플랫폼 차이는 여기서만 갈린다)
    /// </summary>
    public static class PlatformHaptics
    {
        public static IHaptics Create()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidHaptics();
#else
            return new NullHaptics();
#endif
        }
    }
}
