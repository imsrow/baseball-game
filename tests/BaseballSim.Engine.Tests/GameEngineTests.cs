using System.Linq;
using BaseballSim.Engine.AI;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Simulation;
using BaseballSim.Engine.State;
using Xunit;

namespace BaseballSim.Engine.Tests
{
    public class GameEngineTests
    {
        private readonly LeagueConfig _config = LeagueConfig.CreateDefault();

        private static string Fingerprint(GameState state)
        {
            return string.Join("|", state.Log.OfType<PitchEvent>().Select(e =>
                e.Sequence + ":" + e.PitchType + ":" + e.PlateX.ToString("0.0000") + ":" + e.Result + ":" + e.PlateAppearanceOutcome));
        }

        [Fact]
        public void 순수시뮬_경기가_정상적으로_끝난다()
        {
            GameState state = GameSimulator.Play(TestData.AverageGame(), _config, 42);
            Assert.True(state.IsGameOver);
            Assert.True(state.Inning >= _config.Rules.InningsPerGame);
            Assert.NotEqual(state.AwayScore, state.HomeScore);
            Assert.NotNull(state.Winner);

            // 이벤트 로그에서 계산한 득점 = 상태의 점수
            int runs = state.Log.OfType<PitchEvent>().Sum(e => e.RunsScored);
            Assert.Equal(state.AwayScore + state.HomeScore, runs);
        }

        [Fact]
        public void 같은_시드면_같은_로그_다른_시드면_다른_로그()
        {
            string a = Fingerprint(GameSimulator.Play(TestData.AverageGame(), _config, 7));
            string b = Fingerprint(GameSimulator.Play(TestData.AverageGame(), _config, 7));
            string c = Fingerprint(GameSimulator.Play(TestData.AverageGame(), _config, 8));
            Assert.Equal(a, b);
            Assert.NotEqual(a, c);
        }

        [Fact]
        public void 여러_경기_규칙_불변식()
        {
            for (ulong seed = 1; seed <= 30; seed++)
            {
                GameState state = GameSimulator.Play(TestData.AverageGame(), _config, seed);
                Assert.True(state.IsGameOver);
                foreach (PitchEvent e in state.Log.OfType<PitchEvent>())
                {
                    Assert.InRange(e.BallsBefore, 0, 3);
                    Assert.InRange(e.StrikesBefore, 0, 2);
                    Assert.InRange(e.OutsBefore, 0, 2);
                    Assert.InRange(e.OutsAfter, 0, 3);
                }
            }
        }

        [Fact]
        public void 사람_타자는_입력을_기다리고_Submit으로_진행()
        {
            ControllerSet controllers = AiControllers.CreateAllAi();
            controllers.Assign(TeamSide.Away, HumanBattingDecision.Instance);
            var engine = new GameEngine(TestData.AverageGame(), _config, 3, controllers);

            RunResult run = engine.RunUntil(StopConditions.EndOfGame);
            Assert.Equal(RunStatus.AwaitingInput, run.Status);
            Assert.Equal(DecisionKind.Swing, engine.Pending.Kind);
            Assert.Equal(TeamSide.Away, engine.Pending.Side);
            Assert.NotNull(engine.Pending.BattingContext.Perceived);

            // 대기 중이 아닌 결정은 거절
            Assert.False(engine.Submit(new PitchCall(PitchType.FourSeam, new PlateLocation(0, 0.8))).Accepted);

            int before = engine.State.Log.Count;
            Assert.True(engine.Submit(BatterAction.Take()).Accepted);
            Assert.Null(engine.Pending);
            Assert.Equal(before + 1, engine.State.Log.Count);
            Assert.True(((PitchEvent)engine.State.Log.Last()).SwingByHuman);
        }

        [Fact]
        public void 사람_투수_레퍼토리에_없는_구종은_거절()
        {
            ControllerSet controllers = AiControllers.CreateAllAi();
            controllers.Assign(TeamSide.Home, HumanPitchingDecision.Instance);
            var engine = new GameEngine(TestData.AverageGame(), _config, 3, controllers);

            engine.RunUntil(StopConditions.EndOfGame);
            Assert.Equal(DecisionKind.Pitch, engine.Pending.Kind);
            SubmitResult rejected = engine.Submit(new PitchCall(PitchType.Curveball, new PlateLocation(0, 0.8)));
            Assert.False(rejected.Accepted);
            Assert.NotNull(rejected.Reason);
            Assert.NotNull(engine.Pending);
            Assert.True(engine.Submit(new PitchCall(PitchType.Slider, new PlateLocation(0.1, 0.6))).Accepted);
            Assert.Equal(GamePhase.Swing, engine.State.Phase);
        }

        [Fact]
        public void 입력대기_중_AI로_바꾸면_이어서_끝까지_진행()
        {
            ControllerSet controllers = AiControllers.CreateAllAi();
            controllers.Assign(TeamSide.Home, HumanBattingDecision.Instance);
            var engine = new GameEngine(TestData.AverageGame(), _config, 5, controllers);

            Assert.Equal(RunStatus.AwaitingInput, engine.RunUntil(StopConditions.EndOfGame).Status);
            AiControllers.AssignAi(controllers, TeamSide.Home, DecisionRole.Batting);
            Assert.Equal(RunStatus.GameOver, engine.RunUntil(StopConditions.EndOfGame).Status);
            Assert.True(engine.State.IsGameOver);
        }

        [Fact]
        public void 사람이_전부_지켜보면_볼넷이나_삼진으로_끝난다()
        {
            ControllerSet controllers = AiControllers.CreateAllAi();
            controllers.Assign(TeamSide.Away, HumanBattingDecision.Instance);
            var engine = new GameEngine(TestData.AverageGame(), _config, 11, controllers);

            for (int i = 0; i < 200 && engine.State.PlateAppearanceNumber == 1; i++)
            {
                engine.RunUntil(StopConditions.EndOfGame);
                engine.Submit(BatterAction.Take());
            }

            PitchEvent last = engine.State.Log.OfType<PitchEvent>().Last(e => e.PlateAppearanceNumber == 1);
            Assert.Contains(last.PlateAppearanceOutcome, new PlateAppearanceOutcome?[]
            {
                PlateAppearanceOutcome.Walk, PlateAppearanceOutcome.Strikeout, PlateAppearanceOutcome.HitByPitch,
            });
        }
    }
}
