using System.Linq;
using BaseballSim.Engine.AI;
using BaseballSim.Engine.AI.ManagerAI;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Simulation;
using BaseballSim.Engine.State;
using BaseballSim.Engine.Teams;
using Xunit;

namespace BaseballSim.Engine.Tests
{
    /// <summary>
    /// 고의4구·교체·불펜 역할
    /// </summary>
    public class ManagerTests
    {
        private readonly LeagueConfig _config = LeagueConfig.CreateDefault();

        /// <summary>지정한 팀 감독을 사람으로 두고 해당 감독 결정 지점까지 진행</summary>
        private GameEngine EngineAt(DecisionKind kind, TeamSide humanSide, ulong seed = 1, GameSetup setup = null)
        {
            ControllerSet controllers = AiControllers.CreateAllAi();
            controllers.Assign(humanSide, new PromptingManager());
            var engine = new GameEngine(setup ?? TestData.AverageGame(), _config, seed, controllers);
            while (true)
            {
                engine.RunUntil(StopConditions.EndOfGame);
                if (engine.Pending.Kind == kind)
                {
                    return engine;
                }

                Assert.True(engine.Submit(ManagerOrders.None()).Accepted);
            }
        }

        private ManagerContext Context(GameEngine engine, TeamSide side)
        {
            return new ManagerContext
            {
                State = engine.State,
                Side = side,
                Players = engine.Players,
                Config = _config,
                Random = engine.State.Random,
            };
        }

        [Fact]
        public void 고의4구_투구없이_출루()
        {
            GameEngine engine = EngineAt(DecisionKind.DefenseManager, TeamSide.Home);
            int batter = engine.State.CurrentBatterId;
            int pa = engine.State.PlateAppearanceNumber;
            Assert.True(engine.Submit(ManagerOrders.Of(ManagerAction.IntentionalWalk())).Accepted);

            var ev = engine.State.Log.OfType<IntentionalWalkEvent>().Single();
            Assert.Equal(batter, ev.BatterId);
            Assert.Equal(batter, engine.State.Bases[0].PlayerId);
            Assert.Equal(pa + 1, engine.State.PlateAppearanceNumber);
            Assert.Equal(GamePhase.OffenseManager, engine.State.Phase);
        }

        [Fact]
        public void 대타_교체와_재출전_금지()
        {
            GameEngine engine = EngineAt(DecisionKind.OffenseManager, TeamSide.Away);
            TeamGameState away = engine.State.Away;
            int batter = engine.State.CurrentBatterId;
            int bench = away.Bench[0];

            Assert.False(engine.Submit(ManagerOrders.Of(ManagerAction.PinchHit(engine.State.Home.Bench[0], batter))).Accepted);
            Assert.False(engine.Submit(ManagerOrders.Of(ManagerAction.PinchHit(bench, away.Lineup[5].PlayerId))).Accepted);
            Assert.True(engine.Submit(ManagerOrders.Of(ManagerAction.PinchHit(bench, batter))).Accepted);

            Assert.Equal(bench, engine.State.CurrentBatterId);
            Assert.Contains(batter, away.Removed);
            Assert.DoesNotContain(bench, away.Bench);
            var sub = engine.State.Log.OfType<SubstitutionEvent>().Last();
            Assert.Equal(SubstitutionKind.PinchHitter, sub.Kind);
            Assert.True(sub.ByHuman);
        }

        [Fact]
        public void 대주자는_루상_주자를_바꾼다()
        {
            GameEngine engine = EngineAt(DecisionKind.OffenseManager, TeamSide.Away);
            TeamGameState away = engine.State.Away;
            int runner = away.Lineup[8].PlayerId;
            engine.State.Bases[1] = new BaseRunner(runner, engine.State.Home.CurrentPitcherId);
            int bench = away.Bench[1];

            Assert.False(engine.Submit(ManagerOrders.Of(ManagerAction.PinchRun(bench, 1))).Accepted);
            Assert.True(engine.Submit(ManagerOrders.Of(ManagerAction.PinchRun(bench, 2))).Accepted);
            Assert.Equal(bench, engine.State.Bases[1].PlayerId);
            Assert.Equal(bench, away.Lineup[8].PlayerId);
        }

        [Fact]
        public void 대수비는_같은_포지션을_이어받는다()
        {
            GameEngine engine = EngineAt(DecisionKind.DefenseManager, TeamSide.Home);
            TeamGameState home = engine.State.Home;
            int outgoing = home.PlayerIdAt(Position.Shortstop);
            int bench = home.Bench[1];
            Assert.True(engine.Submit(ManagerOrders.Of(ManagerAction.DefensiveSub(bench, outgoing))).Accepted);
            Assert.Equal(bench, home.PlayerIdAt(Position.Shortstop));
            Assert.Equal(GamePhase.OffensePrePitch, engine.State.Phase);
        }

        [Fact]
        public void 세이브상황_반이닝_시작이면_마무리()
        {
            var engine = new GameEngine(TestData.AverageGame(), _config, 1, AiControllers.CreateAllAi());
            GameState state = engine.State;
            state.Inning = 9;
            state.IsTopHalf = true;
            state.PlateAppearancesThisHalf = 0;
            state.Home.Runs = 3;
            state.Away.Runs = 1;
            Assert.Equal(state.Home.CloserId, PitchingChangeAdvisor.Advise(Context(engine, TeamSide.Home)));

            state.Home.Runs = 8;
            Assert.Equal(-1, PitchingChangeAdvisor.Advise(Context(engine, TeamSide.Home)));
        }

        [Fact]
        public void 셋업은_8회_리드에_등판()
        {
            var engine = new GameEngine(TestData.AverageGame(), _config, 1, AiControllers.CreateAllAi());
            GameState state = engine.State;
            state.Inning = 8;
            state.IsTopHalf = true;
            state.Home.Runs = 2;
            state.Away.Runs = 1;
            state.Home.CurrentPitcher.PitchCount = 100;
            Assert.Equal(state.Home.SetupIds[0], PitchingChangeAdvisor.Advise(Context(engine, TeamSide.Home)));
        }

        [Fact]
        public void 고의4구_판단_강타자_다음_약타자()
        {
            GameSetup setup = TestData.AverageGame();
            var engine = new GameEngine(setup, _config, 1, AiControllers.CreateAllAi());
            GameState state = engine.State;
            state.Inning = 8;
            state.Bases[1] = new BaseRunner(state.Away.Lineup[8].PlayerId, state.Home.CurrentPitcherId);
            Player batter = engine.Players.Get(state.Away.Lineup[0].PlayerId);
            Player next = engine.Players.Get(state.Away.Lineup[1].PlayerId);
            batter.Batting.Contact = 75;
            batter.Batting.Power = 75;
            next.Batting.Contact = 30;
            next.Batting.Power = 30;
            Assert.True(IntentionalWalkAdvisor.Advise(Context(engine, TeamSide.Home)));

            state.Bases[0] = new BaseRunner(state.Away.Lineup[7].PlayerId, state.Home.CurrentPitcherId);
            Assert.False(IntentionalWalkAdvisor.Advise(Context(engine, TeamSide.Home)));
        }

        [Fact]
        public void 시뮬_경기에서_교체규칙이_지켜진다()
        {
            for (ulong seed = 1; seed <= 40; seed++)
            {
                GameState state = GameSimulator.Play(TestData.AverageGame(), _config, seed);
                foreach (TeamGameState team in new[] { state.Away, state.Home })
                {
                    // 교체되어 나간 선수는 라인업에 없다
                    foreach (int removed in team.Removed)
                    {
                        Assert.Equal(-1, team.LineupIndexOf(removed));
                    }

                    // 라인업 포지션은 중복 없이 9자리
                    Assert.Equal(9, team.Lineup.Select(s => s.Position).Distinct().Count());
                }

                int runs = state.Log.OfType<PitchEvent>().Sum(e => e.RunsScored)
                    + state.Log.OfType<IntentionalWalkEvent>().Sum(e => e.RunsScored);
                Assert.Equal(state.AwayScore + state.HomeScore, runs);
            }
        }
    }
}
