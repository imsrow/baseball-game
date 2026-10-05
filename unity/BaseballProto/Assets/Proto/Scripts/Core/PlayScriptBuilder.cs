using System.Collections.Generic;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Fielding;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Teams;
using UnityEngine;

namespace BaseballProto.Core
{
    /// <summary>
    /// 엔진 투구 이벤트(인플레이) → 연출 대본. 결과는 엔진이 정한 그대로 보여주기만 한다:
    /// - 공: 엔진의 체공·낙하·처리 시각과 지점을 지난다
    /// - 처리 야수: 바로 첫 발을 떼고 곡선 경로로 엔진 도착 시각에 맞춰 도착 (멍하니 서 있지 않게)
    /// - 송구·주자: 엔진 주자 이동(아웃·세이프)에 맞게 도착 순서를 맞춘다. 주자 속도는 그 주자 스피드 능력치 범위 안에서만,
    ///   그걸로 안 되면 송구 시작·속도를 바꾼다
    /// 좌표: 엔진 필드 (x, y) → 월드 (x, 0, y).
    /// </summary>
    public sealed class PlayScriptBuilder
    {
        private const float BatterBoxZ = -0.1f;
        private const float BatterBoxX = 0.85f;
        private const float GroundBallStartHeightM = 0.3f;
        private const float BobbleOffsetM = 1.5f;
        private const float BobbleRecoverS = 0.9f;
        private const float CatcherStandBackM = 0.6f;
        private const float MinMoveM = 0.4f;
        private const float DriftRatio = 0.15f;
        private const float DriftMinS = 0.8f;
        private const float DriftSpeedMps = 5f;
        private const float CoverSpeedMps = 6.5f;
        private const float CoverEarlyS = 0.3f;
        private const float DeepBallM = 50f;
        private const float JogSpeedMps = 5.5f;
        private const float CampBehindM = 1.2f;
        private const float DiveSpeedMps = 14f;

        private readonly LeagueConfig _config;
        private readonly FieldGeometry _field;
        private readonly ProtoTuning _t;
        private readonly BaserunningModel _running;

        public PlayScriptBuilder(LeagueConfig config, FieldGeometry field, ProtoTuning tuning)
        {
            _config = config;
            _field = field;
            _t = tuning;
            _running = new BaserunningModel(config.Baserunning);
        }

        public PlayScript Build(PitchEvent ev, Vector3 contact, float batterSide, PlayerDirectory players)
        {
            BattedBallData b = ev.BattedBall;
            FieldingConfig fc = _config.Fielding;
            PlateAppearanceOutcome outcome = ev.PlateAppearanceOutcome ?? PlateAppearanceOutcome.Single;
            bool homeRun = outcome == PlateAppearanceOutcome.HomeRun;
            bool air = !ev.IsBunt && b.LaunchAngleDeg >= fc.GroundBallMaxLaunchAngleDeg;
            bool landed = b.HasLanding && b.LandingTimeS > 0;
            bool catchAttempt = air && !homeRun && !landed;
            bool error = ev.IsError && outcome == PlateAppearanceOutcome.ReachedOnError;
            bool caughtOut = catchAttempt && !error;
            Vector3 end = Ground(b.EndX, b.EndY);
            Position? primary = homeRun ? (Position?)null : b.FieldedBy;
            bool infieldPlay = primary.HasValue && !PositionInfo.IsOutfield(primary.Value);

            var script = new PlayScript { Primary = primary };
            foreach (Position p in PositionInfo.Fielding)
            {
                script.Fielders[p] = new MotionTrack(Ground(_field.FielderStart(p)));
            }

            // ── 공 ──
            var ball = new MotionTrack(contact);
            script.Ball = ball;
            float fielded = FieldedTime(b, contact, end, air);
            float reach = fielded;
            float catchHeight = (float)_config.Physics.CatchHeightM;
            if (homeRun)
            {
                float land = (float)(b.LandingTimeS > 0 ? b.LandingTimeS : System.Math.Max(b.HangTimeS, 3.0));
                AddFlight(ball, end, 0f, land);
                script.BallHideTime = land + 0.3f;
                script.End = land + _t.HomeRunTailS;
            }
            else if (catchAttempt)
            {
                AddFlight(ball, end + Vector3.up * catchHeight, 0f, fielded);
            }
            else if (landed)
            {
                float land = (float)b.LandingTimeS;
                AddFlight(ball, Ground(b.LandingX, b.LandingY), 0f, land);
                reach = infieldPlay ? fielded : Mathf.Max(land + 0.3f, fielded - (float)fc.OutfieldPickupS);
                MotionSegment roll = ball.MoveTo(end, land, reach, MotionEase.EaseOut);
                roll.Hops = 1;
                roll.HopHeight = _t.LandingHopHeightM;
                ball.Add(roll);
            }
            else
            {
                // 땅볼: 내야수가 잡는 곳까지는 빠르게, 외야로 빠지면 점점 느려진다
                reach = infieldPlay ? fielded : Mathf.Max(0.3f, fielded - (float)fc.OutfieldPickupS);
                MotionSegment roll = ball.MoveTo(end + Vector3.up * 0.1f, 0f, reach,
                    infieldPlay ? MotionEase.Linear : MotionEase.EaseOut);
                roll.From = new Vector3(contact.x, GroundBallStartHeightM, contact.z);
                roll.Hops = _t.GroundBallHops;
                roll.HopHeight = _t.GroundBallHopHeightM;
                ball.Add(roll);
            }

            script.FieldedTime = fielded;

            // ── 처리 야수 ──
            float pickup = fielded;
            Vector3 throwFrom = end;
            if (primary.HasValue)
            {
                MotionTrack track = script.Fielders[primary.Value];
                float depart = FirstStep(primary.Value);
                float arrive = (float)b.FielderArrivalS;
                if (arrive <= 0f)
                {
                    arrive = depart + Flat(track.Start, end) / CoverSpeedMps;
                }

                if (catchAttempt)
                {
                    // 여유 있는 뜬공: 조깅으로 낙하 지점 조금 뒤까지 가서 자리 잡고, 포구 직전에 앞으로 들어오며 잡는다.
                    // 빠듯하면 엔진 도착 시각(포구 순간까지)에 맞추고, 엔진이 늦게 도착해도 잡았으면 몸을 날린다
                    float jogArrive = Mathf.Max(arrive, depart + Flat(track.Start, end) / JogSpeedMps);
                    if (jogArrive < fielded - _t.CatchSettleS)
                    {
                        Vector3 camp = end + (end - Vector3.zero).normalized * CampBehindM;
                        Run(track, camp, depart, jogArrive, primary.Value, _t.FielderMaxSpeedMps);
                        track.Add(track.MoveTo(end, fielded - _t.CatchSettleS, fielded, MotionEase.Smooth));
                    }
                    else
                    {
                        Run(track, end, depart, Mathf.Min(arrive, fielded), primary.Value, DiveSpeedMps);
                    }
                }
                else
                {
                    Run(track, end, depart, Mathf.Min(arrive, reach), primary.Value, _t.FielderMaxSpeedMps);
                }

                if (catchAttempt && !caughtOut)
                {
                    // 뜬공 포구 실책: 공을 떨어뜨리고 다시 줍는다
                    throwFrom = end + (Vector3.zero - end).normalized * BobbleOffsetM;
                    MotionSegment drop = ball.MoveTo(throwFrom + Vector3.up * 0.1f, fielded, fielded + 0.6f, MotionEase.EaseOut);
                    drop.Hops = 1;
                    drop.HopHeight = 0.5f;
                    ball.Add(drop);
                    pickup = fielded + BobbleRecoverS;
                    track.Add(track.MoveTo(throwFrom, fielded + 0.2f, pickup, MotionEase.Smooth));
                }
                else if (error && infieldPlay && BatterTo(ev) <= 1)
                {
                    // 땅볼 포구 실책: 튀어 나간 공을 다시 줍는다
                    throwFrom = end + (end - Vector3.zero).normalized * BobbleOffsetM;
                    MotionSegment bobble = ball.MoveTo(throwFrom + Vector3.up * 0.1f, fielded, fielded + 0.4f, MotionEase.EaseOut);
                    bobble.Hops = 1;
                    bobble.HopHeight = 0.6f;
                    ball.Add(bobble);
                    pickup = fielded + BobbleRecoverS;
                    track.Add(track.MoveTo(throwFrom, fielded + 0.1f, pickup, MotionEase.Smooth));
                }
            }

            // ── 주자 계획 ──
            var plans = new List<RunnerPlan>();
            RunnerPlan batter = null;
            foreach (RunnerMovement m in ev.RunnerMovements)
            {
                Hand hand = m.FromBase == 0 ? ev.BattingHand : Hand.Right;
                var plan = new RunnerPlan(m, players.Get(m.PlayerId).Batting, hand, _running, _t.RunnerSpeedRangeSd);
                plans.Add(plan);
                if (plan.IsBatter)
                {
                    batter = plan;
                }

                if (homeRun)
                {
                    plan.Start = 0.3f;
                    plan.Scale = _t.HomeRunTrotScale;
                    plan.Fixed = true;
                }
                else if (caughtOut)
                {
                    // 태그업: 포구 순간 출발
                    plan.Start = plan.IsBatter ? 0f : fielded + (float)_config.Baserunning.TagUpReactionS;
                }
                else if (air && !plan.IsBatter && ev.OutsBefore < _config.Rules.OutsPerHalfInning - 1)
                {
                    // 2아웃 전 뜬 타구: 주자는 떨어지는 걸 확인하고 출발
                    plan.Start = (float)_config.Baserunning.FlyBallHoldDelayS;
                }
            }

            // ── 송구 ──
            var legs = new List<ThrowLeg>();
            if (!homeRun && primary.HasValue)
            {
                BuildThrows(ev, outcome, plans, batter, primary.Value, infieldPlay, caughtOut, error, throwFrom, pickup, legs);
            }

            Sync(legs);

            // 송구 공 경로와 받는 야수
            var receivers = new HashSet<Position>();
            foreach (ThrowLeg leg in legs)
            {
                Vector3 release = leg.From + Vector3.up * _t.ThrowReleaseHeightM;
                Vector3 glove = leg.Target + Vector3.up * _t.GloveHeightM;
                ball.Add(ball.MoveTo(release, leg.Start - 0.15f, leg.Start, MotionEase.Smooth));
                MotionSegment flight = ball.MoveTo(glove, leg.Start, leg.Arrive);
                flight.Arc = Flat(leg.From, leg.Target) * _t.ThrowArcRatio;
                ball.Add(flight);
                if (leg.Overshoot)
                {
                    Vector3 past = leg.Target + (leg.Target - leg.From).normalized * _t.OvershootM;
                    MotionSegment wild = ball.MoveTo(past + Vector3.up * 0.1f, leg.Arrive, leg.Arrive + 0.7f, MotionEase.EaseOut);
                    wild.Hops = 1;
                    wild.HopHeight = 0.4f;
                    ball.Add(wild);
                }

                script.Throws.Add(new ThrowScript { Start = leg.Start, Arrive = leg.Arrive, Target = glove });
                if (receivers.Add(leg.Receiver))
                {
                    float after = primary.HasValue && leg.Receiver == primary.Value ? pickup + 0.1f : 0f;
                    Cover(script.Fielders[leg.Receiver], leg.Target, leg.Arrive, leg.Receiver, after);
                }
            }

            // 나머지 야수: 공 쪽으로 반응 (옆 외야수는 백업)
            Vector3 focus = homeRun ? end : (b.HasLanding ? Ground(b.LandingX, b.LandingY) : end);
            foreach (Position p in PositionInfo.Fielding)
            {
                if ((primary.HasValue && p == primary.Value) || receivers.Contains(p))
                {
                    continue;
                }

                Drift(script.Fielders[p], focus, p);
            }

            // ── 주자 경로 ──
            float lastRunner = 0f;
            foreach (RunnerPlan plan in plans)
            {
                RunnerScript runner = BuildRunner(plan, ev, air, caughtOut, homeRun, fielded, batterSide);
                script.Runners.Add(runner);
                if (!homeRun)
                {
                    lastRunner = Mathf.Max(lastRunner, Mathf.Min(runner.Track.EndTime, runner.HideTime));
                }
            }

            if (!homeRun)
            {
                float lastThrow = legs.Count > 0 ? legs[legs.Count - 1].Arrive + (legs[legs.Count - 1].Overshoot ? 0.7f : 0f) : 0f;
                script.End = Mathf.Max(Mathf.Max(ball.EndTime, lastThrow), Mathf.Max(lastRunner, pickup)) + _t.PlayEndPadS;
            }

            script.CameraProfile = Profile(outcome, air, homeRun, end, b);
            return script;
        }

        // ───────────────────────── 송구 결정 ─────────────────────────

        private void BuildThrows(PitchEvent ev, PlateAppearanceOutcome outcome, List<RunnerPlan> plans, RunnerPlan batter,
            Position primary, bool infieldPlay, bool caughtOut, bool error, Vector3 from, float pickup, List<ThrowLeg> legs)
        {
            float spray = (float)ev.BattedBall.SprayAngleDeg;
            RunnerPlan outRunner = null;
            foreach (RunnerPlan p in plans)
            {
                if (p.IsOut && !(caughtOut && p.IsBatter))
                {
                    outRunner = p;
                    break;
                }
            }

            float transfer = (float)(infieldPlay ? _config.Fielding.InfieldTransferS : _config.Fielding.OutfieldTransferS);
            ThrowLeg Leg(Vector3 origin, int toBase, RunnerPlan runner, bool runnerOut)
            {
                return NewLeg(origin, toBase, runner, runnerOut, primary, infieldPlay, spray, pickup + transfer, pickup);
            }

            if (caughtOut)
            {
                if (outRunner != null)
                {
                    legs.Add(Leg(from, outRunner.To, outRunner, true));
                    return;
                }

                RunnerPlan lead = LeadAdvancer(plans, false);
                if (lead != null)
                {
                    legs.Add(Leg(from, lead.To, lead, false));
                }

                return;
            }

            if (infieldPlay)
            {
                if (outcome == PlateAppearanceOutcome.GroundedIntoDoublePlay)
                {
                    RunnerPlan forced = plans.Find(p => p.IsOut && !p.IsBatter);
                    ThrowLeg first = Leg(from, forced != null ? forced.To : 2, forced, true);
                    legs.Add(first);
                    ThrowLeg relay = NewLeg(first.Target, 1, batter, true, first.Receiver, true, spray, 0f, 0f);
                    relay.EarliestStart = -1f; // Sync가 첫 송구 도착 + 피벗 시간으로 정한다
                    legs.Add(relay);
                    return;
                }

                if (outRunner != null)
                {
                    legs.Add(Leg(from, outRunner.To, outRunner, true));
                    return;
                }

                if (error && batter != null && batter.To >= 2)
                {
                    ThrowLeg wild = Leg(from, 1, null, false);
                    wild.Overshoot = true;
                    legs.Add(wild);
                    return;
                }

                RunnerPlan fromFirst = plans.Find(p => p.From == 1);
                if (outcome == PlateAppearanceOutcome.FieldersChoice && fromFirst != null)
                {
                    legs.Add(Leg(from, 2, fromFirst, false));
                    return;
                }

                legs.Add(Leg(from, 1, batter, false));
                return;
            }

            // 외야 처리: 아웃시킨 주자에게, 아니면 타자 다음 베이스로 중계
            if (outRunner != null)
            {
                legs.Add(Leg(from, outRunner.To, outRunner, true));
                return;
            }

            int batterBase = batter != null ? batter.To : 1;
            int target = Mathf.Min(3, batterBase + 1);
            RunnerPlan arriving = plans.Find(p => p.To == target && !p.IsOut);
            legs.Add(Leg(from, target, arriving, false));
        }

        private ThrowLeg NewLeg(Vector3 from, int toBase, RunnerPlan runner, bool runnerOut, Position thrower, bool infieldThrow,
            float spray, float start, float pickup)
        {
            FieldingConfig fc = _config.Fielding;
            Position receiver = Receiver(toBase, thrower, spray);
            Vector3 target = BasePoint(toBase);
            float speed = (float)(infieldThrow ? fc.InfieldThrowSpeedMps : fc.OutfieldThrowSpeedMps);
            float distance = Mathf.Max(1f, Flat(from, target));
            var leg = new ThrowLeg
            {
                From = from,
                ToBase = toBase,
                Target = target,
                Receiver = receiver,
                Start = start,
                Duration = distance / speed,
                MinDuration = distance / (speed * _t.ThrowSpeedMaxRatio),
                MaxDuration = distance / (speed * _t.ThrowSpeedMinRatio),
                EarliestStart = pickup + _t.MinThrowTransferS,
                Runner = runner,
                RunnerOut = runnerOut,
            };
            if (runner != null && (toBase <= runner.From || toBase > Mathf.Max(runner.To, runner.From + 1)))
            {
                leg.Runner = null;
            }

            return leg;
        }

        /// <summary>
        /// 송구와 주자 도착 순서를 엔진 결과에 맞춘다. 아웃이면 공이 먼저, 세이프면 주자가 먼저.
        /// 1) 주자 속도를 능력치 범위 안에서 맞추고 2) 모자라면 송구 속도(범위 안)와 시작 시각을 바꾼다.
        /// </summary>
        private void Sync(List<ThrowLeg> legs)
        {
            float m = _t.OutSyncMarginS;
            float previousArrive = 0f;
            for (int i = 0; i < legs.Count; i++)
            {
                ThrowLeg leg = legs[i];
                if (leg.EarliestStart < 0f)
                {
                    // 병살 중계: 첫 송구를 받은 뒤 피벗
                    leg.Start = previousArrive + (float)_config.Fielding.PivotS;
                    leg.EarliestStart = previousArrive + _t.MinThrowTransferS;
                }

                leg.Start = Mathf.Max(leg.Start, leg.EarliestStart);
                RunnerPlan runner = leg.Runner;
                if (runner != null)
                {
                    int k = leg.ToBase;
                    if (leg.RunnerOut)
                    {
                        float got = runner.SetArrival(k, leg.Arrive + m);
                        if (got < leg.Arrive + 0.05f)
                        {
                            // 가장 느리게 뛰어도 공보다 빠르다: 공을 더 빨리
                            float need = got - m;
                            leg.Duration = Mathf.Clamp(need - leg.Start, leg.MinDuration, leg.MaxDuration);
                            if (leg.Arrive > need)
                            {
                                leg.Start = Mathf.Max(leg.EarliestStart, need - leg.Duration);
                            }
                        }

                        runner.OutTime = Mathf.Max(runner.ArrivalAt(k), leg.Arrive);
                    }
                    else
                    {
                        float got = runner.SetArrival(k, leg.Arrive - m);
                        if (got > leg.Arrive - 0.05f)
                        {
                            // 가장 빠르게 뛰어도 공보다 늦다: 공을 더 늦게
                            float need = got + m;
                            leg.Duration = Mathf.Clamp(need - leg.Start, leg.MinDuration, leg.MaxDuration);
                            if (leg.Arrive < need)
                            {
                                leg.Start = need - leg.Duration;
                            }
                        }
                    }
                }

                previousArrive = leg.Arrive;
            }
        }

        // ───────────────────────── 주자 ─────────────────────────

        private RunnerScript BuildRunner(RunnerPlan plan, PitchEvent ev, bool air, bool caughtOut, bool homeRun, float fielded,
            float batterSide)
        {
            Vector3 start = plan.IsBatter ? new Vector3(batterSide * BatterBoxX, 0f, BatterBoxZ) : BasePoint(plan.From);
            var track = new MotionTrack(start);
            var runner = new RunnerScript(plan.PlayerId, track);

            if (plan.IsBatter && caughtOut)
            {
                // 뜬공 아웃: 1루 쪽으로 뛰다가 잡히면 멈춘다
                float outAt = fielded;
                float full = plan.Natural(1) * _t.BatterJogScale;
                float fraction = Mathf.Clamp01((outAt + 0.3f) / Mathf.Max(0.1f, full));
                Vector3 stop = Vector3.Lerp(start, BasePoint(1), Mathf.Min(0.9f, fraction));
                track.Add(track.MoveTo(stop, 0f, outAt + 0.3f, MotionEase.Accelerate));
                runner.OutTime = outAt;
                runner.HideTime = outAt + _t.RunnerHideDelayS;
                return runner;
            }

            if (plan.To <= plan.From && !plan.IsOut)
            {
                // 그대로 있는 주자: 리드했다가 돌아온다
                Vector3 lead = Vector3.MoveTowards(start, BasePoint(plan.From + 1), air ? _t.RunnerLeadM : _t.RunnerLeadM * 0.6f);
                float back = caughtOut ? fielded + 0.2f : 1.4f;
                track.Add(track.MoveTo(lead, 0.1f, 0.8f, MotionEase.Smooth));
                track.Add(track.MoveTo(start, back, back + 0.9f, MotionEase.Smooth));
                runner.FinalBase = plan.From;
                return runner;
            }

            float previous = plan.Start;
            for (int k = plan.From + 1; k <= plan.To; k++)
            {
                float arrive = plan.ArrivalAt(k);
                track.Add(track.MoveTo(BasePoint(k), previous, arrive, k == plan.From + 1 ? MotionEase.Accelerate : MotionEase.Linear));
                previous = arrive;
            }

            if (plan.IsOut)
            {
                runner.OutTime = float.IsNaN(plan.OutTime) ? previous : plan.OutTime;
                runner.HideTime = runner.OutTime + _t.RunnerHideDelayS;
            }
            else if (plan.To >= 4)
            {
                runner.HideTime = previous + _t.RunnerHideDelayS;
            }
            else
            {
                runner.FinalBase = plan.To;
            }

            return runner;
        }

        private static RunnerPlan LeadAdvancer(List<RunnerPlan> plans, bool includeBatter)
        {
            RunnerPlan lead = null;
            foreach (RunnerPlan p in plans)
            {
                if ((includeBatter || !p.IsBatter) && p.To > p.From && !p.IsOut && (lead == null || p.To > lead.To))
                {
                    lead = p;
                }
            }

            return lead;
        }

        private static int BatterTo(PitchEvent ev)
        {
            foreach (RunnerMovement m in ev.RunnerMovements)
            {
                if (m.FromBase == 0)
                {
                    return m.ToBase;
                }
            }

            return 1;
        }

        // ───────────────────────── 야수 ─────────────────────────

        /// <summary>베이스를 받을 야수: 1루수/2루 커버(왼쪽 타구는 2루수, 오른쪽은 유격수)/3루수/포수. 송구한 야수는 빼고</summary>
        private static Position Receiver(int toBase, Position thrower, float spray)
        {
            Position cover;
            switch (toBase)
            {
                case 1:
                    // 1루수가 잡으면 투수가 1루 커버
                    cover = thrower == Position.FirstBase ? Position.Pitcher : Position.FirstBase;
                    break;
                case 2:
                    cover = spray > 0f ? Position.Shortstop : Position.SecondBase;
                    if (cover == thrower)
                    {
                        cover = cover == Position.Shortstop ? Position.SecondBase : Position.Shortstop;
                    }

                    break;
                case 3:
                    cover = thrower == Position.ThirdBase ? Position.Shortstop : Position.ThirdBase;
                    break;
                default:
                    cover = thrower == Position.Catcher ? Position.Pitcher : Position.Catcher;
                    break;
            }

            return cover;
        }

        private void Run(MotionTrack track, Vector3 target, float depart, float arrive, Position position, float maxSpeed)
        {
            Vector3 from = track.EndPosition;
            float distance = Flat(from, target);
            if (distance < MinMoveM)
            {
                return;
            }

            arrive = Mathf.Max(arrive, depart + distance / maxSpeed);
            MotionSegment run = track.MoveTo(target, depart, arrive, MotionEase.Accelerate);
            float bend = PositionInfo.IsOutfield(position) ? _t.OutfielderRouteBend : _t.InfielderRouteBend;
            if (bend > 0f)
            {
                // 홈에서 먼 쪽으로 휘는 경로 (뒤로 돌아 들어가는 외야수의 바나나 루트)
                Vector3 mid = (from + target) * 0.5f;
                Vector3 side = Vector3.Cross(Vector3.up, target - from).normalized;
                if (Vector3.Dot(side, mid) < 0f)
                {
                    side = -side;
                }

                run.Curved = true;
                run.Control = mid + side * (bend * distance);
            }

            track.Add(run);
        }

        /// <param name="earliestDepart">처리 야수가 베이스로 갈 때는 공을 잡은 뒤에 출발</param>
        private void Cover(MotionTrack track, Vector3 basePoint, float ballArrive, Position position, float earliestDepart)
        {
            float depart = Mathf.Max(FirstStep(position), earliestDepart);
            float distance = Flat(track.EndPosition, basePoint);
            float arrive = Mathf.Max(depart + distance / CoverSpeedMps, ballArrive - CoverEarlyS);
            Run(track, basePoint, depart, Mathf.Min(arrive, Mathf.Max(ballArrive, depart + distance / _t.FielderMaxSpeedMps)),
                position, _t.FielderMaxSpeedMps);
        }

        private void Drift(MotionTrack track, Vector3 focus, Position position)
        {
            Vector3 start = track.Start;
            float distance = Flat(start, focus);
            if (distance < MinMoveM)
            {
                return;
            }

            float move;
            if (position == Position.Catcher)
            {
                // 포수는 일어나 홈 앞으로
                track.Add(track.MoveTo(new Vector3(0f, 0f, -CatcherStandBackM), 0.4f, 1.2f, MotionEase.Smooth));
                return;
            }

            if (PositionInfo.IsOutfield(position) && focus.magnitude > DeepBallM)
            {
                move = Mathf.Min(_t.BackupMaxM, distance * _t.BackupRatio);
            }
            else
            {
                move = Mathf.Min(_t.DriftMaxM, distance * DriftRatio);
            }

            Vector3 target = Vector3.MoveTowards(start, focus, move);
            float depart = FirstStep(position);
            track.Add(track.MoveTo(target, depart, depart + Mathf.Max(DriftMinS, move / DriftSpeedMps), MotionEase.Accelerate));
        }

        private float FirstStep(Position position)
        {
            return PositionInfo.IsOutfield(position) ? _t.OutfielderFirstStepS : _t.InfielderFirstStepS;
        }

        // ───────────────────────── 공 ─────────────────────────

        private static void AddFlight(MotionTrack ball, Vector3 to, float t0, float t1)
        {
            MotionSegment flight = ball.MoveTo(to, t0, t1);
            float hang = Mathf.Max(0f, t1 - t0);
            // 같은 체공시간의 포물선 높이 (g t² / 8)
            flight.Arc = 9.81f * hang * hang / 8f;
            ball.Add(flight);
        }

        /// <summary>엔진 처리 시각. 구버전 저장 등으로 없으면 거리로 어림한다</summary>
        private static float FieldedTime(BattedBallData b, Vector3 contact, Vector3 end, bool air)
        {
            if (b.FieldedTimeS > 0)
            {
                return (float)b.FieldedTimeS;
            }

            if (air && b.HangTimeS > 0)
            {
                return (float)b.HangTimeS;
            }

            return 0.5f + Flat(contact, end) / 12f;
        }

        private BallCamProfile Profile(PlateAppearanceOutcome outcome, bool air, bool homeRun, Vector3 end, BattedBallData b)
        {
            float distance = b.HasLanding ? Ground(b.LandingX, b.LandingY).magnitude : end.magnitude;
            if (homeRun || outcome == PlateAppearanceOutcome.Double || outcome == PlateAppearanceOutcome.Triple
                || (air && distance >= _t.BallCamFarDistanceM))
            {
                return BallCamProfile.Far;
            }

            return air ? BallCamProfile.Mid : BallCamProfile.Near;
        }

        // ───────────────────────── 좌표 ─────────────────────────

        private Vector3 BasePoint(int baseIndex)
        {
            return Ground(_field.Base(Mathf.Clamp(baseIndex, 0, 4)));
        }

        private static Vector3 Ground(FieldPoint p)
        {
            return new Vector3((float)p.X, 0f, (float)p.Y);
        }

        private static Vector3 Ground(double x, double y)
        {
            return new Vector3((float)x, 0f, (float)y);
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }
    }
}
