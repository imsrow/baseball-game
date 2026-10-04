namespace BaseballProto.Feedback
{
    /// <summary>
    /// 연출 종류 하나의 세기 묶음
    /// </summary>
    public sealed class ImpactSpec
    {
        public ImpactSpec(int vibrationMs, int amplitude, float hitStopS, float shakeM, float shakeS, float volume)
        {
            VibrationMs = vibrationMs;
            Amplitude = amplitude;
            HitStopS = hitStopS;
            ShakeM = shakeM;
            ShakeS = shakeS;
            Volume = volume;
        }

        public int VibrationMs { get; }

        public int Amplitude { get; }

        public float HitStopS { get; }

        public float ShakeM { get; }

        public float ShakeS { get; }

        public float Volume { get; }
    }
}
