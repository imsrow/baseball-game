using System;
using BaseballSim.Engine.AI;
using BaseballSim.Engine.Batting;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Probability;
using BaseballSim.Engine.Randomness;
using BaseballSim.Engine.Ratings;
using Xunit;

namespace BaseballSim.Engine.Tests
{
    public class BattingTests
    {
        private readonly LeagueConfig _config = LeagueConfig.CreateDefault();

        private static ExecutedPitch Pitch(AttackRegion region, double stuffZ = 0)
        {
            return new ExecutedPitch
            {
                Type = PitchType.FourSeam,
                Actual = new PlateLocation(0, 0.77),
                Region = region,
                IsInZone = region == AttackRegion.Heart,
                EffectiveStuffZ = stuffZ,
            };
        }

        [Fact]
        public void 컨택_평균대결은_구역별_리그값()
        {
            var resolver = new ContactResolver(_config);
            double p = resolver.ContactProbability(new BatterRatings(), Pitch(AttackRegion.Chase), 0, false, null);
            Assert.Equal(_config.Environment.ContactRateByRegion.Chase, p, 10);
        }

        [Fact]
        public void 컨택_삼진회피와_구위()
        {
            var resolver = new ContactResolver(_config);
            double avg = resolver.ContactProbability(new BatterRatings(), Pitch(AttackRegion.Shadow), 0, false, null);
            double good = resolver.ContactProbability(new BatterRatings { AvoidK = 70 }, Pitch(AttackRegion.Shadow), 0, false, null);
            double nasty = resolver.ContactProbability(new BatterRatings(), Pitch(AttackRegion.Shadow, 2.0), 0, false, null);
            double twoStrikes = resolver.ContactProbability(new BatterRatings(), Pitch(AttackRegion.Shadow), 2, false, null);
            double platoon = resolver.ContactProbability(new BatterRatings(), Pitch(AttackRegion.Shadow), 0, true, null);
            Assert.True(good > avg);
            Assert.True(nasty < avg);
            Assert.True(twoStrikes > avg);
            Assert.True(platoon < avg);
        }

        [Fact]
        public void 컨택_타이밍입력은_상한까지만()
        {
            var resolver = new ContactResolver(_config);
            double max = resolver.ContactProbability(new BatterRatings(), Pitch(AttackRegion.Heart), 0, false, 1.0);
            double over = resolver.ContactProbability(new BatterRatings(), Pitch(AttackRegion.Heart), 0, false, 9.0);
            Assert.Equal(max, over, 12);
        }

        [Fact]
        public void 컨택_타이밍입력_보상과_벌칙_상한은_따로()
        {
            LeagueConfig config = LeagueConfig.CreateDefault();
            config.InputModifier.MaxSwingContactLogitShift = 1.0;
            config.InputModifier.MaxSwingContactLogitPenalty = 0.4;
            var resolver = new ContactResolver(config);
            double neutral = resolver.ContactProbability(new BatterRatings(), Pitch(AttackRegion.Heart), 0, false, null);
            double good = resolver.ContactProbability(new BatterRatings(), Pitch(AttackRegion.Heart), 0, false, 1.0);
            double bad = resolver.ContactProbability(new BatterRatings(), Pitch(AttackRegion.Heart), 0, false, -1.0);
            Assert.Equal(1.0, LogOdds.Logit(good) - LogOdds.Logit(neutral), 9);
            Assert.Equal(-0.4, LogOdds.Logit(bad) - LogOdds.Logit(neutral), 9);
        }

        [Fact]
        public void 스윙_선구안이_좋으면_유인구에_덜_속는다()
        {
            var chase = new PerceivedPitch { Region = AttackRegion.Chase, IsInZone = false };
            double good = SwingDecisionModel.SwingProbability(_config, 70, chase, 0, 1, 1, false);
            double poor = SwingDecisionModel.SwingProbability(_config, 30, chase, 0, 1, 1, false);
            Assert.True(good < poor);
        }

        [Fact]
        public void 스윙_카운트와_구역()
        {
            var heart = new PerceivedPitch { Region = AttackRegion.Heart, IsInZone = true };
            var waste = new PerceivedPitch { Region = AttackRegion.Waste, IsInZone = false };
            Assert.True(SwingDecisionModel.SwingProbability(_config, 50, heart, 0, 1, 1, false)
                > SwingDecisionModel.SwingProbability(_config, 50, waste, 0, 1, 1, false));
            Assert.True(SwingDecisionModel.SwingProbability(_config, 50, heart, 0, 3, 0, false)
                < SwingDecisionModel.SwingProbability(_config, 50, heart, 0, 0, 2, false));
        }

        [Fact]
        public void 인지오차_선구안이_좋을수록_작다()
        {
            var perception = new PitchPerception(_config.Swing, new StrikeZone(_config.StrikeZone));
            Assert.True(perception.PerceptionSigma(70, 0) < perception.PerceptionSigma(50, 0));
            Assert.True(perception.PerceptionSigma(50, 1.5) > perception.PerceptionSigma(50, 0));
        }

        [Fact]
        public void 인지위치는_실제위치와_다르다()
        {
            var perception = new PitchPerception(_config.Swing, new StrikeZone(_config.StrikeZone));
            var rng = new Pcg32Random(3);
            PerceivedPitch seen = perception.Perceive(Pitch(AttackRegion.Heart), new BatterRatings(), rng);
            Assert.NotEqual(0.0, seen.Location.X);
        }

        [Fact]
        public void 타구_파워와_당겨치기()
        {
            var gen = new BattedBallGenerator(_config);
            var rng = new Pcg32Random(8);
            double evAvg = 0, evPower = 0, sprayRight = 0, sprayLeft = 0;
            const int n = 5000;
            for (int i = 0; i < n; i++)
            {
                evAvg += gen.Generate(new BatterRatings(), Hand.Right, Pitch(AttackRegion.Heart), false, null, rng).ExitVelocityKmh;
                evPower += gen.Generate(new BatterRatings { Power = 75 }, Hand.Right, Pitch(AttackRegion.Heart), false, null, rng).ExitVelocityKmh;
                sprayRight += gen.Generate(new BatterRatings { PullTendency = 75 }, Hand.Right, Pitch(AttackRegion.Heart), false, null, rng).SprayAngleDeg;
                sprayLeft += gen.Generate(new BatterRatings { PullTendency = 75 }, Hand.Left, Pitch(AttackRegion.Heart), false, null, rng).SprayAngleDeg;
            }

            Assert.True(evPower > evAvg + 2 * n);
            Assert.True(sprayRight / n < -5, "우타자 당겨치기는 좌측(−)");
            Assert.True(sprayLeft / n > 5, "좌타자 당겨치기는 우측(+)");
        }

        [Fact]
        public void 타구_방향은_페어지역()
        {
            var gen = new BattedBallGenerator(_config);
            var rng = new Pcg32Random(12);
            for (int i = 0; i < 3000; i++)
            {
                BattedBall ball = gen.Generate(new BatterRatings { PullTendency = 80 }, Hand.Right, Pitch(AttackRegion.Heart), false, null, rng);
                Assert.InRange(ball.SprayAngleDeg, -_config.BattedBall.FairAngleDeg, _config.BattedBall.FairAngleDeg);
            }
        }

        [Fact]
        public void 타이밍_방향_이르면_당겨치기_늦으면_밀어치기()
        {
            var gen = new BattedBallGenerator(_config);
            double early = 0, late = 0, earlyLeft = 0;
            const int n = 3000;
            var rngEarly = new Pcg32Random(21);
            var rngLate = new Pcg32Random(21);
            var rngEarlyLeft = new Pcg32Random(21);
            for (int i = 0; i < n; i++)
            {
                early += gen.Generate(new BatterRatings(), Hand.Right, Pitch(AttackRegion.Heart), false, null, rngEarly, -1.0).SprayAngleDeg;
                late += gen.Generate(new BatterRatings(), Hand.Right, Pitch(AttackRegion.Heart), false, null, rngLate, 1.0).SprayAngleDeg;
                earlyLeft += gen.Generate(new BatterRatings(), Hand.Left, Pitch(AttackRegion.Heart), false, null, rngEarlyLeft, -1.0).SprayAngleDeg;
            }

            Assert.True(early / n < late / n - 10, "우타자: 이르면 좌측(−), 늦으면 우측(+)");
            Assert.True(earlyLeft / n > 10, "좌타자: 이르면 우측(+)");
        }

        [Fact]
        public void 커서가_공보다_위면_발사각_낮게()
        {
            var gen = new BattedBallGenerator(_config);
            double above = 0, below = 0;
            const int n = 3000;
            var rngAbove = new Pcg32Random(22);
            var rngBelow = new Pcg32Random(22);
            for (int i = 0; i < n; i++)
            {
                above += gen.Generate(new BatterRatings(), Hand.Right, Pitch(AttackRegion.Heart), false, null, rngAbove, null, 1.0).LaunchAngleDeg;
                below += gen.Generate(new BatterRatings(), Hand.Right, Pitch(AttackRegion.Heart), false, null, rngBelow, null, -1.0).LaunchAngleDeg;
            }

            Assert.True(above / n < below / n - 10);
        }

        [Fact]
        public void 방향_입력이_없으면_기존과_동일()
        {
            var gen = new BattedBallGenerator(_config);
            var rngA = new Pcg32Random(23);
            var rngB = new Pcg32Random(23);
            for (int i = 0; i < 500; i++)
            {
                BattedBall a = gen.Generate(new BatterRatings(), Hand.Right, Pitch(AttackRegion.Heart), false, null, rngA);
                BattedBall b = gen.Generate(new BatterRatings(), Hand.Right, Pitch(AttackRegion.Heart), false, null, rngB, 0.0, 0.0);
                Assert.Equal(a.SprayAngleDeg, b.SprayAngleDeg, 10);
                Assert.Equal(a.LaunchAngleDeg, b.LaunchAngleDeg, 10);
                Assert.Equal(a.ExitVelocityKmh, b.ExitVelocityKmh, 10);
            }
        }

        [Fact]
        public void 정타확률_평균대결은_리그값()
        {
            // 투구 위치 효과를 끄면 평균 대결은 리그 기준값
            LeagueConfig config = LeagueConfig.CreateDefault();
            config.LocationEffectScale = 0;
            var gen = new BattedBallGenerator(config);
            Assert.Equal(config.Environment.SolidContactRate,
                gen.SolidContactProbability(new BatterRatings(), Pitch(AttackRegion.Heart), false, null), 10);
            Assert.True(gen.SolidContactProbability(new BatterRatings { Contact = 70 }, Pitch(AttackRegion.Heart), false, null)
                > config.Environment.SolidContactRate);
        }

        [Fact]
        public void 투구위치_한가운데일수록_정타확률과_타구속도가_높다()
        {
            var gen = new BattedBallGenerator(_config);
            var batter = new BatterRatings();
            double heart = gen.SolidContactProbability(batter, Pitch(AttackRegion.Heart), false, null);
            double shadow = gen.SolidContactProbability(batter, Pitch(AttackRegion.Shadow), false, null);
            double chase = gen.SolidContactProbability(batter, Pitch(AttackRegion.Chase), false, null);
            double waste = gen.SolidContactProbability(batter, Pitch(AttackRegion.Waste), false, null);
            Assert.True(heart > shadow && shadow > chase && chase > waste);

            Assert.True(AverageExitVelocity(gen, AttackRegion.Heart) > AverageExitVelocity(gen, AttackRegion.Chase) + 8);
        }

        [Fact]
        public void 투구위치_효과_배율()
        {
            var batter = new BatterRatings();
            LeagueConfig off = LeagueConfig.CreateDefault();
            off.LocationEffectScale = 0;
            var genOff = new BattedBallGenerator(off);
            Assert.Equal(genOff.SolidContactProbability(batter, Pitch(AttackRegion.Heart), false, null),
                genOff.SolidContactProbability(batter, Pitch(AttackRegion.Chase), false, null), 10);
            Assert.Equal(AverageExitVelocity(genOff, AttackRegion.Heart), AverageExitVelocity(genOff, AttackRegion.Chase), 6);

            LeagueConfig strong = LeagueConfig.CreateDefault();
            strong.LocationEffectScale = 2;
            var genStrong = new BattedBallGenerator(strong);
            var genNormal = new BattedBallGenerator(_config);
            double gapNormal = genNormal.SolidContactProbability(batter, Pitch(AttackRegion.Heart), false, null)
                - genNormal.SolidContactProbability(batter, Pitch(AttackRegion.Chase), false, null);
            double gapStrong = genStrong.SolidContactProbability(batter, Pitch(AttackRegion.Heart), false, null)
                - genStrong.SolidContactProbability(batter, Pitch(AttackRegion.Chase), false, null);
            Assert.True(gapStrong > gapNormal);
        }

        private static double AverageExitVelocity(BattedBallGenerator gen, AttackRegion region)
        {
            var rng = new Pcg32Random(31);
            double sum = 0;
            const int n = 4000;
            for (int i = 0; i < n; i++)
            {
                sum += gen.Generate(new BatterRatings(), Hand.Right, Pitch(region), false, null, rng).ExitVelocityKmh;
            }

            return sum / n;
        }

        [Fact]
        public void 타구유형_분류()
        {
            Assert.Equal(BattedBallType.GroundBall, BattedBallClassifier.Classify(-5));
            Assert.Equal(BattedBallType.LineDrive, BattedBallClassifier.Classify(15));
            Assert.Equal(BattedBallType.FlyBall, BattedBallClassifier.Classify(35));
            Assert.Equal(BattedBallType.PopUp, BattedBallClassifier.Classify(60));
        }
    }
}
