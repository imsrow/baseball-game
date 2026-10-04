using System;

namespace BaseballSim.Engine.Randomness
{
    /// <summary>
    /// PCG32 (XSH-RR) 난수 생성기.
    /// System.Random은 런타임(Mono/.NET)마다 구현이 달라질 수 있어 직접 구현한다.
    /// 내부 상태(State, Increment) 두 값만으로 완전히 복원 가능하다.
    /// </summary>
    public sealed class Pcg32Random : IRandomSource
    {
        // PCG 기본 LCG 승수 (알고리즘 정의 상수)
        private const ulong Multiplier = 6364136223846793005UL;

        // 2^53: double 가수부 정밀도
        private const double TwoPow53 = 9007199254740992.0;

        // 2^26
        private const double TwoPow26 = 67108864.0;

        private ulong _state;
        private ulong _increment;

        /// <summary>
        /// 시드와 스트림 번호로 생성. pcg32_srandom_r과 동일한 초기화 절차를 따른다.
        /// </summary>
        public Pcg32Random(ulong seed, ulong stream = 0)
        {
            _state = 0;
            _increment = (stream << 1) | 1UL;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        private Pcg32Random(ulong state, ulong increment, bool _)
        {
            _state = state;
            _increment = increment | 1UL;
        }

        /// <summary>현재 내부 상태 (저장용)</summary>
        public ulong State => _state;

        /// <summary>스트림 증가값 (저장용)</summary>
        public ulong Increment => _increment;

        /// <summary>저장된 상태에서 복원</summary>
        public static Pcg32Random FromState(ulong state, ulong increment)
        {
            return new Pcg32Random(state, increment, true);
        }

        /// <summary>같은 상태를 가진 복제본</summary>
        public Pcg32Random Clone()
        {
            return FromState(_state, _increment);
        }

        public uint NextUInt()
        {
            ulong oldState = _state;
            _state = unchecked(oldState * Multiplier + _increment);
            uint xorShifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            int rotation = (int)(oldState >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((32 - rotation) & 31));
        }

        public double NextDouble()
        {
            // 상위 27비트 + 26비트로 53비트 정밀도 실수 생성
            uint a = NextUInt() >> 5;
            uint b = NextUInt() >> 6;
            return (a * TwoPow26 + b) / TwoPow53;
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }

            // 나머지 편향 제거를 위한 거부 샘플링
            uint bound = (uint)maxExclusive;
            uint threshold = (uint)((0x100000000UL - bound) % bound);
            while (true)
            {
                uint value = NextUInt();
                if (value >= threshold)
                {
                    return (int)(value % bound);
                }
            }
        }

        public double NextGaussian()
        {
            // Box-Muller. 상태 직렬화를 단순하게 하려고 두 번째 값은 캐시하지 않는다.
            double u1 = 1.0 - NextDouble();
            double u2 = NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }
    }
}
