using System.Collections;
using System.Collections.Generic;
using BaseballProto.Feedback;
using BaseballProto.Input;
using BaseballProto.Quality;
using BaseballProto.View;
using BaseballSim.Engine.AI;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Ratings;
using BaseballSim.Engine.Simulation;
using BaseballSim.Engine.State;
using UnityEngine;

namespace BaseballProto.Core
{
    /// <summary>
    /// 엔진 구동과 투구 한 개 단위 진행.
    /// 엔진 호출(RunUntil·Submit·Step)은 모두 메인 스레드에서 동기로 하고, 연출만 코루틴으로 나눈다 (WebGL 단일 스레드 대응).
    /// 사람 쪽 역할은 양 팀 모두에 배정해 공수 교대와 관계없이 계속 같은 역할을 한다.
    /// </summary>
    public sealed class DuelController
    {
        private const float GameOverHoldS = 2f;
        private const float PitchingModeWindupS = 0.35f;
        private const float SwingContactFraction = 0.45f;
        private const int MaxStepsForAiSwing = 8;

        private readonly MonoBehaviour _host;
        private readonly LeagueConfig _config;
        private readonly ProtoTuning _tuning;
        private readonly FieldView _field;
        private readonly BallView _ball;
        private readonly BatView _bat;
        private readonly BattedBallFlight _flight;
        private readonly RingView _targetRing;
        private readonly RingView _actualRing;
        private readonly BattingInput _batting;
        private readonly PitchingInput _pitching;
        private readonly ImpactFeedback _impact;

        private GameEngine _engine;
        private int _gameCount;
        private bool _restart;

        public DuelController(MonoBehaviour host, LeagueConfig config, ProtoTuning tuning, FieldView field, BallView ball,
            BatView bat, BattedBallFlight flight, RingView targetRing, RingView actualRing, BattingInput batting,
            PitchingInput pitching, ImpactFeedback impact)
        {
            _host = host;
            _config = config;
            _tuning = tuning;
            _field = field;
            _ball = ball;
            _bat = bat;
            _flight = flight;
            _targetRing = targetRing;
            _actualRing = actualRing;
            _batting = batting;
            _pitching = pitching;
            _impact = impact;
        }

        public DuelMode Mode { get; private set; } = DuelMode.Batting;

        public DuelPhase Phase { get; private set; }

        public GameEngine Engine => _engine;

        public LeagueConfig Config => _config;

        /// <summary>현재 대결 (투수 vs 타자)</summary>
        public string Matchup { get; private set; } = string.Empty;

        public BattingJudgement LastBatting { get; private set; }

        public PitchJudgement LastPitching { get; private set; }

        public PitchEvent LastEvent { get; private set; }

        /// <summary>직전 투구 연출 정보 (구속·비행시간)</summary>
        public float LastFlightTimeS { get; private set; }

        public string Headline { get; private set; } = string.Empty;

        public double HeadlineUntil { get; private set; }

        public string Message { get; private set; } = string.Empty;

        /// <summary>모드를 바꾸면 새 경기로 시작한다</summary>
        public void RequestMode(DuelMode mode)
        {
            if (mode != Mode)
            {
                Mode = mode;
                _restart = true;
            }
        }

        public IEnumerator Run()
        {
            while (true)
            {
                if (_engine == null || _engine.IsGameOver || _restart)
                {
                    NewGame();
                }

                RunResult run = _engine.RunUntil(NeverStop.Instance);
                if (run.Status == RunStatus.GameOver)
                {
                    ShowHeadline("GAME OVER  " + _engine.State.AwayScore + " : " + _engine.State.HomeScore, GameOverHoldS);
                    yield return _host.StartCoroutine(Wait(GameOverHoldS));
                    continue;
                }

                PendingDecision pending = _engine.Pending;
                if (pending != null && pending.Kind == DecisionKind.Swing)
                {
                    yield return _host.StartCoroutine(BattingTurn(pending.BattingContext));
                }
                else if (pending != null && pending.Kind == DecisionKind.Pitch)
                {
                    yield return _host.StartCoroutine(PitchingTurn(pending.PitchingContext));
                }
                else
                {
                    // 감독 결정은 AI 담당이라 여기 오지 않는다
                    Message = "Unexpected pending: " + (pending == null ? "none" : pending.Kind.ToString());
                    yield return null;
                }
            }
        }

        // ───────────────────────── 경기 ─────────────────────────

        private void NewGame()
        {
            _gameCount++;
            int seed = Random.Range(1, int.MaxValue);
            ControllerSet controllers = AiControllers.CreateAllAi();
            foreach (TeamSide side in new[] { TeamSide.Away, TeamSide.Home })
            {
                if (Mode == DuelMode.Batting)
                {
                    controllers.Assign(side, HumanBattingDecision.Instance);
                }
                else
                {
                    controllers.Assign(side, HumanPitchingDecision.Instance);
                }
            }

            _engine = new GameEngine(MatchFactory.Create(_gameCount, seed), _config, (ulong)seed, controllers);
            _restart = false;
            LastBatting = null;
            LastPitching = null;
            LastEvent = null;
            Message = "Game " + _gameCount + " (seed " + seed + ")";
        }

        // ───────────────────────── 타격 모드 ─────────────────────────

        private IEnumerator BattingTurn(BattingContext ctx)
        {
            Prepare(ctx.Pitcher, ctx.Batter, ctx.BattingHand);
            Phase = DuelPhase.Windup;
            double windupEnd = ProtoClock.Now + _tuning.WindupS + Random.Range(0f, _tuning.WindupJitterS);
            while (ProtoClock.Now < windupEnd)
            {
                if (_restart)
                {
                    yield break;
                }

                yield return null;
            }

            ExecutedPitch pitch = ctx.Actual;
            var trajectory = new PitchTrajectory(pitch.Actual, pitch.VelocityKmh, pitch.Type, ctx.Pitcher.Throws, ProtoClock.Now,
                _tuning);
            LastFlightTimeS = trajectory.FlightTime;
            double cutoff = trajectory.ArrivalTime + (_tuning.LateCutoffMs + _tuning.DisplayLatencyMs) / 1000.0;
            _batting.Arm(trajectory.ReleaseTime);
            Phase = DuelPhase.InFlight;

            bool swung = false;
            SwingInput swing = default;
            while (true)
            {
                double now = ProtoClock.Now;
                _ball.Show(trajectory.PositionAt(now));
                if (_batting.TryTakeSwing(out swing) && swing.Time <= cutoff)
                {
                    swung = true;
                    break;
                }

                if (now > cutoff)
                {
                    break;
                }

                if (_restart)
                {
                    _batting.Disarm();
                    yield break;
                }

                yield return null;
            }

            _batting.Disarm();

            BatterAction action;
            if (swung)
            {
                LastBatting = BattingQualityMapper.Judge(swing, trajectory.ArrivalTime, pitch.Actual, _tuning);
                action = LastBatting.Action;
                _bat.Swing(_tuning.SwingDurationS);
            }
            else
            {
                LastBatting = null;
                action = BatterAction.Take();
            }

            LastPitching = null;
            int before = _engine.State.Log.Count;
            SubmitResult result = _engine.Submit(action);
            if (!result.Accepted)
            {
                Message = "Rejected: " + result.Reason;
                yield break;
            }

            LastEvent = FindPitchEvent(before);
            yield return _host.StartCoroutine(PlayOutcome(LastEvent, trajectory));
        }

        // ───────────────────────── 투구 모드 ─────────────────────────

        private IEnumerator PitchingTurn(PitchingContext ctx)
        {
            Prepare(ctx.Pitcher, ctx.Batter, ctx.BattingHand);
            Phase = DuelPhase.Aiming;
            var types = new List<PitchType>();
            foreach (PitchRating rating in ctx.Pitcher.Pitching.Repertoire)
            {
                types.Add(rating.Type);
            }

            _pitching.Begin(types);
            while (_pitching.Stage != PitchingStage.Done)
            {
                _pitching.CheckTimeout(ProtoClock.Now);
                if (_pitching.Stage == PitchingStage.Gauge)
                {
                    _targetRing.Show(_pitching.Target);
                }

                if (_restart)
                {
                    _pitching.End();
                    yield break;
                }

                yield return null;
            }

            _pitching.End();
            LastPitching = ReleaseQualityMapper.Judge(_pitching, _tuning);
            LastBatting = null;
            _targetRing.Show(_pitching.Target);

            int before = _engine.State.Log.Count;
            SubmitResult result = _engine.Submit(LastPitching.Call);
            if (!result.Accepted)
            {
                Message = "Rejected: " + result.Reason;
                yield break;
            }

            // AI 타자의 스윙 결정까지 진행
            PitchEvent ev = FindPitchEvent(before);
            for (int i = 0; i < MaxStepsForAiSwing && ev == null; i++)
            {
                StepResult step = _engine.Step();
                ev = FindPitchEvent(before);
                if (step.Status != StepStatus.Advanced)
                {
                    break;
                }
            }

            if (ev == null)
            {
                Message = "No pitch event";
                yield break;
            }

            LastEvent = ev;
            Phase = DuelPhase.Windup;
            yield return _host.StartCoroutine(Wait(PitchingModeWindupS));

            var trajectory = new PitchTrajectory(new PlateLocation(ev.PlateX, ev.PlateZ), ev.VelocityKmh, ev.PitchType,
                ctx.Pitcher.Throws, ProtoClock.Now, _tuning);
            LastFlightTimeS = trajectory.FlightTime;
            Phase = DuelPhase.InFlight;
            bool batStarted = false;
            double swingStart = trajectory.ArrivalTime - _tuning.SwingDurationS * SwingContactFraction;
            while (ProtoClock.Now < trajectory.ArrivalTime)
            {
                _ball.Show(trajectory.PositionAt(ProtoClock.Now));
                if (ev.Swung && !batStarted && ProtoClock.Now >= swingStart)
                {
                    _bat.Swing(_tuning.SwingDurationS);
                    batStarted = true;
                }

                yield return null;
            }

            if (ev.Swung && !batStarted)
            {
                _bat.Swing(_tuning.SwingDurationS);
            }

            yield return _host.StartCoroutine(PlayOutcome(ev, trajectory));
        }

        // ───────────────────────── 결과 연출 ─────────────────────────

        private IEnumerator PlayOutcome(PitchEvent ev, PitchTrajectory trajectory)
        {
            if (ev != null && ev.Result == PitchResult.SwingingStrike)
            {
                _impact.Play(ImpactKind.Whiff);
            }

            while (ProtoClock.Now < trajectory.ArrivalTime)
            {
                _ball.Show(trajectory.PositionAt(ProtoClock.Now));
                yield return null;
            }

            Phase = DuelPhase.Result;
            Vector3 plate = trajectory.PlatePoint;
            _actualRing.Show(new Vector2(plate.x, plate.y));
            float hold = _tuning.ResultHoldS;

            if (ev == null)
            {
                yield return _host.StartCoroutine(Wait(hold));
                yield break;
            }

            switch (ev.Result)
            {
                case PitchResult.InPlay:
                    bool homeRun = ev.PlateAppearanceOutcome == PlateAppearanceOutcome.HomeRun;
                    _impact.Play(homeRun ? ImpactKind.HomeRun
                        : ev.BattedBall != null && ev.BattedBall.IsSolid ? ImpactKind.SolidContact : ImpactKind.WeakContact);
                    if (ev.BattedBall != null)
                    {
                        _flight.Launch(plate, ev.BattedBall);
                        hold += _flight.Duration;
                    }

                    break;

                case PitchResult.Foul:
                    _impact.Play(ImpactKind.Foul);
                    _flight.LaunchFoul(plate, Random.value < 0.5f ? -1f : 1f);
                    hold += _flight.Duration;
                    break;

                case PitchResult.SwingingStrike:
                    break;

                default:
                    _impact.Play(ImpactKind.MittPop);
                    break;
            }

            ShowHeadline(ResultText.Headline(ev), hold);

            double holdEnd = ProtoClock.Now + hold;
            while (ProtoClock.Now < holdEnd || _flight.IsActive)
            {
                if (!_flight.IsActive && ev.Result != PitchResult.InPlay && ev.Result != PitchResult.Foul)
                {
                    _ball.Show(trajectory.PositionAt(ProtoClock.Now));
                }

                yield return null;
            }

            _flight.Stop();
            _ball.Hide();
            _bat.ResetPose();
            _actualRing.Hide();
            _targetRing.Hide();
            Phase = DuelPhase.Idle;
        }

        // ───────────────────────── 도우미 ─────────────────────────

        private void Prepare(Player pitcher, Player batter, Hand battingHand)
        {
            _field.SetBatter(battingHand);
            _field.SetPitcherHand(pitcher.Throws);
            _bat.SetSide(_field.BatterSide);
            _ball.Hide();
            _actualRing.Hide();
            _targetRing.Hide();
            Matchup = pitcher.Name + " (" + (pitcher.Throws == Hand.Right ? "R" : "L") + ")  vs  " + batter.Name + " ("
                + (battingHand == Hand.Right ? "R" : "L") + ")";
        }

        private PitchEvent FindPitchEvent(int fromIndex)
        {
            List<GameEvent> log = _engine.State.Log;
            for (int i = fromIndex; i < log.Count; i++)
            {
                if (log[i] is PitchEvent pitch)
                {
                    return pitch;
                }
            }

            return null;
        }

        private void ShowHeadline(string text, float seconds)
        {
            Headline = text;
            HeadlineUntil = ProtoClock.Now + seconds;
        }

        private static IEnumerator Wait(float seconds)
        {
            double end = ProtoClock.Now + seconds;
            while (ProtoClock.Now < end)
            {
                yield return null;
            }
        }
    }
}
