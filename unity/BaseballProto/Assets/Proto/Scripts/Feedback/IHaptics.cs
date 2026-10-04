namespace BaseballProto.Feedback
{
    /// <summary>
    /// 진동. 플랫폼별 구현을 분리한다 (Android: 실제 진동, 그 외: 없음)
    /// </summary>
    public interface IHaptics
    {
        bool IsSupported { get; }

        /// <summary>진동 한 번</summary>
        /// <param name="milliseconds">길이</param>
        /// <param name="amplitude">세기 1~255 (기기가 세기 조절을 지원하지 않으면 무시)</param>
        void Pulse(int milliseconds, int amplitude);
    }
}
