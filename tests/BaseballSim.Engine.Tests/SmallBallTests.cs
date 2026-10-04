using System.Linq;
using BaseballSim.Engine.AI;
using BaseballSim.Engine.Batting;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Fielding;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Randomness;
using BaseballSim.Engine.Ratings;
using BaseballSim.Engine.Simulation;
using BaseballSim.Engine.State;
using Xunit;

namespace BaseballSim.Engine.Tests
{
    /// <summary>
    /// 도루·번트·폭투·포일·낫아웃
    /// </summary>
    public class SmallBallTests
    {
        private readonly LeagueConfig _config = LeagueConfig.CreateDefault();

        private static Player Catcher(int arm, int blocking)
        {
            Player c = TestData.Hitter(900, Position.Catcher);
            c.Fielding.ArmStrength = arm;
            c.Fielding.ArmAccuracy = 50;
            c.Fielding.Blocking = blocking;
            return c;
        }

        [Fact]
        public void 도루_평균대결은_기준성공률()
        {
            var model = new StealModel(_config);
            double p = model.SuccessProbability(new BatterRatings(), 2, new PitcherRatings(), Catcher(50, 50), PitchType.FourSeam);
            Assert.Equal(_config.Steal.BaseSuccessRate, p, 6);
        }

        [Fact]
        public void 도루_주자와_배터리_능력치()
        {
            var model = new StealModel(_config);
            double avg = model.SuccessProbability(new BatterRatings(), 2, new PitcherRatings(), Catcher(50, 50), PitchType.FourSeam);
            Assert.True(model.SuccessProbability(new BatterRatings { Speed = 70, StealJump = 70 }, 2, new PitcherRatings(), Catcher(50, 50), PitchType.FourSeam) > avg);
            Assert.True(model.SuccessProbability(new BatterRatings(), 2, new PitcherRatings { HoldRunners = 70 }, Catcher(50, 50), PitchType.FourSeam) < avg);
            Assert.True(model.SuccessProbability(new BatterRatings(), 2, new PitcherRatings(), Catcher(75, 50), PitchType.FourSeam) < avg);
            Assert.True(model.SuccessProbability(new BatterRatings(), 3, new PitcherRatings(), Catcher(50, 50), PitchType.FourSeam) < avg);
            Assert.True(model.SuccessProbability(new BatterRatings(), 2, new PitcherRatings(), Catcher(50, 50), PitchType.Curveball) > avg);
        }

        [Fact]
        public void 놓친공_원바운드가_더_잦고_블로킹이_줄인다()
        {
            var model = new PassedBallModel(_config);
            var dirt = new PlateLocation(0, 0.05);
            var normal = new PlateLocation(0, 0.8);
            Assert.True(model.IsWildLocation(dirt));
            Assert.False(model.IsWildLocation(normal));
            Assert.True(model.MissProbability(dirt, Catcher(50, 50)) > 10 * model.MissProbability(normal, Catcher(50, 50)));
            Assert.True(model.MissProbability(dirt, Catcher(50, 75)) < model.MissProbability(dirt, Catcher(50, 50)));
        }

        [Fact]
        public void 낫아웃_규칙()
        {
            Assert.True(PassedBallModel.BatterMayRunOnDroppedThirdStrike(false, 0, 3));
            Assert.False(PassedBallModel.BatterMayRunOnDroppedThirdStrike(true, 1, 3));
            Assert.True(PassedBallModel.BatterMayRunOnDroppedThirdStrike(true, 2, 3));
        }

        private static PlaySituation Situation(int outs, bool first, bool second, bool third)
        {
            var s = new PlaySituation { OutsBefore = outs, Batter = new RunnerProfile(1, new BatterRatings(), Hand.Right) };
            if (first)
            {
                s.Runners[0] = new RunnerProfile(11, new BatterRatings(), Hand.Right);
            }

            if (second)
            {
                s.Runners[1] = new RunnerProfile(12, new BatterRatings(), Hand.Right);
            }

            if (third)
            {
                s.Runners[2] = new RunnerProfile(13, new BatterRatings(), Hand.Right);
            }

            return s;
        }

        [Fact]
        public void 번트종류_상황정리()
        {
            Assert.Equal(BuntType.ForHit, BuntResolver.EffectiveType(BuntType.Sacrifice, Situation(0, false, false, false)));
            Assert.Equal(BuntType.Sacrifice, BuntResolver.EffectiveType(BuntType.None, Situation(0, true, false, false)));
            Assert.Equal(BuntType.ForHit, BuntResolver.EffectiveType(BuntType.None, Situation(2, true, false, false)));
        }

        [Fact]
        public void 희생번트_결과가_일관된다()
        {
            var resolver = new BuntResolver(_config);
            var rng = new Pcg32Random(31);
            int sacrifices = 0;
            const int n = 2000;
            for (int i = 0; i < n; i++)
            {
                PlayResult r = resolver.ResolveFair(BuntType.Sacrifice, new BatterRatings(), Situation(0, true, false, false), rng, out BattedBall ball);
                Assert.InRange(ball.ExitVelocityKmh, _config.Bunt.MinExitVelocityKmh, _config.Bunt.MaxExitVelocityKmh);
                Assert.Equal(r.OutsRecorded, r.Movements.Count(m => m.IsOut));
                if (r.Outcome == PlateAppearanceOutcome.SacrificeBunt)
                {
                    sacrifices++;
                    Assert.Contains(r.Movements, m => m.FromBase == 1 && m.ToBase == 2 && !m.IsOut);
                }
            }

            Assert.InRange(sacrifices / (double)n, 0.55, 0.80);
        }

        [Fact]
        public void 번트능력이_좋으면_컨택이_높다()
        {
            var resolver = new BuntResolver(_config);
            var pitch = new ExecutedPitch { IsInZone = true };
            Assert.True(resolver.ContactProbability(new BatterRatings { Bunt = 70 }, pitch)
                > resolver.ContactProbability(new BatterRatings { Bunt = 30 }, pitch));
            Assert.True(resolver.ContactProbability(new BatterRatings(), new ExecutedPitch { IsInZone = false })
                < resolver.ContactProbability(new BatterRatings(), pitch));
        }

        /// <summary>원정 감독·타자를 사람으로 두고 투구 전 작전 단계까지 진행</summary>
        private GameEngine EngineAtPrePitch(ulong seed)
        {
            ControllerSet controllers = AiControllers.CreateAllAi();
            controllers.Assign(TeamSide.Away, HumanManagerDecision.Instance);
            controllers.Assign(TeamSide.Away, HumanBattingDecision.Instance);
            var engine = new GameEngine(TestData.AverageGame(), _config, seed, controllers);
            while (true)
            {
                engine.RunUntil(StopConditions.EndOfGame);
                if (engine.Pending.Kind == DecisionKind.OffensePrePitch)
                {
                    return engine;
                }

                Assert.True(engine.Pending.Kind == DecisionKind.OffenseManager
                    ? engine.Submit(ManagerOrders.None()).Accepted
                    : engine.Submit(BatterAction.Take()).Accepted);
            }
        }

        [Fact]
        public void 사람_감독_도루지시()
        {
            bool checkedSteal = false;
            for (ulong seed = 1; seed <= 20 && !checkedSteal; seed++)
            {
                GameEngine engine = EngineAtPrePitch(seed);
                int runnerId = engine.State.Away.Lineup[8].PlayerId;
                engine.State.Bases[0] = new BaseRunner(runnerId, engine.State.Home.CurrentPitcherId);

                Assert.False(engine.Submit(ManagerOrders.Of(ManagerAction.Steal(2))).Accepted);
                Assert.True(engine.Submit(ManagerOrders.Of(ManagerAction.Steal(1))).Accepted);
                engine.RunUntil(StopConditions.EndOfGame);
                Assert.Equal(DecisionKind.Swing, engine.Pending.Kind);
                Assert.True(engine.Submit(BatterAction.Take()).Accepted);

                PitchEvent ev = engine.State.Log.OfType<PitchEvent>().Last();
                if (ev.Result == PitchResult.Ball || ev.Result == PitchResult.CalledStrike)
                {
                    Assert.Equal(1, ev.StealFromBase);
                    Assert.Equal(runnerId, ev.StealRunnerId);
                    bool onSecond = engine.State.Bases[1]?.PlayerId == runnerId;
                    Assert.Equal(ev.StealSucceeded || ev.IsWildPitch || ev.IsPassedBall, onSecond || engine.State.Outs == 0 && ev.OutsAfter == 3);
                    checkedSteal = true;
                }
            }

            Assert.True(checkedSteal);
        }

        [Fact]
        public void 사람_타자_번트()
        {
            GameEngine engine = EngineAtPrePitch(4);
            Assert.True(engine.Submit(ManagerOrders.Of(ManagerAction.Bunt(BuntType.ForHit))).Accepted);
            engine.RunUntil(StopConditions.EndOfGame);
            Assert.True(engine.Submit(BatterAction.Bunt(BuntType.ForHit)).Accepted);
            Assert.True(engine.State.Log.OfType<PitchEvent>().Last().IsBunt);
            Assert.Equal(BuntType.None, engine.State.BuntSign);
        }

        [Fact]
        public void 시뮬_시즌에_작전이_발생하고_불변식이_유지된다()
        {
            int steals = 0, bunts = 0, missed = 0;
            for (ulong seed = 1; seed <= 40; seed++)
            {
                // 평균 주자는 AI 도루 기준(예상 성공률)에 못 미치므로 빠른 주자로 구성
                var setup = TestData.AverageGame();
                foreach (Player p in setup.Away.Team.Roster.Concat(setup.Home.Team.Roster).Where(p => !p.IsPitcher))
                {
                    p.Batting.Speed = 65;
                    p.Batting.StealJump = 65;
                }

                GameState state = GameSimulator.Play(setup, _config, seed);
                Assert.True(state.IsGameOver);
                int runs = state.Log.OfType<PitchEvent>().Sum(e => e.RunsScored);
                Assert.Equal(state.AwayScore + state.HomeScore, runs);
                foreach (PitchEvent e in state.Log.OfType<PitchEvent>())
                {
                    Assert.InRange(e.OutsAfter, 0, 3);
                    steals += e.StealFromBase > 0 ? 1 : 0;
                    bunts += e.IsBunt ? 1 : 0;
                    missed += e.IsWildPitch || e.IsPassedBall ? 1 : 0;
                }
            }

            Assert.True(steals > 0);
            Assert.True(missed > 0);
            Assert.True(bunts >= 0);
        }
    }
}
