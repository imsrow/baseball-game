using System.Collections.Generic;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Fielding;

namespace BaseballSim.Engine.Stats
{
    /// <summary>
    /// 이벤트 로그에서 기록을 파생한다 (리그 합계, 선수별 타격·투구, 투구 진단).
    /// 시뮬 구간이든 직접 플레이 구간이든 같은 로그를 받으므로 기록이 동일하게 쌓인다.
    /// </summary>
    public sealed class StatsAggregator : IEventSink
    {
        public BattingLine League { get; } = new BattingLine();

        public PitchDiagnostics Diagnostics { get; } = new PitchDiagnostics();

        public Dictionary<int, BattingLine> Batters { get; } = new Dictionary<int, BattingLine>();

        public Dictionary<int, PitchingLine> Pitchers { get; } = new Dictionary<int, PitchingLine>();

        public long PitchingChanges { get; private set; }

        public void OnEvent(GameEvent gameEvent)
        {
            if (gameEvent is PitchEvent pitch)
            {
                OnPitch(pitch);
            }
            else if (gameEvent is SubstitutionEvent sub && sub.Kind == SubstitutionKind.PitchingChange)
            {
                PitchingChanges++;
            }
        }

        public void OnEvents(IEnumerable<GameEvent> events)
        {
            foreach (GameEvent e in events)
            {
                OnEvent(e);
            }
        }

        private void OnPitch(PitchEvent e)
        {
            PitchDiagnostics d = Diagnostics;
            d.Pitches++;
            if (e.IsInZone)
            {
                d.InZone++;
            }

            if (e.Swung)
            {
                d.Swings++;
                bool contact = e.Result == PitchResult.Foul || e.Result == PitchResult.InPlay;
                if (contact)
                {
                    d.Contacts++;
                }

                if (e.IsInZone)
                {
                    d.ZoneSwings++;
                    if (contact)
                    {
                        d.ZoneContacts++;
                    }
                }
                else
                {
                    d.ChaseSwings++;
                    if (contact)
                    {
                        d.ChaseContacts++;
                    }
                }
            }

            if (e.Result == PitchResult.Foul)
            {
                d.Fouls++;
            }

            if (e.Result == PitchResult.CalledStrike)
            {
                d.CalledStrikes++;
            }

            if (e.StealFromBase > 0)
            {
                d.StealAttempts++;
                BattingLine runner = Get(Batters, e.StealRunnerId);
                if (e.StealSucceeded)
                {
                    d.StolenBases++;
                    runner.StolenBases++;
                    League.StolenBases++;
                }
                else
                {
                    runner.CaughtStealing++;
                    League.CaughtStealing++;
                }
            }

            if (e.IsWildPitch)
            {
                d.WildPitches++;
            }

            if (e.IsPassedBall)
            {
                d.PassedBalls++;
            }

            if (e.BatterReachedOnDroppedThirdStrike)
            {
                d.DroppedThirdStrikeReaches++;
            }

            if (e.Result == PitchResult.InPlay && e.IsBunt)
            {
                d.BuntsInPlay++;
            }

            if (e.Result == PitchResult.InPlay && e.BattedBall != null && !e.IsBunt)
            {
                d.InPlay++;
                d.BattedBallTypes[(int)e.BattedBall.Type]++;
                d.ExitVelocitySumKmh += e.BattedBall.ExitVelocityKmh;
                d.LaunchAngleSumDeg += e.BattedBall.LaunchAngleDeg;
                PlateAppearanceOutcome? result = e.PlateAppearanceOutcome;
                if (result.HasValue && result.Value != PlateAppearanceOutcome.HomeRun
                    && result.Value != PlateAppearanceOutcome.SacrificeBunt)
                {
                    d.BallsInPlayByType[(int)e.BattedBall.Type]++;
                    if (PlateAppearanceOutcomeInfo.IsHit(result.Value))
                    {
                        d.HitsByType[(int)e.BattedBall.Type]++;
                    }
                }
            }

            if (e.IsError)
            {
                d.Errors++;
            }

            d.Runs += e.RunsScored;

            PitchingLine pitcher = Get(Pitchers, e.PitcherId);
            pitcher.Pitches++;
            pitcher.Outs += e.OutsAfter - e.OutsBefore;
            pitcher.Runs += e.RunsScored;

            if (!e.RunsNullified)
            {
                foreach (RunnerMovement move in e.RunnerMovements)
                {
                    if (move.Scored)
                    {
                        Get(Batters, move.PlayerId).Runs++;
                    }
                }
            }

            if (e.PlateAppearanceOutcome.HasValue)
            {
                PlateAppearanceOutcome outcome = e.PlateAppearanceOutcome.Value;
                League.Record(outcome);
                League.RunsBattedIn += e.RunsBattedIn;
                BattingLine batter = Get(Batters, e.BatterId);
                batter.Record(outcome);
                batter.RunsBattedIn += e.RunsBattedIn;
                pitcher.Record(outcome);
            }
        }

        private static T Get<T>(Dictionary<int, T> map, int id) where T : new()
        {
            if (!map.TryGetValue(id, out T value))
            {
                value = new T();
                map[id] = value;
            }

            return value;
        }
    }
}
