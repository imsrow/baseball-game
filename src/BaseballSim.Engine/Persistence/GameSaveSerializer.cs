using System.Collections.Generic;
using System.IO;
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
using BaseballSim.Engine.Teams;

namespace BaseballSim.Engine.Persistence
{
    /// <summary>
    /// 경기 저장/불러오기 (BCL BinaryWriter 기반, 형식 버전 포함).
    /// 담는 것: GameState 전체(난수 상태·이벤트 로그 포함), 양 팀 선수 능력치 스냅샷, 팀 이름, 담당 방식, 설정 지문.
    /// </summary>
    public static class GameSaveSerializer
    {
        // "BBSV" (파일 식별자)
        private const int Magic = 0x56534242;
        private const int FormatVersion = 2;

        /// <summary>읽을 수 있는 가장 오래된 형식 (v1: 타구 연출용 시각 없음)</summary>
        private const int MinReadableVersion = 1;

        // 이벤트 종류 태그
        private const int PitchEventTag = 1;
        private const int SubstitutionEventTag = 2;
        private const int IntentionalWalkEventTag = 3;

        public static byte[] Save(GameEngine engine)
        {
            using (var stream = new MemoryStream())
            using (var binary = new BinaryWriter(stream))
            {
                var w = new SaveWriter(binary);
                w.Int(Magic);
                w.Int(FormatVersion);
                w.ULong(ConfigFingerprint.Compute(engine.Config));
                WriteModes(w, ControlModes.From(engine.Controllers));
                WriteTeamNames(w, engine.TeamNames);
                WritePlayers(w, engine.Players);
                WriteState(w, engine.State);
                binary.Flush();
                return stream.ToArray();
            }
        }

        public static SavedGame Load(byte[] data, LeagueConfig config)
        {
            using (var stream = new MemoryStream(data))
            using (var binary = new BinaryReader(stream))
            {
                var r = new SaveReader(binary);
                if (r.Int() != Magic)
                {
                    throw new SaveFormatException("야구 경기 저장 데이터가 아닙니다.");
                }

                int version = r.Int();
                if (version < MinReadableVersion || version > FormatVersion)
                {
                    throw new SaveFormatException("지원하지 않는 저장 형식 버전입니다: " + version);
                }

                r.Version = version;
                var saved = new SavedGame { ConfigMatches = r.ULong() == ConfigFingerprint.Compute(config) };
                saved.Modes = ReadModes(r);
                saved.TeamNames = ReadTeamNames(r);
                saved.Players = ReadPlayers(r);
                saved.State = ReadState(r);
                return saved;
            }
        }

        // ───────────────────────── 담당·팀 ─────────────────────────

        private static void WriteModes(SaveWriter w, ControlModes modes)
        {
            for (int side = 0; side < 2; side++)
            {
                for (int role = 0; role < 3; role++)
                {
                    w.Bool(modes.Human[side, role]);
                }

                w.Bool(modes.ManagerDelegatedToAi[side]);
            }
        }

        private static ControlModes ReadModes(SaveReader r)
        {
            var modes = new ControlModes();
            for (int side = 0; side < 2; side++)
            {
                for (int role = 0; role < 3; role++)
                {
                    modes.Human[side, role] = r.Bool();
                }

                modes.ManagerDelegatedToAi[side] = r.Bool();
            }

            return modes;
        }

        private static void WriteTeamNames(SaveWriter w, Dictionary<int, string> names)
        {
            w.Int(names.Count);
            foreach (KeyValuePair<int, string> pair in names)
            {
                w.Int(pair.Key);
                w.String(pair.Value);
            }
        }

        private static Dictionary<int, string> ReadTeamNames(SaveReader r)
        {
            var names = new Dictionary<int, string>();
            int count = r.Int();
            for (int i = 0; i < count; i++)
            {
                names[r.Int()] = r.String();
            }

            return names;
        }

        // ───────────────────────── 선수 ─────────────────────────

        private static void WritePlayers(SaveWriter w, PlayerDirectory players)
        {
            var list = new List<Player>(players.All);
            w.Int(list.Count);
            foreach (Player p in list)
            {
                w.Int(p.Id);
                w.String(p.Name);
                w.Int((int)p.Bats);
                w.Int((int)p.Throws);
                w.Int((int)p.PrimaryPosition);

                BatterRatings b = p.Batting;
                foreach (int v in new[] { b.Contact, b.Power, b.Eye, b.AvoidK, b.Gap, b.Speed, b.PullTendency, b.StealJump, b.Bunt, b.BaserunningInstinct })
                {
                    w.Int(v);
                }

                w.Bool(p.Pitching != null);
                if (p.Pitching != null)
                {
                    PitcherRatings pr = p.Pitching;
                    w.Int(pr.Velocity);
                    w.Int(pr.Control);
                    w.Int(pr.Stamina);
                    w.Int(pr.HoldRunners);
                    w.Int(pr.Repertoire.Count);
                    foreach (PitchRating pitch in pr.Repertoire)
                    {
                        w.Int((int)pitch.Type);
                        w.Int(pitch.Stuff);
                        w.Double(pitch.Usage);
                    }
                }

                FielderRatings f = p.Fielding;
                foreach (int v in new[] { f.Range, f.Hands, f.ArmStrength, f.ArmAccuracy, f.Blocking, f.Framing })
                {
                    w.Int(v);
                }

                w.IntList(f.Proficiency);
            }
        }

        private static PlayerDirectory ReadPlayers(SaveReader r)
        {
            var directory = new PlayerDirectory();
            int count = r.Int();
            for (int i = 0; i < count; i++)
            {
                var p = new Player
                {
                    Id = r.Int(),
                    Name = r.String(),
                    Bats = (BatSide)r.Int(),
                    Throws = (Hand)r.Int(),
                    PrimaryPosition = (Position)r.Int(),
                };
                p.Batting = new BatterRatings
                {
                    Contact = r.Int(),
                    Power = r.Int(),
                    Eye = r.Int(),
                    AvoidK = r.Int(),
                    Gap = r.Int(),
                    Speed = r.Int(),
                    PullTendency = r.Int(),
                    StealJump = r.Int(),
                    Bunt = r.Int(),
                    BaserunningInstinct = r.Int(),
                };

                if (r.Bool())
                {
                    var pr = new PitcherRatings
                    {
                        Velocity = r.Int(),
                        Control = r.Int(),
                        Stamina = r.Int(),
                        HoldRunners = r.Int(),
                    };
                    int pitches = r.Int();
                    for (int k = 0; k < pitches; k++)
                    {
                        pr.Repertoire.Add(new PitchRating((PitchType)r.Int(), r.Int(), r.Double()));
                    }

                    p.Pitching = pr;
                }

                p.Fielding = new FielderRatings
                {
                    Range = r.Int(),
                    Hands = r.Int(),
                    ArmStrength = r.Int(),
                    ArmAccuracy = r.Int(),
                    Blocking = r.Int(),
                    Framing = r.Int(),
                    Proficiency = r.IntList().ToArray(),
                };
                directory.Add(p);
            }

            return directory;
        }

        // ───────────────────────── 경기 상태 ─────────────────────────

        private static void WriteState(SaveWriter w, GameState s)
        {
            w.Int(s.GameId);
            w.Int((int)s.Phase);
            w.Int(s.Inning);
            w.Bool(s.IsTopHalf);
            w.Int(s.Outs);
            w.Int(s.Balls);
            w.Int(s.Strikes);
            for (int b = 0; b < 3; b++)
            {
                BaseRunner runner = s.Bases[b];
                w.Bool(runner != null);
                if (runner != null)
                {
                    w.Int(runner.PlayerId);
                    w.Int(runner.ResponsiblePitcherId);
                }
            }

            WriteTeam(w, s.Away);
            WriteTeam(w, s.Home);
            w.Int(s.PlateAppearanceNumber);
            w.Int(s.PitchNumberInPlateAppearance);
            w.Int(s.EventSequence);
            w.Int(s.PlateAppearancesThisHalf);
            w.Int(s.StealFromBase);
            w.Int((int)s.BuntSign);
            WritePitchInFlight(w, s.CurrentPitch);
            w.ULong(s.Random.State);
            w.ULong(s.Random.Increment);

            w.Int(s.Log.Count);
            foreach (GameEvent e in s.Log)
            {
                WriteEvent(w, e);
            }
        }

        private static GameState ReadState(SaveReader r)
        {
            var s = new GameState
            {
                GameId = r.Int(),
                Phase = (GamePhase)r.Int(),
                Inning = r.Int(),
                IsTopHalf = r.Bool(),
                Outs = r.Int(),
                Balls = r.Int(),
                Strikes = r.Int(),
            };
            for (int b = 0; b < 3; b++)
            {
                s.Bases[b] = r.Bool() ? new BaseRunner(r.Int(), r.Int()) : null;
            }

            s.Away = ReadTeam(r);
            s.Home = ReadTeam(r);
            s.PlateAppearanceNumber = r.Int();
            s.PitchNumberInPlateAppearance = r.Int();
            s.EventSequence = r.Int();
            s.PlateAppearancesThisHalf = r.Int();
            s.StealFromBase = r.Int();
            s.BuntSign = (BuntType)r.Int();
            s.CurrentPitch = ReadPitchInFlight(r);
            ulong rngState = r.ULong();
            ulong rngIncrement = r.ULong();
            s.Random = Pcg32Random.FromState(rngState, rngIncrement);

            int events = r.Int();
            for (int i = 0; i < events; i++)
            {
                s.Log.Add(ReadEvent(r));
            }

            return s;
        }

        private static void WriteTeam(SaveWriter w, TeamGameState t)
        {
            w.Int(t.TeamId);
            w.Int(t.Lineup.Count);
            foreach (LineupSlot slot in t.Lineup)
            {
                w.Int(slot.PlayerId);
                w.Int((int)slot.Position);
            }

            w.Int(t.NextBatterIndex);
            w.Int(t.CurrentPitcherId);
            w.Int(t.Pitchers.Count);
            foreach (PitcherGameState p in t.Pitchers)
            {
                w.Int(p.PlayerId);
                w.Bool(p.IsStarter);
                w.Int(p.PitchCount);
                w.Int(p.BattersFaced);
                w.Int(p.OutsRecorded);
                w.Int(p.RunsAllowed);
            }

            w.IntList(t.AvailableBullpen);
            w.Int(t.CloserId);
            w.IntList(t.SetupIds);
            w.IntList(t.Bench);
            w.IntList(t.Removed);
            w.Int(t.Runs);
            w.Int(t.Hits);
            w.Int(t.Errors);
        }

        private static TeamGameState ReadTeam(SaveReader r)
        {
            var t = new TeamGameState { TeamId = r.Int() };
            int slots = r.Int();
            for (int i = 0; i < slots; i++)
            {
                t.Lineup.Add(new LineupSlot(r.Int(), (Position)r.Int()));
            }

            t.NextBatterIndex = r.Int();
            t.CurrentPitcherId = r.Int();
            int pitchers = r.Int();
            for (int i = 0; i < pitchers; i++)
            {
                t.Pitchers.Add(new PitcherGameState
                {
                    PlayerId = r.Int(),
                    IsStarter = r.Bool(),
                    PitchCount = r.Int(),
                    BattersFaced = r.Int(),
                    OutsRecorded = r.Int(),
                    RunsAllowed = r.Int(),
                });
            }

            t.AvailableBullpen = r.IntList();
            t.CloserId = r.Int();
            t.SetupIds = r.IntList();
            t.Bench = r.IntList();
            t.Removed = r.IntList();
            t.Runs = r.Int();
            t.Hits = r.Int();
            t.Errors = r.Int();
            return t;
        }

        private static void WriteLocation(SaveWriter w, PlateLocation location)
        {
            w.Double(location.X);
            w.Double(location.Z);
        }

        private static PlateLocation ReadLocation(SaveReader r)
        {
            return new PlateLocation(r.Double(), r.Double());
        }

        private static void WritePitchInFlight(SaveWriter w, PitchInFlight pitch)
        {
            w.Bool(pitch != null);
            if (pitch == null)
            {
                return;
            }

            w.Int((int)pitch.Call.Type);
            WriteLocation(w, pitch.Call.Target);
            w.NullableDouble(pitch.Call.ReleaseQuality);

            ExecutedPitch e = pitch.Executed;
            w.Int((int)e.Type);
            WriteLocation(w, e.Target);
            WriteLocation(w, e.Actual);
            w.Double(e.VelocityKmh);
            w.Double(e.EffectiveStuffZ);
            w.Int((int)e.Region);
            w.Bool(e.IsInZone);

            WriteLocation(w, pitch.Perceived.Location);
            w.Int((int)pitch.Perceived.Region);
            w.Bool(pitch.Perceived.IsInZone);
            w.Bool(pitch.ByHuman);
        }

        private static PitchInFlight ReadPitchInFlight(SaveReader r)
        {
            if (!r.Bool())
            {
                return null;
            }

            var call = new PitchCall((PitchType)r.Int(), ReadLocation(r), r.NullableDouble());
            var executed = new ExecutedPitch
            {
                Type = (PitchType)r.Int(),
                Target = ReadLocation(r),
                Actual = ReadLocation(r),
                VelocityKmh = r.Double(),
                EffectiveStuffZ = r.Double(),
                Region = (AttackRegion)r.Int(),
                IsInZone = r.Bool(),
            };
            var perceived = new PerceivedPitch
            {
                Location = ReadLocation(r),
                Region = (AttackRegion)r.Int(),
                IsInZone = r.Bool(),
            };
            return new PitchInFlight { Call = call, Executed = executed, Perceived = perceived, ByHuman = r.Bool() };
        }

        // ───────────────────────── 이벤트 ─────────────────────────

        private static void WriteMovements(SaveWriter w, List<RunnerMovement> moves)
        {
            w.Int(moves.Count);
            foreach (RunnerMovement m in moves)
            {
                w.Int(m.PlayerId);
                w.Int(m.FromBase);
                w.Int(m.ToBase);
                w.Bool(m.IsOut);
            }
        }

        private static List<RunnerMovement> ReadMovements(SaveReader r)
        {
            int count = r.Int();
            var moves = new List<RunnerMovement>(count);
            for (int i = 0; i < count; i++)
            {
                moves.Add(new RunnerMovement(r.Int(), r.Int(), r.Int(), r.Bool()));
            }

            return moves;
        }

        private static void WriteEventBase(SaveWriter w, GameEvent e)
        {
            w.Int(e.GameId);
            w.Int(e.Sequence);
            w.Int(e.Inning);
            w.Bool(e.IsTopHalf);
        }

        private static void ReadEventBase(SaveReader r, GameEvent e)
        {
            e.GameId = r.Int();
            e.Sequence = r.Int();
            e.Inning = r.Int();
            e.IsTopHalf = r.Bool();
        }

        private static void WriteEvent(SaveWriter w, GameEvent e)
        {
            switch (e)
            {
                case PitchEvent p:
                    w.Int(PitchEventTag);
                    WriteEventBase(w, p);
                    WritePitchEvent(w, p);
                    break;
                case SubstitutionEvent s:
                    w.Int(SubstitutionEventTag);
                    WriteEventBase(w, s);
                    w.Int((int)s.Kind);
                    w.Int((int)s.Side);
                    w.Int(s.OutgoingPlayerId);
                    w.Int(s.IncomingPlayerId);
                    w.Int((int)s.Position);
                    w.Int(s.OutsAtChange);
                    w.Bool(s.ByHuman);
                    break;
                case IntentionalWalkEvent i:
                    w.Int(IntentionalWalkEventTag);
                    WriteEventBase(w, i);
                    w.Int(i.PlateAppearanceNumber);
                    w.Int(i.BatterId);
                    w.Int(i.PitcherId);
                    w.Int(i.OutsBefore);
                    w.Bool(i.ByHuman);
                    WriteMovements(w, i.RunnerMovements);
                    w.Int(i.RunsScored);
                    w.Int(i.AwayScoreAfter);
                    w.Int(i.HomeScoreAfter);
                    break;
                default:
                    throw new SaveFormatException("저장할 수 없는 이벤트 종류: " + e.GetType().Name);
            }
        }

        private static GameEvent ReadEvent(SaveReader r)
        {
            int tag = r.Int();
            switch (tag)
            {
                case PitchEventTag:
                {
                    var p = new PitchEvent();
                    ReadEventBase(r, p);
                    ReadPitchEvent(r, p);
                    return p;
                }

                case SubstitutionEventTag:
                {
                    var s = new SubstitutionEvent();
                    ReadEventBase(r, s);
                    s.Kind = (SubstitutionKind)r.Int();
                    s.Side = (TeamSide)r.Int();
                    s.OutgoingPlayerId = r.Int();
                    s.IncomingPlayerId = r.Int();
                    s.Position = (Position)r.Int();
                    s.OutsAtChange = r.Int();
                    s.ByHuman = r.Bool();
                    return s;
                }

                case IntentionalWalkEventTag:
                {
                    var i = new IntentionalWalkEvent();
                    ReadEventBase(r, i);
                    i.PlateAppearanceNumber = r.Int();
                    i.BatterId = r.Int();
                    i.PitcherId = r.Int();
                    i.OutsBefore = r.Int();
                    i.ByHuman = r.Bool();
                    i.RunnerMovements = ReadMovements(r);
                    i.RunsScored = r.Int();
                    i.AwayScoreAfter = r.Int();
                    i.HomeScoreAfter = r.Int();
                    return i;
                }

                default:
                    throw new SaveFormatException("알 수 없는 이벤트 태그: " + tag);
            }
        }

        private static void WritePitchEvent(SaveWriter w, PitchEvent e)
        {
            foreach (int v in new[]
            {
                e.PlateAppearanceNumber, e.PitchNumberInPlateAppearance, e.BatterId, e.PitcherId, e.CatcherId,
                (int)e.BattingHand, (int)e.PitcherHand, e.BallsBefore, e.StrikesBefore, e.OutsBefore,
                e.RunnerOnFirst, e.RunnerOnSecond, e.RunnerOnThird, e.AwayScoreBefore, e.HomeScoreBefore, (int)e.PitchType,
            })
            {
                w.Int(v);
            }

            foreach (double v in new[] { e.VelocityKmh, e.TargetX, e.TargetZ, e.PlateX, e.PlateZ, e.PerceivedX, e.PerceivedZ })
            {
                w.Double(v);
            }

            w.Int((int)e.Region);
            foreach (bool v in new[] { e.IsInZone, e.PitchByHuman, e.SwingByHuman, e.Swung, e.IsBunt })
            {
                w.Bool(v);
            }

            w.Int((int)e.Result);
            w.Bool(e.BattedBall != null);
            if (e.BattedBall != null)
            {
                BattedBallData b = e.BattedBall;
                foreach (double v in new[] { b.ExitVelocityKmh, b.LaunchAngleDeg, b.SprayAngleDeg, b.EndX, b.EndY, b.HangTimeS, b.DistanceM })
                {
                    w.Double(v);
                }

                w.Int((int)b.Type);
                w.Bool(b.IsSolid);
                w.NullableInt(b.FieldedBy.HasValue ? (int)b.FieldedBy.Value : (int?)null);
                w.Bool(b.HasLanding);
                foreach (double v in new[] { b.LandingX, b.LandingY, b.LandingTimeS, b.FieldedTimeS, b.FielderArrivalS })
                {
                    w.Double(v);
                }
            }

            w.NullableInt(e.PlateAppearanceOutcome.HasValue ? (int)e.PlateAppearanceOutcome.Value : (int?)null);
            WriteMovements(w, e.RunnerMovements);
            w.Int(e.StealFromBase);
            w.Int(e.StealRunnerId);
            foreach (bool v in new[]
            {
                e.StealSucceeded, e.IsWildPitch, e.IsPassedBall, e.DroppedThirdStrike, e.BatterReachedOnDroppedThirdStrike,
                e.IsError, e.RunsNullified,
            })
            {
                w.Bool(v);
            }

            foreach (int v in new[] { e.RunsScored, e.RunsBattedIn, e.OutsAfter, e.AwayScoreAfter, e.HomeScoreAfter })
            {
                w.Int(v);
            }
        }

        private static void ReadPitchEvent(SaveReader r, PitchEvent e)
        {
            e.PlateAppearanceNumber = r.Int();
            e.PitchNumberInPlateAppearance = r.Int();
            e.BatterId = r.Int();
            e.PitcherId = r.Int();
            e.CatcherId = r.Int();
            e.BattingHand = (Hand)r.Int();
            e.PitcherHand = (Hand)r.Int();
            e.BallsBefore = r.Int();
            e.StrikesBefore = r.Int();
            e.OutsBefore = r.Int();
            e.RunnerOnFirst = r.Int();
            e.RunnerOnSecond = r.Int();
            e.RunnerOnThird = r.Int();
            e.AwayScoreBefore = r.Int();
            e.HomeScoreBefore = r.Int();
            e.PitchType = (PitchType)r.Int();

            e.VelocityKmh = r.Double();
            e.TargetX = r.Double();
            e.TargetZ = r.Double();
            e.PlateX = r.Double();
            e.PlateZ = r.Double();
            e.PerceivedX = r.Double();
            e.PerceivedZ = r.Double();

            e.Region = (AttackRegion)r.Int();
            e.IsInZone = r.Bool();
            e.PitchByHuman = r.Bool();
            e.SwingByHuman = r.Bool();
            e.Swung = r.Bool();
            e.IsBunt = r.Bool();

            e.Result = (PitchResult)r.Int();
            if (r.Bool())
            {
                var b = new BattedBallData
                {
                    ExitVelocityKmh = r.Double(),
                    LaunchAngleDeg = r.Double(),
                    SprayAngleDeg = r.Double(),
                    EndX = r.Double(),
                    EndY = r.Double(),
                    HangTimeS = r.Double(),
                    DistanceM = r.Double(),
                    Type = (BattedBallType)r.Int(),
                    IsSolid = r.Bool(),
                };
                int? fielder = r.NullableInt();
                b.FieldedBy = fielder.HasValue ? (Position)fielder.Value : (Position?)null;
                if (r.Version >= 2)
                {
                    b.HasLanding = r.Bool();
                    b.LandingX = r.Double();
                    b.LandingY = r.Double();
                    b.LandingTimeS = r.Double();
                    b.FieldedTimeS = r.Double();
                    b.FielderArrivalS = r.Double();
                }

                e.BattedBall = b;
            }

            int? outcome = r.NullableInt();
            e.PlateAppearanceOutcome = outcome.HasValue ? (PlateAppearanceOutcome)outcome.Value : (PlateAppearanceOutcome?)null;
            e.RunnerMovements = ReadMovements(r);
            e.StealFromBase = r.Int();
            e.StealRunnerId = r.Int();
            e.StealSucceeded = r.Bool();
            e.IsWildPitch = r.Bool();
            e.IsPassedBall = r.Bool();
            e.DroppedThirdStrike = r.Bool();
            e.BatterReachedOnDroppedThirdStrike = r.Bool();
            e.IsError = r.Bool();
            e.RunsNullified = r.Bool();
            e.RunsScored = r.Int();
            e.RunsBattedIn = r.Int();
            e.OutsAfter = r.Int();
            e.AwayScoreAfter = r.Int();
            e.HomeScoreAfter = r.Int();
        }
    }
}
