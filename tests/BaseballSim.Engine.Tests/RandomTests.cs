using System;
using BaseballSim.Engine.Randomness;
using Xunit;

namespace BaseballSim.Engine.Tests
{
    public class RandomTests
    {
        [Fact]
        public void Pcg32_참조구현과_같은_출력()
        {
            // pcg32-demo (initstate 42, initseq 54) 첫 출력
            var rng = new Pcg32Random(42, 54);
            uint[] expected = { 0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e };
            foreach (uint value in expected)
            {
                Assert.Equal(value, rng.NextUInt());
            }
        }

        [Fact]
        public void 같은_시드면_같은_수열()
        {
            var a = new Pcg32Random(123);
            var b = new Pcg32Random(123);
            for (int i = 0; i < 1000; i++)
            {
                Assert.Equal(a.NextUInt(), b.NextUInt());
            }
        }

        [Fact]
        public void 상태에서_복원하면_이어서_같은_수열()
        {
            var a = new Pcg32Random(7);
            for (int i = 0; i < 37; i++)
            {
                a.NextDouble();
            }

            Pcg32Random restored = Pcg32Random.FromState(a.State, a.Increment);
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(a.NextUInt(), restored.NextUInt());
            }
        }

        [Fact]
        public void NextDouble_범위와_평균()
        {
            var rng = new Pcg32Random(1);
            double sum = 0;
            const int n = 100000;
            for (int i = 0; i < n; i++)
            {
                double v = rng.NextDouble();
                Assert.InRange(v, 0.0, 0.9999999999);
                sum += v;
            }

            Assert.Equal(0.5, sum / n, 2);
        }

        [Fact]
        public void NextInt_범위와_고른분포()
        {
            var rng = new Pcg32Random(2);
            var counts = new int[7];
            const int n = 70000;
            for (int i = 0; i < n; i++)
            {
                counts[rng.NextInt(7)]++;
            }

            foreach (int c in counts)
            {
                Assert.InRange(c, 9500, 10500);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(0));
        }

        [Fact]
        public void NextGaussian_평균0_표준편차1()
        {
            var rng = new Pcg32Random(3);
            double sum = 0;
            double sumSq = 0;
            const int n = 100000;
            for (int i = 0; i < n; i++)
            {
                double v = rng.NextGaussian();
                sum += v;
                sumSq += v * v;
            }

            double mean = sum / n;
            Assert.Equal(0.0, mean, 1);
            Assert.Equal(1.0, Math.Sqrt(sumSq / n - mean * mean), 1);
        }

        [Fact]
        public void 파생시드는_서로_다르고_결정적()
        {
            Assert.Equal(SeedMixer.Derive(5, 1), SeedMixer.Derive(5, 1));
            Assert.NotEqual(SeedMixer.Derive(5, 1), SeedMixer.Derive(5, 2));
            Assert.NotEqual(SeedMixer.Derive(5, 1), SeedMixer.Derive(6, 1));
        }

        [Fact]
        public void WeightedIndex_가중치를_따른다()
        {
            var rng = new Pcg32Random(4);
            var counts = new int[3];
            for (int i = 0; i < 30000; i++)
            {
                counts[rng.WeightedIndex(new[] { 1.0, 0.0, 3.0 })]++;
            }

            Assert.Equal(0, counts[1]);
            Assert.InRange(counts[2] / (double)counts[0], 2.7, 3.3);
        }
    }
}
