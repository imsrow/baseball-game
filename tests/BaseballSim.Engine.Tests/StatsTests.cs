using BaseballSim.Engine.Events;
using BaseballSim.Engine.Stats;
using Xunit;

namespace BaseballSim.Engine.Tests
{
    public class StatsTests
    {
        [Fact]
        public void 타격기록_비율지표()
        {
            var line = new BattingLine();
            line.Record(PlateAppearanceOutcome.Single);
            line.Record(PlateAppearanceOutcome.HomeRun);
            line.Record(PlateAppearanceOutcome.Strikeout);
            line.Record(PlateAppearanceOutcome.Walk);
            line.Record(PlateAppearanceOutcome.SacrificeFly);
            line.Record(PlateAppearanceOutcome.GroundOut);

            Assert.Equal(6, line.PlateAppearances);
            Assert.Equal(4, line.AtBats);
            Assert.Equal(0.5, line.Avg, 10);
            Assert.Equal(3.0 / 6.0, line.Obp, 10);
            Assert.Equal(5.0 / 4.0, line.Slg, 10);
            // BABIP = (H − HR) / (AB − K − HR + SF) = 1 / (4 − 1 − 1 + 1)
            Assert.Equal(1.0 / 3.0, line.Babip, 10);
            Assert.Equal(1.0 / 6.0, line.StrikeoutRate, 10);
        }
    }
}
