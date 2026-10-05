using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BaseballSim.Engine.AI;
using BaseballSim.Engine.AI.ManagerAI;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Persistence;
using BaseballSim.Engine.Simulation;
using BaseballSim.Engine.State;
using Xunit;

namespace BaseballSim.Engine.Tests
{
    /// <summary>
    /// 직접 플레이 ↔ 시뮬 전환, 멈춤 조건, 사람 감독, 저장/불러오기
    /// </summary>
    public class PlayModeTests
    {
        private readonly LeagueConfig _config = LeagueConfig.CreateDefault();

        private static string Fingerprint(GameState state)
        {
            return string.Join("|", state.Log.Select(e => JsonSerializer.Serialize(e, e.GetType())));
        }

        private GameEngine AiEngine(ulong seed)
        {
            return new GameEngine(TestData.AverageGame(), _config, seed, AiControllers.CreateAllAi());
        }

        private static void StepN(GameEngine engine, int steps)
        {
            for (int i = 0; i < steps && !engine.IsGameOver; i++)
            {
                engine.Step();
            }
        }

        // ───────────── 저장 / 불러오기 ─────────────

        [Fact]
        public void 저장후_이어서_진행해도_중단없는_경기와_같다()
        {
            GameEngine full = AiEngine(17);
            full.RunUntil(StopConditions.EndOfGame);

            GameEngine first = AiEngine(17);
            StepN(first, 400);
            byte[] data = first.Save();
            GameEngine resumed = GameEngine.Load(data, _config, out SavedGame saved);
            Assert.True(saved.ConfigMatches);
            resumed.RunUntil(StopConditions.EndOfGame);

            Assert.Equal(Fingerprint(full.State), Fingerprint(resumed.State));
        }

        [Fact]
        public void 저장_불러오기_왕복은_모든_값을_보존한다()
        {
            GameEngine engine = AiEngine(23);
            StepN(engine, 655);
            byte[] data = engine.Save();
            GameEngine loaded = GameEngine.Load(data, _config, out SavedGame _);

            Assert.Equal(data, loaded.Save());
            Assert.Equal(JsonSerializer.Serialize(engine.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(Fingerprint(engine.State), Fingerprint(loaded.State));
            foreach (var player in engine.Players.All)
            {
                Assert.Equal(JsonSerializer.Serialize(player), JsonSerializer.Serialize(loaded.Players.Get(player.Id)));
            }

            Assert.Equal(engine.TeamNames, loaded.TeamNames);
        }

        [Fact]
        public void 타구_연출용_시각은_순서가_맞다()
        {
            GameEngine engine = AiEngine(31);
            engine.RunUntil(StopConditions.EndOfGame);
            int checkedBalls = 0;
            foreach (GameEvent e in engine.State.Log)
            {
                if (!(e is PitchEvent p) || p.BattedBall == null || p.IsBunt
                    || p.PlateAppearanceOutcome == PlateAppearanceOutcome.HomeRun)
                {
                    continue;
                }

                BattedBallData b = p.BattedBall;
                Assert.True(b.FieldedTimeS > 0);
                Assert.True(b.FielderArrivalS > 0);
                if (b.HasLanding)
                {
                    Assert.True(b.LandingTimeS <= b.FieldedTimeS + 1e-9);
                }

                checkedBalls++;
            }

            Assert.True(checkedBalls > 20);
        }

        // ───────────── 사람 감독: 작전만 사람, 교체는 AI ─────────────

        private GameEngine TacticsEngine(ulong seed, bool offense, bool defense)
        {
            ControllerSet controllers = AiControllers.CreateAllAi();
            foreach (TeamSide side in new[] { TeamSide.Away, TeamSide.Home })
            {
                controllers.Assign(side, new HumanManagerDecision
                {
                    HumanOffenseTactics = offense,
                    HumanDefenseTactics = defense,
                    SubstitutionsByAi = true,
                });
            }

            return new GameEngine(TestData.AverageGame(), _config, seed, controllers);
        }

        [Fact]
        public void 교체를_AI가_맡으면_걸어둔_작전이_없을때_멈추지_않고_경기가_끝난다()
        {
            GameEngine engine = TacticsEngine(41, true, true);
            RunResult run = engine.RunUntil(StopConditions.EndOfGame);
            Assert.Equal(RunStatus.GameOver, run.Status);
            // 교체는 AI가 했다 (투수 교체가 한 번은 나온다)
            Assert.Contains(engine.State.Log, e => e is SubstitutionEvent);
        }

        [Fact]
        public void 걸어둔_고의4구와_도루가_적용된다()
        {
            GameEngine engine = TacticsEngine(42, true, true);
            var home = (HumanManagerDecision)engine.Controllers.Manager(TeamSide.Home);
            home.Queue(ManagerAction.IntentionalWalk());
            engine.RunUntil(StopConditions.EndOfHalfInning());
            // 수비 작전이 사람 담당이면 AI 고의4구는 없으므로, 나온 고의4구는 걸어둔 것
            Assert.Contains(engine.State.Log, e => e is IntentionalWalkEvent);

            // 1루 주자가 생기면 도루를 걸어둔다
            var away = (HumanManagerDecision)engine.Controllers.Manager(TeamSide.Away);
            bool stole = false;
            for (int i = 0; i < 4000 && !engine.IsGameOver && !stole; i++)
            {
                if (engine.State.Phase == GamePhase.OffensePrePitch && engine.State.OffenseSide == TeamSide.Away
                    && engine.State.Bases[0] != null && engine.State.Bases[1] == null)
                {
                    away.Queue(ManagerAction.Steal(1));
                }

                int before = engine.State.Log.Count;
                engine.Step();
                stole = engine.State.Log.Skip(before).OfType<PitchEvent>().Any(p => p.StealFromBase == 1);
            }

            Assert.True(stole, "걸어둔 도루가 한 번도 적용되지 않음");
        }

        [Fact]
        public void 감독_세부설정과_주루성향은_저장된다()
        {
            GameEngine engine = TacticsEngine(43, true, false);
            engine.State.Away.BaserunningStyle = BaserunningStyle.Aggressive;
            StepN(engine, 300);
            GameEngine loaded = GameEngine.Load(engine.Save(), _config, out SavedGame _);
            var manager = Assert.IsType<HumanManagerDecision>(loaded.Controllers.Manager(TeamSide.Home));
            Assert.True(manager.HumanOffenseTactics);
            Assert.False(manager.HumanDefenseTactics);
            Assert.True(manager.SubstitutionsByAi);
            Assert.Equal(BaserunningStyle.Aggressive, loaded.State.Away.BaserunningStyle);
            Assert.Equal(BaserunningStyle.Normal, loaded.State.Home.BaserunningStyle);
        }

        [Fact]
        public void 설정이_바뀌면_불일치를_알린다()
        {
            byte[] data = AiEngine(1).Save();
            LeagueConfig changed = LeagueConfig.CreateDefault();
            changed.Pitch.ControlSigmaM += 0.01;
            GameEngine.Load(data, changed, out SavedGame saved);
            Assert.False(saved.ConfigMatches);
        }

        [Fact]
        public void 잘못된_데이터는_형식예외()
        {
            Assert.Throws<SaveFormatException>(() => GameSaveSerializer.Load(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, _config));
        }

        [Fact]
        public void 사람_입력대기_중_저장하고_불러와도_같은_투구를_다시_묻는다()
        {
            ControllerSet controllers = AiControllers.CreateAllAi();
            controllers.Assign(TeamSide.Away, HumanBattingDecision.Instance);
            var engine = new GameEngine(TestData.AverageGame(), _config, 5, controllers);
            engine.RunUntil(StopConditions.EndOfGame);
            Assert.Equal(DecisionKind.Swing, engine.Pending.Kind);
            double perceivedX = engine.Pending.BattingContext.Perceived.Location.X;

            GameEngine loaded = GameEngine.Load(engine.Save(), _config, out SavedGame saved);
            Assert.True(saved.Modes.IsHuman(TeamSide.Away, DecisionRole.Batting));
            Assert.True(loaded.Controllers.IsHuman(TeamSide.Away, DecisionRole.Batting));
            loaded.RunUntil(StopConditions.EndOfGame);
            Assert.Equal(DecisionKind.Swing, loaded.Pending.Kind);
            Assert.Equal(perceivedX, loaded.Pending.BattingContext.Perceived.Location.X);

            engine.Submit(BatterAction.Swing());
            loaded.Submit(BatterAction.Swing());
            AiControllers.AssignAi(engine.Controllers, TeamSide.Away, DecisionRole.Batting);
            AiControllers.AssignAi(loaded.Controllers, TeamSide.Away, DecisionRole.Batting);
            engine.RunUntil(StopConditions.EndOfGame);
            loaded.RunUntil(StopConditions.EndOfGame);
            Assert.Equal(Fingerprint(engine.State), Fingerprint(loaded.State));
        }

        // ───────────── 전환 / 멈춤 조건 ─────────────

        [Fact]
        public void 직접에서_시뮬로_타석끝까지_넘기고_다시_직접()
        {
            ControllerSet controllers = AiControllers.CreateAllAi();
            controllers.Assign(TeamSide.Away, HumanBattingDecision.Instance);
            var engine = new GameEngine(TestData.AverageGame(), _config, 9, controllers);
            engine.RunUntil(StopConditions.EndOfGame);
            Assert.Equal(DecisionKind.Swing, engine.Pending.Kind);
            int pa = engine.State.PlateAppearanceNumber;

            RunResult run = engine.SimulateUntil(StopConditions.EndOfPlateAppearance());
            Assert.Equal(RunStatus.ConditionMet, run.Status);
            Assert.Equal(pa + 1, engine.State.PlateAppearanceNumber);
            Assert.True(engine.Controllers.IsHuman(TeamSide.Away, DecisionRole.Batting));

            // 시뮬 구간도 같은 로그에 끊김 없이 기록
            List<int> sequences = engine.State.Log.Select(e => e.Sequence).ToList();
            Assert.Equal(Enumerable.Range(1, sequences.Count), sequences);
            Assert.False(engine.State.Log.OfType<PitchEvent>().Last().SwingByHuman);
        }

        [Fact]
        public void 반이닝_이닝_종료에서_멈춘다()
        {
            GameEngine engine = AiEngine(3);
            Assert.Equal(RunStatus.ConditionMet, engine.RunUntil(StopConditions.EndOfHalfInning()).Status);
            Assert.False(engine.State.IsTopHalf);
            Assert.Equal(1, engine.State.Inning);
            Assert.Equal(0, engine.State.Outs);

            Assert.Equal(RunStatus.ConditionMet, engine.RunUntil(StopConditions.EndOfInning()).Status);
            Assert.Equal(2, engine.State.Inning);
            Assert.True(engine.State.IsTopHalf);
        }

        [Fact]
        public void N회_시작에서_멈춘다()
        {
            GameEngine engine = AiEngine(4);
            Assert.Equal(RunStatus.ConditionMet, engine.RunUntil(StopConditions.InningReached(7)).Status);
            Assert.Equal(7, engine.State.Inning);
            Assert.True(engine.State.IsTopHalf);
            Assert.Equal(0, engine.State.PlateAppearancesThisHalf);
            Assert.Equal(GamePhase.OffenseManager, engine.State.Phase);
        }

        [Fact]
        public void 득점권_위기와_찬스에서_멈춘다()
        {
            // 득점권 위기가 실제로 나오는 경기를 고른다 (시드에 따라 한 번도 없을 수 있음)
            GameEngine engine = null;
            RunResult run = null;
            for (ulong seed = 6; seed < 30; seed++)
            {
                engine = AiEngine(seed);
                run = engine.RunUntil(StopConditions.ScoringThreat(TeamSide.Home));
                if (run.Status == RunStatus.ConditionMet)
                {
                    break;
                }
            }

            Assert.Equal(RunStatus.ConditionMet, run.Status);
            Assert.Equal(TeamSide.Home, engine.State.DefenseSide);
            Assert.True(engine.State.HasRunnerInScoringPosition);

            // 같은 타석에서 다시 요청해도 바로 멈추지 않고 다음 타석 이후로 진행
            int pa = engine.State.PlateAppearanceNumber;
            engine.RunUntil(StopConditions.ScoringChance(TeamSide.Away));
            Assert.True(engine.IsGameOver || engine.State.PlateAppearanceNumber > pa);
        }

        [Fact]
        public void 특정타자와_투수교체_시점에서_멈춘다()
        {
            GameEngine engine = AiEngine(8);
            int target = engine.State.Home.Lineup[3].PlayerId;
            engine.RunUntil(StopConditions.PlayerUp(target));
            Assert.Equal(target, engine.State.CurrentBatterId);

            Assert.Equal(RunStatus.ConditionMet, engine.RunUntil(StopConditions.PitchingChangeMoment(TeamSide.Home)).Status);
            var context = new ManagerContext
            {
                State = engine.State,
                Side = TeamSide.Home,
                Players = engine.Players,
                Config = _config,
                Random = engine.State.Random,
            };
            Assert.True(PitchingChangeAdvisor.Advise(context) >= 0);
        }

        [Fact]
        public void Any_조건과_경기종료()
        {
            GameEngine engine = AiEngine(10);
            RunResult run = engine.RunUntil(StopConditions.Any(StopConditions.InningReached(30), StopConditions.EndOfGame));
            Assert.Equal(RunStatus.GameOver, run.Status);
        }

        // ───────────── 사람 감독 ─────────────

        [Fact]
        public void 사람감독은_작전에서_멈추지_않고_교체순간에만_묻는다()
        {
            ControllerSet controllers = AiControllers.CreateAllAi();
            controllers.Assign(TeamSide.Home, new HumanManagerDecision());
            var engine = new GameEngine(TestData.AverageGame(), _config, 12, controllers);
            int prompts = 0;
            while (engine.RunUntil(StopConditions.EndOfGame).Status == RunStatus.AwaitingInput)
            {
                PendingDecision pending = engine.Pending;
                Assert.NotEqual(DecisionKind.OffensePrePitch, pending.Kind);
                Assert.NotNull(pending.ManagerContext.Suggestion);
                Assert.False(pending.ManagerContext.Suggestion.IsEmpty);
                prompts++;
                Assert.True(engine.Submit(prompts % 2 == 0 ? pending.ManagerContext.Suggestion : ManagerOrders.None()).Accepted);
                Assert.True(prompts < 60, "교체 확인이 너무 자주 나온다");
            }

            Assert.True(engine.IsGameOver);
            Assert.True(prompts > 0);
        }

        [Fact]
        public void AI에게_맡기면_순수시뮬과_같다()
        {
            GameEngine ai = AiEngine(14);
            ai.RunUntil(StopConditions.EndOfGame);

            ControllerSet controllers = AiControllers.CreateAllAi();
            controllers.Assign(TeamSide.Away, new HumanManagerDecision { DelegateToAi = true });
            controllers.Assign(TeamSide.Home, new HumanManagerDecision { DelegateToAi = true });
            var engine = new GameEngine(TestData.AverageGame(), _config, 14, controllers);
            Assert.Equal(RunStatus.GameOver, engine.RunUntil(StopConditions.EndOfGame).Status);
            Assert.Equal(Fingerprint(ai.State), Fingerprint(engine.State));
        }

        [Fact]
        public void 미리_걸어둔_작전이_적용되고_무효면_무시된다()
        {
            bool applied = false;
            for (ulong seed = 1; seed <= 20 && !applied; seed++)
            {
                var manager = new HumanManagerDecision { DelegateToAi = true };
                ControllerSet controllers = AiControllers.CreateAllAi();
                controllers.Assign(TeamSide.Away, manager);
                var engine = new GameEngine(TestData.AverageGame(), _config, seed, controllers);
                engine.RunUntil(StopConditions.TeamBatting(TeamSide.Away));
                manager.DelegateToAi = false;

                // 주자 없는 상태에서 2루 도루 → 무효라 무시하고 진행
                manager.Queue(ManagerAction.Steal(2));
                while (engine.State.Phase != GamePhase.Pitch)
                {
                    Assert.Equal(StepStatus.Advanced, engine.Step().Status);
                }

                Assert.Empty(manager.Queued);
                Assert.Equal(0, engine.State.StealFromBase);

                // 1루 주자 도루
                engine.RunUntil(StopConditions.EndOfPlateAppearance());
                int runner = engine.State.Away.Lineup[(engine.State.Away.NextBatterIndex + 8) % 9].PlayerId;
                engine.State.Bases[0] = new BaseRunner(runner, engine.State.Home.CurrentPitcherId);
                engine.State.Bases[1] = null;
                manager.Queue(ManagerAction.Steal(1));
                int before = engine.State.Log.Count;
                while (engine.State.Log.Count == before)
                {
                    Assert.Equal(StepStatus.Advanced, engine.Step().Status);
                }

                PitchEvent ev = engine.State.Log.OfType<PitchEvent>().Last();
                if (ev.Result == PitchResult.Ball || ev.Result == PitchResult.CalledStrike || ev.Result == PitchResult.SwingingStrike)
                {
                    Assert.Equal(1, ev.StealFromBase);
                    applied = true;
                }
            }

            Assert.True(applied);
        }

        [Fact]
        public void 의미없는_작전_결정지점은_건너뛴다()
        {
            var counting = new CountingManager();
            ControllerSet controllers = AiControllers.CreateAllAi();
            controllers.Assign(TeamSide.Away, counting);
            controllers.Assign(TeamSide.Home, counting);
            var engine = new GameEngine(TestData.AverageGame(), _config, 2, controllers);
            engine.RunUntil(StopConditions.EndOfGame);
            Assert.True(counting.PrePitchCalls > 0);
            Assert.Equal(0, counting.MeaninglessPrePitchCalls);
        }

        /// <summary>투구 전 결정이 의미 없는 상황(도루 불가 + 2스트라이크)에서 호출되는지 센다</summary>
        private sealed class CountingManager : IManagerDecision
        {
            private readonly ManagerAi _ai = new ManagerAi();

            public int PrePitchCalls { get; private set; }

            public int MeaninglessPrePitchCalls { get; private set; }

            public Decision<ManagerOrders> DecideOffense(ManagerContext context) => _ai.DecideOffense(context);

            public Decision<ManagerOrders> DecideDefense(ManagerContext context) => _ai.DecideDefense(context);

            public Decision<ManagerOrders> DecidePrePitch(ManagerContext context)
            {
                PrePitchCalls++;
                BaseRunner[] b = context.State.Bases;
                bool canSteal = (b[0] != null && b[1] == null) || (b[1] != null && b[2] == null);
                if (!canSteal && context.State.Strikes >= 2)
                {
                    MeaninglessPrePitchCalls++;
                }

                return _ai.DecidePrePitch(context);
            }
        }
    }
}
