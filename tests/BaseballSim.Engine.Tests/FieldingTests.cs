using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Control;
using System.Collections.Generic;
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

        /// <summary>
        /// 주자 상황(8가지) × 아웃(0~2)별 땅볼: 이닝이 안 끝나면 포스 주자는 반드시 진루하거나 포스아웃,
        /// 포스아웃은 포스 주자만, 병살은 1루 주자 2루 아웃 + 타자 1루 아웃
        /// </summary>
        [Fact]
        public void 땅볼_포스_진루와_병살은_주자상황별로_맞다()
        {
            var field = new FieldGeometry(_config.Field);
            var resolver = new FieldingResolver(_config, field);
            var defense = DefensiveAlignment.Build(p => TestData.Hitter(100 + (int)p, p), field, _config);
            var rng = new Pcg32Random(33);
            int outsPerInning = _config.Rules.OutsPerHalfInning;
            var seen = new HashSet<PlateAppearanceOutcome>();
            for (int mask = 0; mask < 8; mask++)
            {
                for (int outs = 0; outs < outsPerInning; outs++)
                {
                    for (int i = 0; i < 300; i++)
                    {
                        var s = new PlaySituation
                        {
                            OutsBefore = outs,
                            Batter = new RunnerProfile(1, new BatterRatings(), Hand.Right),
                            Defense = defense,
                        };
                        for (int b = 0; b < 3; b++)
                        {
                            if ((mask & (1 << b)) != 0)
                            {
                                s.Runners[b] = new RunnerProfile(10 + b, new BatterRatings(), Hand.Right);
                            }
                        }

                        var ball = new BattedBall
                        {
                            ExitVelocityKmh = rng.Range(60, 180),
                            LaunchAngleDeg = rng.Range(-30, 9),
                            SprayAngleDeg = rng.Range(-44, 44),
                        };
                        PlayResult r = resolver.Resolve(ball, s, rng);
                        seen.Add(r.Outcome);
                        bool inningOver = outs + r.OutsRecorded >= outsPerInning;
                        string where = $"mask {mask} outs {outs} {r.Outcome}";

                        for (int b = 1; b <= 3; b++)
                        {
                            RunnerProfile runner = s.RunnerOn(b);
                            if (runner == null)
                            {
                                continue;
                            }

                            RunnerMovement m = r.Movements.Find(x => x.PlayerId == runner.PlayerId);
                            Assert.NotNull(m);
                            bool infieldPlay = r.FieldedBy.HasValue && !PositionInfo.IsOutfield(r.FieldedBy.Value);
                            if (m.IsOut && m.FromBase > 0 && infieldPlay)
                            {
                                // 내야 땅볼 아웃은 포스아웃뿐 (다음 베이스에서). 외야로 빠진 땅볼은 추가 진루 중 송구 아웃이 있을 수 있다
                                Assert.True(s.IsForced(b), where + ": 포스가 아닌 주자 아웃");
                                Assert.Equal(b + 1, m.ToBase);
                            }

                            if (s.IsForced(b) && !inningOver)
                            {
                                Assert.True(m.IsOut || m.ToBase >= b + 1, where + $": {b}루 포스 주자가 진루하지 않음");
                            }
                        }

                        if (r.Outcome == PlateAppearanceOutcome.GroundedIntoDoublePlay)
                        {
                            Assert.Equal(2, r.OutsRecorded);
                            Assert.Contains(r.Movements, m => m.FromBase == 1 && m.ToBase == 2 && m.IsOut);
                            Assert.Contains(r.Movements, m => m.FromBase == 0 && m.ToBase == 1 && m.IsOut);
                        }
                    }
                }
            }

            Assert.Contains(PlateAppearanceOutcome.GroundedIntoDoublePlay, seen);
            Assert.Contains(PlateAppearanceOutcome.FieldersChoice, seen);
            Assert.Contains(PlateAppearanceOutcome.GroundOut, seen);
        }

        [Fact]
        public void 주루성향_공격적이면_추가진루가_늘고_신중하면_줄어든다()
        {
            var field = new FieldGeometry(_config.Field);
            var resolver = new FieldingResolver(_config, field);
            var defense = DefensiveAlignment.Build(p => TestData.Hitter(100 + (int)p, p), field, _config);

            // 1루 주자, 외야 라이너·뜬공 안타가 나올 만한 타구: 1루 주자가 3루까지 가는 비율
            int FirstToThird(BaserunningStyle style)
            {
                var rng = new Pcg32Random(77);
                int count = 0;
                for (int i = 0; i < 4000; i++)
                {
                    var s = new PlaySituation
                    {
                        OutsBefore = i % 2,
                        Batter = new RunnerProfile(1, new BatterRatings(), Hand.Right),
                        Defense = defense,
                        BaserunningStyle = style,
                    };
                    s.Runners[0] = new RunnerProfile(10, new BatterRatings(), Hand.Right);
                    var ball = new BattedBall { ExitVelocityKmh = rng.Range(135, 170), LaunchAngleDeg = rng.Range(5, 22),
                        SprayAngleDeg = rng.Range(-40, 40), IsSolid = true };
                    PlayResult r = resolver.Resolve(ball, s, rng);
                    if (r.Movements.Exists(m => m.FromBase == 1 && m.ToBase >= 3))
                    {
                        count++;
                    }
                }

                return count;
            }

            int normal = FirstToThird(BaserunningStyle.Normal);
            Assert.True(FirstToThird(BaserunningStyle.Aggressive) > normal * 1.1, "공격적인데 3루 진루가 늘지 않음");
            Assert.True(FirstToThird(BaserunningStyle.Cautious) < normal * 0.9, "신중한데 3루 진루가 줄지 않음");
        }

        [Fact]
        public void 번트_입력품질은_능력치에_더해_결과를_보정한다()
        {
            var bunts = new BuntResolver(_config);
            var batter = new BatterRatings();
            var pitch = new ExecutedPitch { IsInZone = true };
            double none = bunts.ContactProbability(batter, pitch);
            Assert.True(bunts.ContactProbability(batter, pitch, 1.0) > none);
            Assert.True(bunts.ContactProbability(batter, pitch, -1.0) < none);
            Assert.Equal(none, bunts.ContactProbability(batter, pitch, 0.0), 10);
            Assert.True(bunts.FoulProbability(batter, 1.0) < bunts.FoulProbability(batter));

            // 희생번트 성공(타자 아웃 + 주자 진루) 비율
            var field = new FieldGeometry(_config.Field);
            var defense = DefensiveAlignment.Build(p => TestData.Hitter(100 + (int)p, p), field, _config);
            double SacrificeRate(double? quality)
            {
                var rng = new Pcg32Random(5);
                int ok = 0;
                for (int i = 0; i < 3000; i++)
                {
                    var s = new PlaySituation { Batter = new RunnerProfile(1, batter, Hand.Right), Defense = defense };
                    s.Runners[0] = new RunnerProfile(10, new BatterRatings(), Hand.Right);
                    PlayResult r = bunts.ResolveFair(BuntType.Sacrifice, batter, s, rng, out BattedBall _, quality);
                    if (r.Movements.Exists(m => m.FromBase == 1 && m.ToBase == 2 && !m.IsOut))
                    {
                        ok++;
                    }
                }

                return ok / 3000.0;
            }

            Assert.True(SacrificeRate(1.0) > SacrificeRate(null));
            Assert.True(SacrificeRate(-1.0) < SacrificeRate(null));
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
