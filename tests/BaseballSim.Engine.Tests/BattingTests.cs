using System;
using BaseballSim.Engine.AI;
using BaseballSim.Engine.Batting;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
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
        public void 정타확률_평균대결은_리그값()
        {
            var gen = new BattedBallGenerator(_config);
            Assert.Equal(_config.Environment.SolidContactRate,
                gen.SolidContactProbability(new BatterRatings(), Pitch(AttackRegion.Heart), false, null), 10);
            Assert.True(gen.SolidContactProbability(new BatterRatings { Contact = 70 }, Pitch(AttackRegion.Heart), false, null)
                > _config.Environment.SolidContactRate);
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
