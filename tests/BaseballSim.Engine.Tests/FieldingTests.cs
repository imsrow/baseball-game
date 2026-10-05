using BaseballSim.Engine.Batting;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Fielding;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Randomness;
using BaseballSim.Engine.Ratings;
using BaseballSim.Engine.Units;
using Xunit;

namespace BaseballSim.Engine.Tests
{
    public class FieldingTests
    {
        private readonly LeagueConfig _config = LeagueConfig.CreateDefault();

        private BallFlight Flight()
        {
            return new BallFlight(_config.Physics, new FieldGeometry(_config.Field), _config.Environment);
        }

        [Fact]
        public void 비행거리_현실범위()
        {
            // 103 mph, 28도 → 약 400~420 ft
            FlightResult f = Flight().Simulate(UnitConversion.MphToKmh(103), 28, 0);
            Assert.InRange(UnitConversion.MetersToFeet(f.DistanceM), 395, 425);
            Assert.InRange(f.CatchTimeS, 4.5, 5.5);
        }

        [Fact]
        public void 타구속도가_빠를수록_멀리()
        {
            Assert.True(Flight().Simulate(170, 28, 0).DistanceM > Flight().Simulate(150, 28, 0).DistanceM);
        }

        [Fact]
        public void 홈런과_펜스()
        {
            Assert.True(Flight().Simulate(UnitConversion.MphToKmh(110), 28, 0).IsHomeRun);
            FlightResult wall = Flight().Simulate(UnitConversion.MphToKmh(100), 17, -44);
            Assert.False(Flight().Simulate(UnitConversion.MphToKmh(85), 30, 0).IsHomeRun);
            Assert.True(wall.IsHomeRun || wall.HitWall || wall.DistanceM < 99.1);
        }

        [Fact]
        public void 라인드라이브는_낮게_깔린다()
        {
            // 100 mph, 15도 라이너: 외야수 앞(약 85 m 이내)에 2.5초 안에 떨어진다
            FlightResult f = Flight().Simulate(UnitConversion.MphToKmh(100), 15, 0);
            Assert.True(f.DistanceM < 85);
            Assert.True(f.CatchTimeS < 2.5);
        }

        [Fact]
        public void 빗맞은_뜬공은_더_오래_뜬다()
        {
            double weakLift = _config.Physics.WeakContactLiftMultiplier;
            FlightResult normal = Flight().Simulate(115, 30, 0);
            FlightResult weak = Flight().Simulate(115, 30, 0, weakLift);
            Assert.True(weak.CatchTimeS > normal.CatchTimeS);
        }

        [Fact]
        public void 땅볼경로_멈춤과_시간()
        {
            var path = new GroundPath(new FieldPoint(0, 0), 0, 0, 20, 4);
            Assert.Equal(50, path.StopDistanceM, 6);
            Assert.Equal(0, path.TimeAt(0), 10);
            Assert.True(path.TimeAt(30) > 30 / 20.0);
            Assert.True(double.IsPositiveInfinity(path.TimeAt(51)));
            Assert.Equal(5.0, path.StopTimeS, 6);
        }

        [Fact]
        public void 숙련도와_실효능력()
        {
            DefenseConfig dc = _config.Defense;
            Assert.Equal(70, DefenseCalculator.EffectiveRating(70, dc.FullProficiency, dc), 6);
            Assert.True(DefenseCalculator.EffectiveRating(70, FielderRatings.Unrated, dc) < 70);
            Assert.True(DefenseCalculator.EffectiveRating(70, 80, dc) > 70);
            Assert.Equal(DefenseCalculator.EffectiveRating(70, dc.DefaultProficiency, dc),
                DefenseCalculator.EffectiveRating(70, FielderRatings.Unrated, dc), 10);
        }

        [Fact]
        public void 주자시간_추가베이스는_첫베이스보다_빠르다()
        {
            var model = new BaserunningModel(_config.Baserunning);
            var runner = new RunnerProfile(1, new BatterRatings(), Hand.Right);
            double first = model.TimeToBase(runner, 0, 1);
            double second = model.TimeToBase(runner, 0, 2);
            Assert.Equal(_config.Baserunning.HomeToFirstS, first, 6);
            Assert.True(second - first < model.BaseToBase(runner));
            var fast = new RunnerProfile(2, new BatterRatings { Speed = 75 }, Hand.Left);
            Assert.True(model.TimeToBase(fast, 0, 1) < first);
        }

        [Fact]
        public void 포스상태_판정()
        {
            var s = new PlaySituation();
            s.Runners[0] = new RunnerProfile(1, new BatterRatings(), Hand.Right);
            s.Runners[2] = new RunnerProfile(3, new BatterRatings(), Hand.Right);
            Assert.True(s.IsForced(1));
            Assert.False(s.IsForced(3));
        }

        [Theory]
        [InlineData(115, 22, 4, true)]   // 중견수 앞 69 m 라이너
        [InlineData(101, 46, 0, false)]  // 중견수 앞 61 m 빗맞은 뜬공
        public void 중견수_앞에_짧게_떨어진_안타는_대부분_1루타(double evKmh, double laDeg, double sprayDeg, bool solid)
        {
            var field = new FieldGeometry(_config.Field);
            var resolver = new FieldingResolver(_config, field);
            var defense = DefensiveAlignment.Build(p => TestData.Hitter(100 + (int)p, p), field, _config);
            var rng = new Pcg32Random(7);
            int hits = 0;
            int extraBase = 0;
            for (int i = 0; i < 2000; i++)
            {
                var s = new PlaySituation
                {
                    OutsBefore = 0,
                    Batter = new RunnerProfile(1, new BatterRatings(), Hand.Right),
                    Defense = defense,
                };
                var ball = new BattedBall { ExitVelocityKmh = evKmh, LaunchAngleDeg = laDeg, SprayAngleDeg = sprayDeg, IsSolid = solid };
                PlayResult r = resolver.Resolve(ball, s, rng);
                if (r.Outcome == PlateAppearanceOutcome.Single)
                {
                    hits++;
                }
                else if (r.Outcome == PlateAppearanceOutcome.Double || r.Outcome == PlateAppearanceOutcome.Triple)
                {
                    hits++;
                    extraBase++;
                }
            }

            Assert.True(hits > 0);
            Assert.True(extraBase <= 0.1 * hits, $"장타 {extraBase} / 안타 {hits}");
        }

        [Fact]
        public void 판정결과_주자이동은_일관성이_있다()
        {
            var field = new FieldGeometry(_config.Field);
            var resolver = new FieldingResolver(_config, field);
            var defense = DefensiveAlignment.Build(p => TestData.Hitter(100 + (int)p, p), field, _config);
            var rng = new Pcg32Random(21);
            for (int i = 0; i < 3000; i++)
            {
                var s = new PlaySituation
                {
                    OutsBefore = i % 3,
                    Batter = new RunnerProfile(1, new BatterRatings(), Hand.Right),
                    Defense = defense,
                };
                for (int b = 0; b < 3; b++)
                {
                    if (rng.NextDouble() < 0.4)
                    {
                        s.Runners[b] = new RunnerProfile(10 + b, new BatterRatings(), Hand.Right);
                    }
                }

                var ball = new BattedBall
                {
                    ExitVelocityKmh = rng.Range(60, 180),
                    LaunchAngleDeg = rng.Range(-40, 70),
                    SprayAngleDeg = rng.Range(-45, 45),
                };
                PlayResult r = resolver.Resolve(ball, s, rng);

                // 살아남은 주자는 서로 다른 베이스, 아웃 수는 0~3
                var occupied = new bool[5];
                int outs = 0;
                foreach (RunnerMovement m in r.Movements)
                {
                    if (m.IsOut)
                    {
                        outs++;
                        continue;
                    }

                    if (m.ToBase >= 1 && m.ToBase <= 3)
                    {
                        Assert.False(occupied[m.ToBase], "같은 베이스에 두 주자");
                        occupied[m.ToBase] = true;
                    }

                    Assert.True(m.ToBase >= m.FromBase || m.FromBase == 0, "주자가 뒤로 갔다");
                }

                Assert.Equal(outs, r.OutsRecorded);
                Assert.InRange(r.OutsRecorded, 0, 3);
                if (PlateAppearanceOutcomeInfo.IsHit(r.Outcome))
                {
                    Assert.False(r.OutsRecorded > 0 && r.Outcome == PlateAppearanceOutcome.HomeRun);
                }
            }
        }
    }
}
