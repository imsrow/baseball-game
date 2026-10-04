namespace BaseballSim.Engine.Randomness
{
    /// <summary>
    /// 엔진이 사용하는 난수원. 모든 판정기는 이 인터페이스를 주입받아 사용해 재현성을 보장한다.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>32비트 균등 정수</summary>
        uint NextUInt();

        /// <summary>[0, 1) 균등 실수</summary>
        double NextDouble();

        /// <summary>[0, maxExclusive) 균등 정수 (편향 없음)</summary>
        int NextInt(int maxExclusive);

        /// <summary>표준정규분포 N(0, 1)</summary>
        double NextGaussian();
    }
}
