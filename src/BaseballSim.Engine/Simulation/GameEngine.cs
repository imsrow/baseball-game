using System;
using System.Collections.Generic;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Fielding;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.State;
using BaseballSim.Engine.Teams;

namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// 경기 상태 머신.
    /// Step()은 현재 결정 지점의 담당 컨트롤러에게 결정을 받아 적용하고 다음 결정 지점에서 멈춘다.
    /// 담당자가 사람이면(Pending) 진행하지 않고 AwaitingInput을 돌려주며, UI는 Submit()으로 입력한다.
    /// </summary>
    public sealed class GameEngine
    {
        private readonly LeagueConfig _config;
        private readonly PlayerDirectory _players;
        private readonly PitchPipeline _pipeline;
        private readonly GameState _state;
        private readonly IEventSink _sink;
        private PendingDecision _pending;

        public GameEngine(GameSetup setup, LeagueConfig config, ulong seed, ControllerSet controllers, IEventSink sink = null)
            : this(GameStateFactory.Create(setup, config, seed), PlayerDirectory.FromSetup(setup), config, controllers, sink)
        {
        }

        public GameEngine(GameState state, PlayerDirectory players, LeagueConfig config, ControllerSet controllers,
            IEventSink sink = null)
        {
            _state = state;
            _players = players;
            _config = config;
            _pipeline = new PitchPipeline(config);
            _sink = sink;
            Controllers = controllers ?? throw new ArgumentNullException(nameof(controllers));
        }

        public GameState State => _state;

        public ControllerSet Controllers { get; }

        public LeagueConfig Config => _config;

        public PlayerDirectory Players => _players;

        public PitchPipeline Pipeline => _pipeline;

        /// <summary>사람 입력 대기 중인 결정 (없으면 null)</summary>
        public PendingDecision Pending => _pending;

        public bool IsGameOver => _state.IsGameOver;

        // ───────────────────────── 진행 ─────────────────────────

        /// <summary>
        /// 결정 지점 하나를 처리한다. 담당자가 사람이면 진행하지 않고 AwaitingInput.
        /// 입력 대기 중에 담당자를 AI로 바꾸고 Step()을 부르면 AI가 이어서 결정한다.
        /// </summary>
        public StepResult Step()
        {
            if (_state.IsGameOver)
            {
                _pending = null;
                return new StepResult(StepStatus.GameOver, null, 0);
            }

            int before = _state.Log.Count;
            DecisionKind kind;
            switch (_state.Phase)
            {
                case GamePhase.OffenseManager:
                {
                    kind = DecisionKind.OffenseManager;
                    TeamSide side = _state.OffenseSide;
                    ManagerContext context = BuildManagerContext(side);
                    Decision<ManagerOrders> decision = Controllers.Manager(side).DecideOffense(context);
                    if (!decision.IsReady)
                    {
                        return Await(kind, side, context);
                    }

                    ApplyAiOrThrow(ValidateOffenseOrders(decision.Value), () => ApplyOffenseOrders(decision.Value, false));
                    break;
                }

                case GamePhase.DefenseManager:
                {
                    kind = DecisionKind.DefenseManager;
                    TeamSide side = _state.DefenseSide;
                    ManagerContext context = BuildManagerContext(side);
                    Decision<ManagerOrders> decision = Controllers.Manager(side).DecideDefense(context);
                    if (!decision.IsReady)
                    {
                        return Await(kind, side, context);
                    }

                    ApplyAiOrThrow(ValidateDefenseOrders(decision.Value), () => ApplyDefenseOrders(decision.Value, false));
                    break;
                }

                case GamePhase.Pitch:
                {
                    kind = DecisionKind.Pitch;
                    TeamSide side = _state.DefenseSide;
                    PitchingContext context = BuildPitchingContext();
                    Decision<PitchCall> decision = Controllers.Pitching(side).DecidePitch(context);
                    if (!decision.IsReady)
                    {
                        return Await(kind, side, context);
                    }

                    ApplyAiOrThrow(ValidatePitchCall(decision.Value), () => ApplyPitch(decision.Value, false));
                    break;
                }

                case GamePhase.Swing:
                {
                    kind = DecisionKind.Swing;
                    TeamSide side = _state.OffenseSide;
                    BattingContext context = BuildBattingContext();
                    Decision<BatterAction> decision = Controllers.Batting(side).DecideSwing(context);
                    if (!decision.IsReady)
                    {
                        return Await(kind, side, context);
                    }

                    ApplyAiOrThrow(ValidateBatterAction(decision.Value), () => ApplySwing(decision.Value, false));
                    break;
                }

                default:
                    throw new InvalidOperationException("알 수 없는 단계: " + _state.Phase);
            }

            _pending = null;
            StepStatus status = _state.IsGameOver ? StepStatus.GameOver : StepStatus.Advanced;
            return new StepResult(status, kind, _state.Log.Count - before);
        }

        /// <summary>
        /// 멈춤 조건 충족, 사람 입력 필요, 경기 종료 중 하나가 될 때까지 진행한다.
        /// 조건은 결정 지점에 도착할 때마다 결정 직전에 평가한다.
        /// </summary>
        public RunResult RunUntil(IStopCondition stop)
        {
            int steps = 0;
            while (true)
            {
                if (_state.IsGameOver)
                {
                    return new RunResult(RunStatus.GameOver, steps);
                }

                if (stop.ShouldStop(this))
                {
                    return new RunResult(RunStatus.ConditionMet, steps);
                }

                StepResult result = Step();
                if (result.Status == StepStatus.AwaitingInput)
                {
                    return new RunResult(RunStatus.AwaitingInput, steps);
                }

                steps++;
            }
        }

        // ───────────────────────── 사람 입력 ─────────────────────────

        public SubmitResult Submit(PitchCall call)
        {
            string error = CheckPending(DecisionKind.Pitch) ?? ValidatePitchCall(call);
            if (error != null)
            {
                return SubmitResult.Reject(error);
            }

            ApplyPitch(call, true);
            _pending = null;
            return SubmitResult.Accept();
        }

        public SubmitResult Submit(BatterAction action)
        {
            string error = CheckPending(DecisionKind.Swing) ?? ValidateBatterAction(action);
            if (error != null)
            {
                return SubmitResult.Reject(error);
            }

            ApplySwing(action, true);
            _pending = null;
            return SubmitResult.Accept();
        }

        public SubmitResult Submit(ManagerOrders orders)
        {
            if (_pending == null
                || (_pending.Kind != DecisionKind.OffenseManager && _pending.Kind != DecisionKind.DefenseManager))
            {
                return SubmitResult.Reject("감독 결정 대기 중이 아닙니다.");
            }

            bool offense = _pending.Kind == DecisionKind.OffenseManager;
            string error = offense ? ValidateOffenseOrders(orders) : ValidateDefenseOrders(orders);
            if (error != null)
            {
                return SubmitResult.Reject(error);
            }

            if (offense)
            {
                ApplyOffenseOrders(orders, true);
            }
            else
            {
                ApplyDefenseOrders(orders, true);
            }

            _pending = null;
            return SubmitResult.Accept();
        }

        private string CheckPending(DecisionKind kind)
        {
            if (_pending == null || _pending.Kind != kind)
            {
                return kind + " 결정 대기 중이 아닙니다.";
            }

            return null;
        }

        private StepResult Await(DecisionKind kind, TeamSide side, object context)
        {
            _pending = new PendingDecision(kind, side, context);
            return new StepResult(StepStatus.AwaitingInput, null, 0);
        }

        private static void ApplyAiOrThrow(string error, Action apply)
        {
            if (error != null)
            {
                throw new InvalidOperationException("AI 결정이 규칙에 맞지 않습니다: " + error);
            }

            apply();
        }

        // ───────────────────────── 컨텍스트 ─────────────────────────

        private ManagerContext BuildManagerContext(TeamSide side)
        {
            return new ManagerContext
            {
                State = _state,
                Side = side,
                Players = _players,
                Config = _config,
                Random = _state.Random,
            };
        }

        private PitchingContext BuildPitchingContext()
        {
            Player pitcher = CurrentPitcher();
            Player batter = _players.Get(_state.CurrentBatterId);
            return new PitchingContext
            {
                State = _state,
                Side = _state.DefenseSide,
                Pitcher = pitcher,
                Batter = batter,
                BattingHand = batter.BattingHandAgainst(pitcher.Throws),
                Fatigue = CurrentFatigue(),
                Config = _config,
                Random = _state.Random,
            };
        }

        private BattingContext BuildBattingContext()
        {
            Player pitcher = CurrentPitcher();
            Player batter = _players.Get(_state.CurrentBatterId);
            Hand hand = batter.BattingHandAgainst(pitcher.Throws);
            PitchInFlight pitch = _state.CurrentPitch;
            return new BattingContext
            {
                State = _state,
                Side = _state.OffenseSide,
                Batter = batter,
                Pitcher = pitcher,
                BattingHand = hand,
                SameHand = hand == pitcher.Throws,
                PitchType = pitch.Executed.Type,
                VelocityKmh = pitch.Executed.VelocityKmh,
                EffectiveStuffZ = pitch.Executed.EffectiveStuffZ,
                Perceived = pitch.Perceived,
                Actual = pitch.Executed,
                Config = _config,
                Random = _state.Random,
            };
        }

        private Player CurrentPitcher()
        {
            return _players.Get(_state.Defense.CurrentPitcherId);
        }

        private double CurrentFatigue()
        {
            return FatigueModel.Fatigue(CurrentPitcher().Pitching, _state.Defense.CurrentPitcher.PitchCount, _config.Fatigue);
        }

        // ───────────────────────── 감독 결정 ─────────────────────────

        private string ValidateOffenseOrders(ManagerOrders orders)
        {
            if (orders == null)
            {
                return "지시가 없습니다.";
            }

            foreach (ManagerAction action in orders.Actions)
            {
                return "공격 측에서 할 수 없는 지시입니다: " + action.Type;
            }

            return null;
        }

        private void ApplyOffenseOrders(ManagerOrders orders, bool byHuman)
        {
            _state.Phase = GamePhase.DefenseManager;
        }

        private string ValidateDefenseOrders(ManagerOrders orders)
        {
            if (orders == null)
            {
                return "지시가 없습니다.";
            }

            TeamGameState team = _state.Defense;
            var incoming = new HashSet<int>();
            foreach (ManagerAction action in orders.Actions)
            {
                if (action.Type != ManagerActionType.PitchingChange)
                {
                    return "수비 측에서 할 수 없는 지시입니다: " + action.Type;
                }

                if (!team.AvailableBullpen.Contains(action.IncomingPlayerId) || !incoming.Add(action.IncomingPlayerId))
                {
                    return "등판할 수 없는 투수입니다: " + action.IncomingPlayerId;
                }
            }

            return null;
        }

        private void ApplyDefenseOrders(ManagerOrders orders, bool byHuman)
        {
            foreach (ManagerAction action in orders.Actions)
            {
                ChangePitcher(action.IncomingPlayerId, byHuman);
            }

            _state.Phase = GamePhase.Pitch;
        }

        private void ChangePitcher(int incomingId, bool byHuman)
        {
            TeamGameState team = _state.Defense;
            int outgoing = team.CurrentPitcherId;
            team.AvailableBullpen.Remove(incomingId);
            team.Removed.Add(outgoing);
            team.CurrentPitcherId = incomingId;
            team.Pitchers.Add(new PitcherGameState { PlayerId = incomingId, IsStarter = false });
            Emit(new SubstitutionEvent
            {
                Kind = SubstitutionKind.PitchingChange,
                Side = _state.DefenseSide,
                OutgoingPlayerId = outgoing,
                IncomingPlayerId = incomingId,
                Position = Position.Pitcher,
                OutsAtChange = _state.Outs,
                ByHuman = byHuman,
            });
        }

        // ───────────────────────── 투구 ─────────────────────────

        private string ValidatePitchCall(PitchCall call)
        {
            if (call == null)
            {
                return "투구 결정이 없습니다.";
            }

            if (CurrentPitcher().Pitching.Find(call.Type) == null)
            {
                return "레퍼토리에 없는 구종입니다: " + PitchTypeInfo.Abbreviation(call.Type);
            }

            if (double.IsNaN(call.Target.X) || double.IsNaN(call.Target.Z) || double.IsInfinity(call.Target.X)
                || double.IsInfinity(call.Target.Z))
            {
                return "목표 지점이 올바르지 않습니다.";
            }

            return null;
        }

        private void ApplyPitch(PitchCall call, bool byHuman)
        {
            Player batter = _players.Get(_state.CurrentBatterId);
            double fatigue = CurrentFatigue();
            _state.Defense.CurrentPitcher.PitchCount++;
            _state.CurrentPitch = _pipeline.Release(call, CurrentPitcher(), fatigue, batter, byHuman, _state.Random);
            _state.Phase = GamePhase.Swing;
        }

        // ───────────────────────── 스윙·판정 ─────────────────────────

        private static string ValidateBatterAction(BatterAction action)
        {
            return action == null ? "타자 결정이 없습니다." : null;
        }

        private void ApplySwing(BatterAction action, bool byHuman)
        {
            PitchInFlight pitch = _state.CurrentPitch;
            TeamGameState offense = _state.Offense;
            TeamGameState defense = _state.Defense;
            Player pitcher = CurrentPitcher();
            Player batter = _players.Get(_state.CurrentBatterId);
            Player catcher = _players.Get(defense.PlayerIdAt(Position.Catcher));
            Hand hand = batter.BattingHandAgainst(pitcher.Throws);
            bool sameHand = hand == pitcher.Throws;

            PitchEvent ev = CreatePitchEvent(pitch, batter, pitcher, catcher, hand, action, byHuman);
            PitchResolution resolution = _pipeline.Resolve(pitch, action, batter, hand, sameHand, _state.Strikes, catcher,
                () => BuildPlaySituation(batter, hand), _state.Random);
            ev.Result = resolution.Result;

            PlateAppearanceOutcome? outcome = null;
            switch (resolution.Result)
            {
                case PitchResult.Ball:
                    _state.Balls++;
                    if (_state.Balls >= _config.Rules.BallsForWalk)
                    {
                        outcome = PlateAppearanceOutcome.Walk;
                        ApplyRunnerMovements(ev, ForcedAdvances(batter.Id), false, false);
                    }

                    break;

                case PitchResult.CalledStrike:
                case PitchResult.SwingingStrike:
                    _state.Strikes++;
                    if (_state.Strikes >= _config.Rules.StrikesForStrikeout)
                    {
                        outcome = PlateAppearanceOutcome.Strikeout;
                        RecordOuts(1);
                    }

                    break;

                case PitchResult.Foul:
                    if (_state.Strikes < _config.Rules.StrikesForStrikeout - 1)
                    {
                        _state.Strikes++;
                    }

                    break;

                case PitchResult.HitByPitch:
                    outcome = PlateAppearanceOutcome.HitByPitch;
                    ApplyRunnerMovements(ev, ForcedAdvances(batter.Id), false, false);
                    break;

                case PitchResult.InPlay:
                {
                    PlayResult play = resolution.Play;
                    outcome = play.Outcome;
                    ev.BattedBall = ToBattedBallData(resolution, play);
                    ev.IsError = play.IsError;
                    if (play.IsError)
                    {
                        defense.Errors++;
                    }

                    RecordOuts(play.OutsRecorded);
                    ApplyRunnerMovements(ev, play.Movements, play.RunsNullified, play.Outcome == PlateAppearanceOutcome.HomeRun);
                    break;
                }
            }

            if (outcome.HasValue)
            {
                ev.PlateAppearanceOutcome = outcome.Value;
                if (PlateAppearanceOutcomeInfo.IsHit(outcome.Value))
                {
                    offense.Hits++;
                }

                bool rbiEligible = outcome.Value != PlateAppearanceOutcome.ReachedOnError
                    && outcome.Value != PlateAppearanceOutcome.GroundedIntoDoublePlay;
                ev.RunsBattedIn = rbiEligible ? ev.RunsScored : 0;
            }

            ev.OutsAfter = Math.Min(_state.Outs, _config.Rules.OutsPerHalfInning);
            ev.AwayScoreAfter = _state.AwayScore;
            ev.HomeScoreAfter = _state.HomeScore;
            _state.CurrentPitch = null;
            Emit(ev);

            if (outcome.HasValue)
            {
                EndPlateAppearance();
            }
            else
            {
                _state.PitchNumberInPlateAppearance++;
                _state.Phase = GamePhase.Pitch;
            }
        }

        private PitchEvent CreatePitchEvent(PitchInFlight pitch, Player batter, Player pitcher, Player catcher, Hand hand,
            BatterAction action, bool swingByHuman)
        {
            ExecutedPitch executed = pitch.Executed;
            return new PitchEvent
            {
                PlateAppearanceNumber = _state.PlateAppearanceNumber,
                PitchNumberInPlateAppearance = _state.PitchNumberInPlateAppearance,
                BatterId = batter.Id,
                PitcherId = pitcher.Id,
                CatcherId = catcher.Id,
                BattingHand = hand,
                PitcherHand = pitcher.Throws,
                BallsBefore = _state.Balls,
                StrikesBefore = _state.Strikes,
                OutsBefore = _state.Outs,
                RunnerOnFirst = _state.Bases[0]?.PlayerId ?? -1,
                RunnerOnSecond = _state.Bases[1]?.PlayerId ?? -1,
                RunnerOnThird = _state.Bases[2]?.PlayerId ?? -1,
                AwayScoreBefore = _state.AwayScore,
                HomeScoreBefore = _state.HomeScore,
                PitchType = executed.Type,
                VelocityKmh = executed.VelocityKmh,
                TargetX = executed.Target.X,
                TargetZ = executed.Target.Z,
                PlateX = executed.Actual.X,
                PlateZ = executed.Actual.Z,
                Region = executed.Region,
                IsInZone = executed.IsInZone,
                PerceivedX = pitch.Perceived.Location.X,
                PerceivedZ = pitch.Perceived.Location.Z,
                PitchByHuman = pitch.ByHuman,
                SwingByHuman = swingByHuman,
                Swung = action.Type == BatterActionType.Swing,
            };
        }

        private static BattedBallData ToBattedBallData(PitchResolution resolution, PlayResult play)
        {
            return new BattedBallData
            {
                ExitVelocityKmh = resolution.BattedBall.ExitVelocityKmh,
                LaunchAngleDeg = resolution.BattedBall.LaunchAngleDeg,
                SprayAngleDeg = resolution.BattedBall.SprayAngleDeg,
                Type = resolution.BattedBall.Type,
                IsSolid = resolution.BattedBall.IsSolid,
                EndX = play.BallEndPoint.X,
                EndY = play.BallEndPoint.Y,
                HangTimeS = play.HangTimeS,
                DistanceM = play.Flight != null ? play.Flight.DistanceM : play.BallEndPoint.DistanceFromHome,
                FieldedBy = play.FieldedBy,
            };
        }

        private PlaySituation BuildPlaySituation(Player batter, Hand battingHand)
        {
            TeamGameState defense = _state.Defense;
            var situation = new PlaySituation
            {
                OutsBefore = _state.Outs,
                Batter = new RunnerProfile(batter.Id, batter.Batting, battingHand),
                Defense = DefensiveAlignment.Build(p => _players.Get(defense.PlayerIdAt(p)), _pipeline.Field, _config),
            };
            for (int b = 0; b < 3; b++)
            {
                BaseRunner runner = _state.Bases[b];
                if (runner != null)
                {
                    Player p = _players.Get(runner.PlayerId);
                    situation.Runners[b] = new RunnerProfile(p.Id, p.Batting, Hand.Right);
                }
            }

            return situation;
        }

        /// <summary>볼넷·몸맞는공: 타자 1루, 밀려나는 주자만 진루</summary>
        private List<RunnerMovement> ForcedAdvances(int batterId)
        {
            var moves = new List<RunnerMovement>();
            for (int b = 3; b >= 1; b--)
            {
                BaseRunner runner = _state.Bases[b - 1];
                if (runner == null)
                {
                    continue;
                }

                bool isForced = true;
                for (int k = 1; k < b; k++)
                {
                    isForced &= _state.Bases[k - 1] != null;
                }

                moves.Add(new RunnerMovement(runner.PlayerId, b, isForced ? b + 1 : b, false));
            }

            moves.Add(new RunnerMovement(batterId, 0, 1, false));
            return moves;
        }

        private void RecordOuts(int outs)
        {
            _state.Outs += outs;
            _state.Defense.CurrentPitcher.OutsRecorded += outs;
        }

        /// <summary>
        /// 주자 이동을 루 상태와 점수에 반영. 9회 이후 말 공격 끝내기는 홈런이 아니면 필요한 점수까지만 인정한다.
        /// </summary>
        private void ApplyRunnerMovements(PitchEvent ev, List<RunnerMovement> moves, bool nullified, bool isHomeRun)
        {
            var newBases = new BaseRunner[3];
            var existing = new Dictionary<int, BaseRunner>();
            var moved = new HashSet<int>();
            for (int b = 0; b < 3; b++)
            {
                if (_state.Bases[b] != null)
                {
                    existing[_state.Bases[b].PlayerId] = _state.Bases[b];
                }
            }

            int maxRuns = int.MaxValue;
            if (!_state.IsTopHalf && _state.Inning >= _config.Rules.InningsPerGame && !isHomeRun
                && _state.HomeScore <= _state.AwayScore)
            {
                maxRuns = _state.AwayScore + 1 - _state.HomeScore;
            }

            int runs = 0;
            TeamGameState offense = _state.Offense;
            foreach (RunnerMovement move in moves)
            {
                moved.Add(move.PlayerId);
                if (move.IsOut || move.ToBase == 0)
                {
                    continue;
                }

                BaseRunner runner = move.FromBase == 0
                    ? new BaseRunner(move.PlayerId, _state.Defense.CurrentPitcherId)
                    : existing[move.PlayerId];

                if (move.ToBase == 4)
                {
                    if (nullified)
                    {
                        continue;
                    }

                    if (runs >= maxRuns)
                    {
                        // 끝내기로 경기가 이미 끝났으므로 득점으로 인정하지 않는다
                        move.ToBase = 3;
                        continue;
                    }

                    runs++;
                    offense.Runs++;
                    PitcherGameState responsible = _state.Defense.FindPitcher(runner.ResponsiblePitcherId);
                    if (responsible != null)
                    {
                        responsible.RunsAllowed++;
                    }

                    continue;
                }

                if (newBases[move.ToBase - 1] != null)
                {
                    throw new InvalidOperationException("주자 이동 충돌: " + move.ToBase + "루에 두 명");
                }

                newBases[move.ToBase - 1] = runner;
            }

            // 이동 목록에 없는 주자는 제자리
            for (int b = 0; b < 3; b++)
            {
                BaseRunner runner = _state.Bases[b];
                if (runner != null && !moved.Contains(runner.PlayerId))
                {
                    if (newBases[b] != null)
                    {
                        throw new InvalidOperationException("주자 이동 충돌: " + (b + 1) + "루");
                    }

                    newBases[b] = runner;
                }
            }

            _state.Bases = newBases;
            ev.RunnerMovements = moves;
            ev.RunsNullified = nullified;
            ev.RunsScored = runs;
        }

        // ───────────────────────── 타석·이닝·경기 종료 ─────────────────────────

        private void EndPlateAppearance()
        {
            TeamGameState offense = _state.Offense;
            offense.NextBatterIndex = (offense.NextBatterIndex + 1) % offense.Lineup.Count;
            _state.Defense.CurrentPitcher.BattersFaced++;
            _state.Balls = 0;
            _state.Strikes = 0;
            _state.PlateAppearanceNumber++;
            _state.PitchNumberInPlateAppearance = 1;

            if (_state.Outs >= _config.Rules.OutsPerHalfInning)
            {
                EndHalfInning();
                return;
            }

            if (IsWalkOff())
            {
                _state.Phase = GamePhase.GameOver;
                return;
            }

            _state.Phase = GamePhase.OffenseManager;
        }

        private bool IsWalkOff()
        {
            return !_state.IsTopHalf && _state.Inning >= _config.Rules.InningsPerGame && _state.HomeScore > _state.AwayScore;
        }

        private void EndHalfInning()
        {
            RulesConfig rules = _config.Rules;
            _state.Outs = 0;
            _state.Bases = new BaseRunner[3];

            if (_state.IsTopHalf)
            {
                if (_state.Inning >= rules.InningsPerGame && _state.HomeScore > _state.AwayScore)
                {
                    _state.Phase = GamePhase.GameOver;
                    return;
                }

                _state.IsTopHalf = false;
            }
            else
            {
                bool decided = _state.Inning >= rules.InningsPerGame && _state.HomeScore != _state.AwayScore;
                bool limit = rules.MaxInnings > 0 && _state.Inning >= rules.MaxInnings;
                if (decided || limit)
                {
                    _state.Phase = GamePhase.GameOver;
                    return;
                }

                _state.Inning++;
                _state.IsTopHalf = true;
            }

            if (rules.UseExtraInningRunner && _state.Inning > rules.InningsPerGame)
            {
                // 승부치기: 직전 타순 타자가 2루에서 시작
                TeamGameState offense = _state.Offense;
                int index = (offense.NextBatterIndex - 1 + offense.Lineup.Count) % offense.Lineup.Count;
                _state.Bases[1] = new BaseRunner(offense.Lineup[index].PlayerId, _state.Defense.CurrentPitcherId);
            }

            _state.Phase = GamePhase.OffenseManager;
        }

        private void Emit(GameEvent gameEvent)
        {
            gameEvent.GameId = _state.GameId;
            gameEvent.Sequence = ++_state.EventSequence;
            gameEvent.Inning = _state.Inning;
            gameEvent.IsTopHalf = _state.IsTopHalf;
            _state.Log.Add(gameEvent);
            _sink?.OnEvent(gameEvent);
        }
    }
}
