using System;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Randomness;
using BaseballSim.Engine.Ratings;
using Xunit;

namespace BaseballSim.Engine.Tests
{
    public class PitchingTests
    {
        private readonly LeagueConfig _config = LeagueConfig.CreateDefault();

        [Fact]
        public void 존_분류()
        {
            var zone = new StrikeZone(_config.StrikeZone);
            double c = _config.StrikeZone.CenterHeightM;
            double w = _config.StrikeZone.HalfWidthM;
            Assert.Equal(AttackRegion.Heart, zone.Classify(new PlateLocation(0, c)));
            Assert.Equal(AttackRegion.Shadow, zone.Classify(new PlateLocation(w, c)));
            Assert.Equal(AttackRegion.Chase, zone.Classify(new PlateLocation(w * 1.6, c)));
            Assert.Equal(AttackRegion.Waste, zone.Classify(new PlateLocation(w * 2.5, c)));
            Assert.True(zone.IsInZone(new PlateLocation(w * 0.99, c)));
            Assert.False(zone.IsInZone(new PlateLocation(w * 1.01, c)));
        }

        [Fact]
        public void 경계거리_부호()
        {
            var zone = new StrikeZone(_config.StrikeZone);
            double c = _config.StrikeZone.CenterHeightM;
            double w = _config.StrikeZone.HalfWidthM;
            Assert.True(zone.SignedEdgeDistance(new PlateLocation(0, c)) < 0);
            Assert.Equal(0.05, zone.SignedEdgeDistance(new PlateLocation(w + 0.05, c)), 6);
            Assert.Equal(-0.05, zone.SignedEdgeDistance(new PlateLocation(w - 0.05, c)), 6);
        }

        [Fact]
        public void 몸쪽은_타석방향에_따라_반대()
        {
            var loc = new PlateLocation(-0.3, 0.8);
            Assert.Equal(0.3, StrikeZone.InsideAmount(loc, Hand.Right), 10);
            Assert.Equal(-0.3, StrikeZone.InsideAmount(loc, Hand.Left), 10);
        }

        [Fact]
        public void 판정확률_존안은_높고_밖은_낮다()
        {
            var zone = new StrikeZone(_config.StrikeZone);
            var umpire = new UmpireJudge(_config.StrikeZone, zone);
            double c = _config.StrikeZone.CenterHeightM;
            double w = _config.StrikeZone.HalfWidthM;
            Assert.True(umpire.CalledStrikeProbability(new PlateLocation(0, c), 50) > 0.99);
            Assert.Equal(0.5, umpire.CalledStrikeProbability(new PlateLocation(w, c), 50), 6);
            Assert.True(umpire.CalledStrikeProbability(new PlateLocation(w + 0.15, c), 50) < 0.01);
        }

        [Fact]
        public void 프레이밍은_Shadow에서만_스트라이크를_늘린다()
        {
            var zone = new StrikeZone(_config.StrikeZone);
            var umpire = new UmpireJudge(_config.StrikeZone, zone);
            double c = _config.StrikeZone.CenterHeightM;
            var edge = new PlateLocation(_config.StrikeZone.HalfWidthM + 0.01, c);
            Assert.True(umpire.CalledStrikeProbability(edge, 70) > umpire.CalledStrikeProbability(edge, 30));
            var heart = new PlateLocation(0, c);
            Assert.Equal(umpire.CalledStrikeProbability(heart, 30), umpire.CalledStrikeProbability(heart, 70), 10);
        }

        [Fact]
        public void 제구가_좋으면_오차가_작다()
        {
            Assert.True(Spread(70) < Spread(50));
            Assert.True(Spread(50) < Spread(30));
        }

        private double Spread(int control)
        {
            var executor = new PitchExecutor(_config, new StrikeZone(_config.StrikeZone));
            var pitcher = TestData.Pitcher(1, 50).Pitching;
            pitcher.Control = control;
            var rng = new Pcg32Random(11);
            var target = new PlateLocation(0, 0.8);
            double sumSq = 0;
            const int n = 4000;
            for (int i = 0; i < n; i++)
            {
                ExecutedPitch p = executor.Execute(PitchType.FourSeam, target, pitcher, 0, null, rng);
                sumSq += Math.Pow(p.Actual.X - target.X, 2);
            }

            return Math.Sqrt(sumSq / n);
        }

        [Fact]
        public void 구속_구종차이와_피로()
        {
            var executor = new PitchExecutor(_config, new StrikeZone(_config.StrikeZone));
            var pitcher = TestData.Pitcher(1, 50).Pitching;
            pitcher.Repertoire.Add(new PitchRating(PitchType.Curveball, 50, 0.1));
            var rng = new Pcg32Random(5);
            double ff = 0, cu = 0, tired = 0;
            const int n = 2000;
            for (int i = 0; i < n; i++)
            {
                ff += executor.Execute(PitchType.FourSeam, new PlateLocation(0, 0.8), pitcher, 0, null, rng).VelocityKmh;
                cu += executor.Execute(PitchType.Curveball, new PlateLocation(0, 0.8), pitcher, 0, null, rng).VelocityKmh;
                tired += executor.Execute(PitchType.FourSeam, new PlateLocation(0, 0.8), pitcher, 1.0, null, rng).VelocityKmh;
            }

            Assert.Equal(_config.Pitch.FastballBaseVelocityKmh, ff / n, 0);
            Assert.True(cu / n < ff / n - 15);
            Assert.True(tired / n < ff / n - 2);
        }

        [Fact]
        public void 릴리스_입력품질은_상한_안에서만_오차를_줄인다()
        {
            var executor = new PitchExecutor(_config, new StrikeZone(_config.StrikeZone));
            var pitcher = TestData.Pitcher(1, 50).Pitching;
            var a = new Pcg32Random(9);
            var b = new Pcg32Random(9);
            var c = new Pcg32Random(9);
            var target = new PlateLocation(0, 0.8);
            ExecutedPitch neutral = executor.Execute(PitchType.FourSeam, target, pitcher, 0, null, a);
            ExecutedPitch good = executor.Execute(PitchType.FourSeam, target, pitcher, 0, 1.0, b);
            ExecutedPitch beyond = executor.Execute(PitchType.FourSeam, target, pitcher, 0, 5.0, c);
            double ratio = (good.Actual.X - target.X) / (neutral.Actual.X - target.X);
            Assert.Equal(Math.Exp(-_config.InputModifier.MaxPitchExecutionSigmaLogShift), ratio, 6);
            Assert.Equal(good.Actual.X, beyond.Actual.X, 10);
        }

        [Fact]
        public void 피로도()
        {
            var ratings = new PitcherRatings { Stamina = 50 };
            FatigueConfig fc = _config.Fatigue;
            Assert.Equal(0, FatigueModel.Fatigue(ratings, (int)fc.ComfortPitchesBase, fc));
            Assert.Equal(0.5, FatigueModel.Fatigue(ratings, (int)(fc.ComfortPitchesBase + fc.FadePitches / 2), fc), 6);
            Assert.Equal(1.0, FatigueModel.Fatigue(ratings, 500, fc));
            var strong = new PitcherRatings { Stamina = 70 };
            Assert.True(FatigueModel.ComfortPitches(strong, fc) > FatigueModel.ComfortPitches(ratings, fc));
        }
    }
}
