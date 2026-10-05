using System;
using System.Collections.Generic;
using BaseballSim.Engine.AI;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Fielding;
using BaseballSim.Engine.Persistence;
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
            TeamNames[setup.Away.Team.Id] = setup.Away.Team.Name;
            TeamNames[setup.Home.Team.Id] = setup.Home.Team.Name;
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

        /// <summary>팀 ID → 팀 이름 (표시·저장용)</summary>
        public Dictionary<int, string> TeamNames { get; } = new Dictionary<int, string>();

        // ───────────────────────── 저장 / 불러오기 ─────────────────────────

        /// <summary>현재 경기 전체를 저장 데이터로 (어느 결정 지점에서든 가능)</summary>
        public byte[] Save()
        {
            return GameSaveSerializer.Save(this);
        }

        /// <summary>
        /// 저장 데이터로 경기를 이어서 진행할 엔진을 만든다.
        /// controllers를 주지 않으면 저장 당시 담당 방식(사람/AI)대로 새 컨트롤러를 연결한다.
        /// </summary>
        public static GameEngine Load(byte[] data, LeagueConfig config, out SavedGame saved, ControllerSet controllers = null,
            IEventSink sink = null)
        {
            saved = GameSaveSerializer.Load(data, config);
            var engine = new GameEngine(saved.State, saved.Players, config, controllers ?? ControllersFor(saved.Modes), sink);
            foreach (KeyValuePair<int, string> pair in saved.TeamNames)
            {
                engine.TeamNames[pair.Key] = pair.Value;
            }

            return engine;
        }

        private static ControllerSet ControllersFor(ControlModes modes)
        {
            ControllerSet set = AiControllers.CreateAllAi();
            foreach (TeamSide side in new[] { TeamSide.Away, TeamSide.Home })
            {
                if (modes.IsHuman(side, DecisionRole.Pitching))
                {
                    set.Assign(side, HumanPitchingDecision.Instance);
                }

                if (modes.IsHuman(side, DecisionRole.Batting))
                {
                    set.Assign(side, HumanBattingDecision.Instance);
                }

                if (modes.IsHuman(side, DecisionRole.Manager))
                {
                    set.Assign(side, new HumanManagerDecision
                    {
                        DelegateToAi = modes.ManagerDelegatedToAi[(int)side],
                        HumanOffenseTactics = modes.HumanOffenseTactics[(int)side],
                        HumanDefenseTactics = modes.HumanDefenseTactics[(int)side],
                        SubstitutionsByAi = modes.SubstitutionsByAi[(int)side],
                    });
                }
            }

            return set;
        }

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
                    if (_state.Offense.Bench.Count == 0)
                    {
                        // 대타·대주자로 쓸 선수가 없으면 결정 지점 생략
                        ApplyOffenseOrders(ManagerOrders.None(), false);
                        break;
                    }

                    ManagerContext context = BuildManagerContext(side);
                    Decision<ManagerOrders> decision = Controllers.Manager(side).DecideOffense(context);
                    if (!decision.IsReady)
                    {
                        return Await(kind, side, context);
                    }

                    ManagerOrders orders = ValidOrFallback(side, ValidateOffenseOrders(decision.Value), decision.Value);
                    ApplyOffenseOrders(orders, false);
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

                    ManagerOrders orders = ValidOrFallback(side, ValidateDefenseOrders(decision.Value), decision.Value);
                    ApplyDefenseOrders(orders, false);
                    break;
                }

                case GamePhase.OffensePrePitch:
                {
                    kind = DecisionKind.OffensePrePitch;
                    TeamSide side = _state.OffenseSide;
                    if (!HasPrePitchOptions())
                    {
                        // 도루할 주자도 없고 번트할 카운트도 아니면 결정 지점 생략
                        ApplyPrePitchOrders(ManagerOrders.None());
                        break;
                    }

                    ManagerContext context = BuildManagerContext(side);
                    Decision<ManagerOrders> decision = Controllers.Manager(side).DecidePrePitch(context);
                    if (!decision.IsReady)
                    {
                        return Await(kind, side, context);
                    }

                    ApplyPrePitchOrders(ValidOrFallback(side, ValidatePrePitchOrders(decision.Value), decision.Value));
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

        /// <summary>
        /// 사람 담당 역할도 이 구간만 AI가 대행해 멈춤 조건까지 진행한다. 끝나면 원래 담당자로 되돌린다.
        /// 입력 대기 중에 호출하면 그 결정부터 AI가 이어받는다.
        /// </summary>
        public RunResult SimulateUntil(IStopCondition stop)
        {
            ControllerSet original = Controllers.Clone();
            foreach (TeamSide side in new[] { TeamSide.Away, TeamSide.Home })
            {
                foreach (DecisionRole role in new[] { DecisionRole.Pitching, DecisionRole.Batting, DecisionRole.Manager })
                {
                    if (Controllers.IsHuman(side, role))
                    {
                        AiControllers.AssignAi(Controllers, side, role);
                    }
                }
            }

            try
            {
                _pending = null;
                return RunUntil(stop);
            }
            finally
            {
                Controllers.CopyFrom(original);
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
            if (_pending == null)
            {
                return SubmitResult.Reject("감독 결정 대기 중이 아닙니다.");
            }

            string error;
            Action apply;
            switch (_pending.Kind)
            {
                case DecisionKind.OffenseManager:
                    error = ValidateOffenseOrders(orders);
                    apply = () => ApplyOffenseOrders(orders, true);
                    break;
                case DecisionKind.DefenseManager:
                    error = ValidateDefenseOrders(orders);
                    apply = () => ApplyDefenseOrders(orders, true);
                    break;
                case DecisionKind.OffensePrePitch:
                    error = ValidatePrePitchOrders(orders);
                    apply = () => ApplyPrePitchOrders(orders);
                    break;
                default:
                    return SubmitResult.Reject("감독 결정 대기 중이 아닙니다.");
            }

            if (error != null)
            {
                return SubmitResult.Reject(error);
            }

            ManagerContext asked = _pending.ManagerContext;
            if (orders.IsEmpty && Controllers.Manager(_pending.Side) is HumanManagerDecision human)
            {
                human.OnSuggestionDeclined(asked);
            }

            apply();
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

        /// <summary>
        /// 감독 지시 검증. 사람 감독이 미리 걸어둔 지시가 지금 상황에 맞지 않으면 "작전 없음"으로 진행하고,
        /// AI 지시가 규칙에 어긋나면 버그이므로 예외를 던진다.
        /// </summary>
        private ManagerOrders ValidOrFallback(TeamSide side, string error, ManagerOrders orders)
        {
            if (error == null)
            {
                return orders;
            }

            if (Controllers.IsHuman(side, DecisionRole.Manager))
            {
                return ManagerOrders.None();
            }

            throw new InvalidOperationException("AI 결정이 규칙에 맞지 않습니다: " + error);
        }

        /// <summary>투구 전 작전이 의미 있는지: 도루 가능한 주자 또는 번트 가능한 카운트</summary>
        private bool HasPrePitchOptions()
        {
            bool canSteal = (_state.Bases[0] != null && _state.Bases[1] == null)
                || (_state.Bases[1] != null && _state.Bases[2] == null);
            bool canBunt = _state.Strikes < _config.Rules.StrikesForStrikeout - 1;
            return canSteal || canBunt;
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

            TeamGameState team = _state.Offense;
            var incoming = new HashSet<int>();
            var replacedBases = new HashSet<int>();
            bool pinchHit = false;
            foreach (ManagerAction action in orders.Actions)
            {
                switch (action.Type)
                {
                    case ManagerActionType.PinchHitter:
                        if (pinchHit)
                        {
                            return "대타는 한 명만 지시할 수 있습니다.";
                        }

                        if (action.OutgoingPlayerId != _state.CurrentBatterId)
                        {
                            return "대타는 현재 타자 대신 들어갑니다.";
                        }

                        pinchHit = true;
                        break;
                    case ManagerActionType.PinchRunner:
                        if (action.FromBase < 1 || action.FromBase > 3 || _state.Bases[action.FromBase - 1] == null)
                        {
                            return "대주자를 넣을 주자가 없습니다.";
                        }

                        if (!replacedBases.Add(action.FromBase))
                        {
                            return "같은 주자를 두 번 교체할 수 없습니다.";
                        }

                        if (team.LineupIndexOf(_state.Bases[action.FromBase - 1].PlayerId) < 0)
                        {
                            return "라인업에 없는 주자입니다.";
                        }

                        break;
                    default:
                        return "타석 시작 공격 측에서 할 수 없는 지시입니다: " + action.Type;
                }

                if (!team.Bench.Contains(action.IncomingPlayerId) || !incoming.Add(action.IncomingPlayerId))
                {
                    return "출전할 수 없는 벤치 선수입니다: " + action.IncomingPlayerId;
                }
            }

            return null;
        }

        private void ApplyOffenseOrders(ManagerOrders orders, bool byHuman)
        {
            TeamGameState team = _state.Offense;
            foreach (ManagerAction action in orders.Actions)
            {
                if (action.Type == ManagerActionType.PinchHitter)
                {
                    Substitute(team, _state.OffenseSide, action.OutgoingPlayerId, action.IncomingPlayerId,
                        SubstitutionKind.PinchHitter, byHuman);
                }
                else if (action.Type == ManagerActionType.PinchRunner)
                {
                    BaseRunner runner = _state.Bases[action.FromBase - 1];
                    int outgoing = runner.PlayerId;
                    Substitute(team, _state.OffenseSide, outgoing, action.IncomingPlayerId, SubstitutionKind.PinchRunner, byHuman);
                    runner.PlayerId = action.IncomingPlayerId;
                }
            }

            _state.Phase = GamePhase.DefenseManager;
        }

        /// <summary>라인업 자리 교체 (포지션은 그 자리를 그대로 이어받는다)</summary>
        private void Substitute(TeamGameState team, TeamSide side, int outgoing, int incoming, SubstitutionKind kind, bool byHuman)
        {
            int index = team.LineupIndexOf(outgoing);
            LineupSlot slot = team.Lineup[index];
            slot.PlayerId = incoming;
            team.Bench.Remove(incoming);
            team.Removed.Add(outgoing);
            Emit(new SubstitutionEvent
            {
                Kind = kind,
                Side = side,
                OutgoingPlayerId = outgoing,
                IncomingPlayerId = incoming,
                Position = slot.Position,
                OutsAtChange = _state.Outs,
                ByHuman = byHuman,
            });
        }

        private string ValidateDefenseOrders(ManagerOrders orders)
        {
            if (orders == null)
            {
                return "지시가 없습니다.";
            }

            TeamGameState team = _state.Defense;
            var incoming = new HashSet<int>();
            var outgoing = new HashSet<int>();
            bool pitchingChange = false;
            bool intentionalWalk = false;
            foreach (ManagerAction action in orders.Actions)
            {
                switch (action.Type)
                {
                    case ManagerActionType.PitchingChange:
                        if (pitchingChange)
                        {
                            return "투수 교체는 한 번만 지시할 수 있습니다.";
                        }

                        if (!team.AvailableBullpen.Contains(action.IncomingPlayerId))
                        {
                            return "등판할 수 없는 투수입니다: " + action.IncomingPlayerId;
                        }

                        pitchingChange = true;
                        break;
                    case ManagerActionType.DefensiveSubstitution:
                        if (team.LineupIndexOf(action.OutgoingPlayerId) < 0 || !outgoing.Add(action.OutgoingPlayerId))
                        {
                            return "교체할 수 없는 선수입니다: " + action.OutgoingPlayerId;
                        }

                        if (!team.Bench.Contains(action.IncomingPlayerId) || !incoming.Add(action.IncomingPlayerId))
                        {
                            return "출전할 수 없는 벤치 선수입니다: " + action.IncomingPlayerId;
                        }

                        break;
                    case ManagerActionType.IntentionalWalk:
                        if (intentionalWalk)
                        {
                            return "고의4구는 한 번만 지시할 수 있습니다.";
                        }

                        intentionalWalk = true;
                        break;
                    default:
                        return "수비 측에서 할 수 없는 지시입니다: " + action.Type;
                }
            }

            return null;
        }

        private void ApplyDefenseOrders(ManagerOrders orders, bool byHuman)
        {
            TeamGameState team = _state.Defense;
            bool intentionalWalk = false;
            foreach (ManagerAction action in orders.Actions)
            {
                switch (action.Type)
                {
                    case ManagerActionType.PitchingChange:
                        ChangePitcher(action.IncomingPlayerId, byHuman);
                        break;
                    case ManagerActionType.DefensiveSubstitution:
                        Substitute(team, _state.DefenseSide, action.OutgoingPlayerId, action.IncomingPlayerId,
                            SubstitutionKind.DefensiveSubstitution, byHuman);
                        break;
                    case ManagerActionType.IntentionalWalk:
                        intentionalWalk = true;
                        break;
                }
            }

            if (intentionalWalk)
            {
                ApplyIntentionalWalk(byHuman);
                return;
            }

            _state.Phase = GamePhase.OffensePrePitch;
        }

        /// <summary>고의4구: 투구 없이 타자 1루, 밀려나는 주자만 진루</summary>
        private void ApplyIntentionalWalk(bool byHuman)
        {
            int batterId = _state.CurrentBatterId;
            var ev = new IntentionalWalkEvent
            {
                PlateAppearanceNumber = _state.PlateAppearanceNumber,
                BatterId = batterId,
                PitcherId = _state.Defense.CurrentPitcherId,
                OutsBefore = _state.Outs,
                ByHuman = byHuman,
            };
            List<RunnerMovement> moves = ForcedAdvances(batterId);
            ev.RunsScored = MoveRunners(moves, false, false);
            ev.RunnerMovements = moves;
            ev.AwayScoreAfter = _state.AwayScore;
            ev.HomeScoreAfter = _state.HomeScore;
            Emit(ev);
            EndPlateAppearance();
        }

        private string ValidatePrePitchOrders(ManagerOrders orders)
        {
            if (orders == null)
            {
                return "지시가 없습니다.";
            }

            bool steal = false;
            bool bunt = false;
            foreach (ManagerAction action in orders.Actions)
            {
                switch (action.Type)
                {
                    case ManagerActionType.StealAttempt:
                        if (steal)
                        {
                            return "도루는 한 명만 지시할 수 있습니다.";
                        }

                        if (action.FromBase != 1 && action.FromBase != 2)
                        {
                            return "1루 또는 2루 주자만 도루할 수 있습니다.";
                        }

                        if (_state.Bases[action.FromBase - 1] == null)
                        {
                            return action.FromBase + "루에 주자가 없습니다.";
                        }

                        if (_state.Bases[action.FromBase] != null)
                        {
                            return (action.FromBase + 1) + "루가 비어 있지 않습니다.";
                        }

                        steal = true;
                        break;
                    case ManagerActionType.BuntSign:
                        if (bunt || action.BuntType == BuntType.None)
                        {
                            return "번트 사인이 올바르지 않습니다.";
                        }

                        bunt = true;
                        break;
                    default:
                        return "투구 전에 할 수 없는 지시입니다: " + action.Type;
                }
            }

            return null;
        }

        private void ApplyPrePitchOrders(ManagerOrders orders)
        {
            _state.StealFromBase = 0;
            _state.BuntSign = BuntType.None;
            foreach (ManagerAction action in orders.Actions)
            {
                if (action.Type == ManagerActionType.StealAttempt)
                {
                    _state.StealFromBase = action.FromBase;
                }
                else if (action.Type == ManagerActionType.BuntSign)
                {
                    _state.BuntSign = action.BuntType;
                }
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
            RulesConfig rules = _config.Rules;
            Player pitcher = CurrentPitcher();
            Player batter = _players.Get(_state.CurrentBatterId);
            Player catcher = _players.Get(defense.PlayerIdAt(Position.Catcher));
            Hand hand = batter.BattingHandAgainst(pitcher.Throws);
            bool sameHand = hand == pitcher.Throws;
            bool firstOccupied = _state.Bases[0] != null;
            int outsBefore = _state.Outs;

            PitchEvent ev = CreatePitchEvent(pitch, batter, pitcher, catcher, hand, action, byHuman);
            PitchResolution resolution = _pipeline.Resolve(pitch, action, batter, hand, sameHand, _state.Strikes, catcher,
                () => BuildPlaySituation(batter, hand), _state.Random);
            ev.Result = resolution.Result;
            ev.IsBunt = resolution.IsBunt;

            PlateAppearanceOutcome? outcome = null;
            List<RunnerMovement> moves = null;
            switch (resolution.Result)
            {
                case PitchResult.Ball:
                    _state.Balls++;
                    if (_state.Balls >= rules.BallsForWalk)
                    {
                        outcome = PlateAppearanceOutcome.Walk;
                        moves = ForcedAdvances(batter.Id);
                    }

                    break;

                case PitchResult.CalledStrike:
                case PitchResult.SwingingStrike:
                    _state.Strikes++;
                    if (_state.Strikes >= rules.StrikesForStrikeout)
                    {
                        outcome = PlateAppearanceOutcome.Strikeout;
                    }

                    break;

                case PitchResult.Foul:
                    if (_state.Strikes < rules.StrikesForStrikeout - 1)
                    {
                        _state.Strikes++;
                    }
                    else if (resolution.IsBunt)
                    {
                        // 2스트라이크 번트 파울은 삼진
                        _state.Strikes++;
                        outcome = PlateAppearanceOutcome.Strikeout;
                    }

                    break;

                case PitchResult.HitByPitch:
                    outcome = PlateAppearanceOutcome.HitByPitch;
                    moves = ForcedAdvances(batter.Id);
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

            if (resolution.Result != PitchResult.InPlay)
            {
                moves = ResolveNonContactBaserunning(ev, resolution, outcome, moves, batter, pitcher, catcher,
                    firstOccupied, outsBefore);
                if (moves != null)
                {
                    ApplyRunnerMovements(ev, moves, false, false);
                }
            }

            if (outcome.HasValue)
            {
                ev.PlateAppearanceOutcome = outcome.Value;
                if (PlateAppearanceOutcomeInfo.IsHit(outcome.Value))
                {
                    offense.Hits++;
                }

                ev.RunsBattedIn = IsRbiEligible(outcome.Value) && !ev.IsWildPitch && !ev.IsPassedBall ? ev.RunsScored : 0;
            }

            ev.OutsAfter = Math.Min(_state.Outs, rules.OutsPerHalfInning);
            ev.AwayScoreAfter = _state.AwayScore;
            ev.HomeScoreAfter = _state.HomeScore;
            _state.CurrentPitch = null;
            _state.StealFromBase = 0;
            _state.BuntSign = BuntType.None;
            Emit(ev);

            if (outcome.HasValue)
            {
                EndPlateAppearance();
            }
            else if (_state.Outs >= rules.OutsPerHalfInning)
            {
                // 타석 도중 도루 실패로 이닝 종료: 같은 타자가 다음 이닝에 새 타석
                ResetCountForNewPlateAppearance();
                EndHalfInning();
            }
            else if (IsWalkOff())
            {
                _state.Phase = GamePhase.GameOver;
            }
            else
            {
                _state.PitchNumberInPlateAppearance++;
                _state.Phase = GamePhase.OffensePrePitch;
            }
        }

        /// <summary>
        /// 공이 인플레이되지 않은 투구의 주루: 폭투·포일(낫아웃 포함), 도루.
        /// 삼진 아웃 기록도 여기서 한다 (낫아웃 출루면 아웃이 아니다).
        /// 반환값은 적용할 주자 이동 (없으면 null 또는 전달받은 이동 그대로)
        /// </summary>
        private List<RunnerMovement> ResolveNonContactBaserunning(PitchEvent ev, PitchResolution resolution,
            PlateAppearanceOutcome? outcome, List<RunnerMovement> moves, Player batter, Player pitcher, Player catcher,
            bool firstOccupied, int outsBefore)
        {
            RulesConfig rules = _config.Rules;
            bool strikeout = outcome == PlateAppearanceOutcome.Strikeout;
            bool runnersOn = _state.Bases[0] != null || _state.Bases[1] != null || _state.Bases[2] != null;
            bool missEligible = (resolution.Result == PitchResult.Ball && outcome == null)
                || resolution.Result == PitchResult.CalledStrike || resolution.Result == PitchResult.SwingingStrike;

            // 포수가 공을 놓침
            bool missed = false;
            bool batterReached = false;
            if (missEligible && (runnersOn || strikeout))
            {
                PlateLocation location = _state.CurrentPitch.Executed.Actual;
                if (_state.Random.NextDouble() < _pipeline.PassedBalls.MissProbability(location, catcher))
                {
                    missed = true;
                    bool wild = _pipeline.PassedBalls.IsWildLocation(location);
                    ev.IsWildPitch = wild;
                    ev.IsPassedBall = !wild;
                    moves = AllRunnersAdvance();
                    if (strikeout && PassedBallModel.BatterMayRunOnDroppedThirdStrike(firstOccupied, outsBefore, rules.OutsPerHalfInning))
                    {
                        ev.DroppedThirdStrike = true;
                        batterReached = _state.Random.NextDouble()
                            < _pipeline.PassedBalls.DroppedThirdStrikeReachProbability(batter.Batting);
                        ev.BatterReachedOnDroppedThirdStrike = batterReached;
                        if (batterReached)
                        {
                            moves.Add(new RunnerMovement(batter.Id, 0, 1, false));
                        }
                    }
                }
            }

            if (strikeout && !batterReached)
            {
                RecordOuts(1);
            }

            // 도루
            int from = _state.StealFromBase;
            if (from > 0 && _state.Bases[from - 1] != null)
            {
                BaseRunner runner = _state.Bases[from - 1];
                bool stealCounts = missEligible && outcome != PlateAppearanceOutcome.Walk;
                if (missed && stealCounts)
                {
                    ev.StealFromBase = from;
                    ev.StealRunnerId = runner.PlayerId;
                    ev.StealSucceeded = true;
                }
                else if (stealCounts && _state.Outs < rules.OutsPerHalfInning)
                {
                    Player runnerPlayer = _players.Get(runner.PlayerId);
                    double success = _pipeline.Steals.SuccessProbability(runnerPlayer.Batting, from + 1, pitcher.Pitching,
                        catcher, _state.CurrentPitch.Executed.Type);
                    bool safe = _state.Random.NextDouble() < success;
                    ev.StealFromBase = from;
                    ev.StealRunnerId = runner.PlayerId;
                    ev.StealSucceeded = safe;
                    moves = moves ?? new List<RunnerMovement>();
                    moves.Add(new RunnerMovement(runner.PlayerId, from, from + 1, !safe));
                    if (!safe)
                    {
                        RecordOuts(1);
                    }
                }
            }

            return moves;
        }

        /// <summary>폭투·포일: 모든 주자 한 베이스 진루</summary>
        private List<RunnerMovement> AllRunnersAdvance()
        {
            var moves = new List<RunnerMovement>();
            for (int b = 3; b >= 1; b--)
            {
                BaseRunner runner = _state.Bases[b - 1];
                if (runner != null)
                {
                    moves.Add(new RunnerMovement(runner.PlayerId, b, b + 1, false));
                }
            }

            return moves;
        }

        private static bool IsRbiEligible(PlateAppearanceOutcome outcome)
        {
            return outcome != PlateAppearanceOutcome.ReachedOnError
                && outcome != PlateAppearanceOutcome.GroundedIntoDoublePlay
                && outcome != PlateAppearanceOutcome.Strikeout;
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
                HasLanding = play.LandingPoint.HasValue,
                LandingX = play.LandingPoint?.X ?? 0,
                LandingY = play.LandingPoint?.Y ?? 0,
                LandingTimeS = play.LandingTimeS,
                FieldedTimeS = play.FieldedTimeS,
                FielderArrivalS = play.FielderArrivalS,
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
                BaserunningStyle = _state.Offense.BaserunningStyle,
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
            int runs = MoveRunners(moves, nullified, isHomeRun);
            ev.RunnerMovements = moves;
            ev.RunsNullified = nullified;
            ev.RunsScored = runs;
        }

        /// <summary>주자 이동을 루 상태와 점수에 반영하고 인정된 득점 수를 돌려준다</summary>
        private int MoveRunners(List<RunnerMovement> moves, bool nullified, bool isHomeRun)
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
            return runs;
        }

        // ───────────────────────── 타석·이닝·경기 종료 ─────────────────────────

        private void EndPlateAppearance()
        {
            TeamGameState offense = _state.Offense;
            offense.NextBatterIndex = (offense.NextBatterIndex + 1) % offense.Lineup.Count;
            _state.Defense.CurrentPitcher.BattersFaced++;
            _state.PlateAppearancesThisHalf++;
            ResetCountForNewPlateAppearance();

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

        private void ResetCountForNewPlateAppearance()
        {
            _state.Balls = 0;
            _state.Strikes = 0;
            _state.PlateAppearanceNumber++;
            _state.PitchNumberInPlateAppearance = 1;
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
            _state.PlateAppearancesThisHalf = 0;

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
