namespace BaseballSim.Engine.Randomness
{
    /// <summary>
    /// SplitMix64 기반 시드 파생. 시즌 시드 + 경기 번호처럼 여러 값에서 독립 시드를 만든다.
    /// </summary>
    public static class SeedMixer
    {
        // SplitMix64 알고리즘 정의 상수
        private const ulong Golden = 0x9E3779B97F4A7C15UL;
        private const ulong MixA = 0xBF58476D1CE4E5B9UL;
        private const ulong MixB = 0x94D049BB133111EBUL;

        /// <summary>64비트 값 하나를 섞는다</summary>
        public static ulong Mix(ulong value)
        {
            unchecked
            {
                ulong z = value + Golden;
                z = (z ^ (z >> 30)) * MixA;
                z = (z ^ (z >> 27)) * MixB;
                return z ^ (z >> 31);
            }
        }

        /// <summary>기준 시드와 인덱스로 파생 시드 생성</summary>
        public static ulong Derive(ulong baseSeed, ulong index)
        {
            unchecked
            {
                return Mix(Mix(baseSeed) ^ (index * Golden));
            }
        }
    }
}
