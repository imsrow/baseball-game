using System;
using System.Collections.Generic;
using System.Linq;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Randomness;
using BaseballSim.Engine.Ratings;
using BaseballSim.Engine.Teams;

namespace BaseballSim.Harness
{
    /// <summary>
    /// 가상 리그 생성. 능력치는 평균 50, 표준편차 10 (포지션별 수비 도구 평균만 다름).
    /// 로스터: 선발 야수 9 + 벤치 5(포수 1, 내야 2, 외야 2) + 선발투수 5 + 불펜 8 = 27명
    /// </summary>
    public sealed class LeagueGenerator
    {
        // ── 생성 분포 (하네스 전용 값) ──
        private const double RatingMean = 50;
        private const double RatingSd = 10;
        private const double ToolSd = 8;
        private const double PrimaryProficiencyMean = 62;
        private const double PrimaryProficiencySd = 7;
        private const double StarterStaminaMean = 62;
        private const double RelieverStaminaMean = 35;
        private const double StaminaSd = 7;
        private const double LeftyPitcherRate = 0.28;
        private const double LeftyBatterRate = 0.32;
        private const double SwitchBatterRate = 0.10;
        private const int StartersPerTeam = 5;
        private const int RelieversPerTeam = 8;

        // 포지션별 수비 도구 평균 (범위, 포구, 송구 강도, 송구 정확도)
        private static readonly Dictionary<Position, double[]> ToolMeans = new Dictionary<Position, double[]>
        {
            { Position.Catcher, new double[] { 45, 50, 55, 50 } },
            { Position.FirstBase, new double[] { 42, 52, 45, 48 } },
            { Position.SecondBase, new double[] { 55, 52, 48, 52 } },
            { Position.ThirdBase, new double[] { 50, 52, 57, 50 } },
            { Position.Shortstop, new double[] { 58, 53, 57, 52 } },
            { Position.LeftField, new double[] { 48, 48, 47, 48 } },
            { Position.CenterField, new double[] { 58, 50, 52, 50 } },
            { Position.RightField, new double[] { 50, 48, 57, 50 } },
            { Position.DesignatedHitter, new double[] { 40, 42, 45, 45 } },
        };

        // 주 포지션별 부 포지션과 숙련도 감소폭
        private static readonly Dictionary<Position, (Position, int)[]> Secondary = new Dictionary<Position, (Position, int)[]>
        {
            { Position.Catcher, new[] { (Position.FirstBase, 20) } },
            { Position.FirstBase, new[] { (Position.LeftField, 20), (Position.RightField, 22) } },
            { Position.SecondBase, new[] { (Position.Shortstop, 10), (Position.ThirdBase, 10) } },
            { Position.ThirdBase, new[] { (Position.FirstBase, 8), (Position.SecondBase, 12) } },
            { Position.Shortstop, new[] { (Position.SecondBase, 5), (Position.ThirdBase, 6) } },
            { Position.LeftField, new[] { (Position.RightField, 5), (Position.FirstBase, 15) } },
            { Position.CenterField, new[] { (Position.LeftField, 3), (Position.RightField, 3) } },
            { Position.RightField, new[] { (Position.LeftField, 3), (Position.FirstBase, 15) } },
            { Position.DesignatedHitter, new[] { (Position.FirstBase, 15), (Position.LeftField, 20) } },
        };

        private static readonly Position[] StarterPositions =
        {
            Position.Catcher, Position.FirstBase, Position.SecondBase, Position.ThirdBase, Position.Shortstop,
            Position.LeftField, Position.CenterField, Position.RightField, Position.DesignatedHitter,
        };

        private static readonly Position[] BenchPositions =
        {
            Position.Catcher, Position.Shortstop, Position.ThirdBase, Position.CenterField, Position.LeftField,
        };

        private static readonly PitchType[] SecondaryPitches =
        {
            PitchType.Slider, PitchType.Curveball, PitchType.Changeup, PitchType.Splitter, PitchType.Cutter, PitchType.Sinker,
        };

        private readonly IRandomSource _random;
        private readonly NameGenerator _names;
        private int _nextPlayerId = 1;

        public LeagueGenerator(ulong seed)
        {
            _random = new Pcg32Random(seed);
            _names = new NameGenerator(_random);
        }

        public List<LeagueTeam> Generate(SeasonConfig season)
        {
            var teams = new List<LeagueTeam>();
            for (int i = 0; i < season.TeamCount; i++)
            {
                teams.Add(GenerateTeam(i + 1, _names.TeamName(i)));
            }

            CenterRatings(teams);
            return teams;
        }

        /// <summary>
        /// 스카우트 스케일에서 50은 리그 평균이므로, 리그 안에서 각 능력치 평균이 정확히 50이 되도록 이동한다.
        /// 타격은 주전 야수, 투구는 전체 투수 기준.
        /// </summary>
        private static void CenterRatings(List<LeagueTeam> teams)
        {
            var hitters = teams.SelectMany(t => t.Lineup.Select(slot => t.Team.FindPlayer(slot.PlayerId))).ToList();
            var benchers = teams.SelectMany(t => t.Bench.Select(id => t.Team.FindPlayer(id))).ToList();
            var allHitters = hitters.Concat(benchers).ToList();
            Center(hitters, allHitters, p => p.Batting.Contact, (p, v) => p.Batting.Contact = v);
            Center(hitters, allHitters, p => p.Batting.Power, (p, v) => p.Batting.Power = v);
            Center(hitters, allHitters, p => p.Batting.Eye, (p, v) => p.Batting.Eye = v);
            Center(hitters, allHitters, p => p.Batting.AvoidK, (p, v) => p.Batting.AvoidK = v);
            Center(hitters, allHitters, p => p.Batting.Gap, (p, v) => p.Batting.Gap = v);
            Center(hitters, allHitters, p => p.Batting.Speed, (p, v) => p.Batting.Speed = v);

            var pitchers = teams.SelectMany(t => t.Team.Roster.Where(p => p.IsPitcher)).ToList();
            Center(pitchers, pitchers, p => p.Pitching.Velocity, (p, v) => p.Pitching.Velocity = v);
            Center(pitchers, pitchers, p => p.Pitching.Control, (p, v) => p.Pitching.Control = v);
            double stuffShift = RatingMean - pitchers.SelectMany(p => p.Pitching.Repertoire).Average(r => (double)r.Stuff);
            foreach (PitchRating pitch in pitchers.SelectMany(p => p.Pitching.Repertoire))
            {
                pitch.Stuff = ScoutScale.Clamp((int)Math.Round(pitch.Stuff + stuffShift));
            }
        }

        private static void Center(List<Player> reference, List<Player> targets, Func<Player, int> get, Action<Player, int> set)
        {
            double shift = RatingMean - reference.Average(p => (double)get(p));
            foreach (Player p in targets)
            {
                set(p, ScoutScale.Clamp((int)Math.Round(get(p) + shift)));
            }
        }

        private LeagueTeam GenerateTeam(int teamId, string name)
        {
            var team = new Team { Id = teamId, Name = name };
            var league = new LeagueTeam { Team = team };

            var starters = new List<Player>();
            foreach (Position position in StarterPositions)
            {
                Player p = PositionPlayer(position);
                team.Roster.Add(p);
                starters.Add(p);
            }

            foreach (Position position in BenchPositions)
            {
                Player p = PositionPlayer(position);
                if (position == Position.Shortstop || position == Position.ThirdBase)
                {
                    // 내야 유틸리티: 2B·SS·3B 모두 가능
                    p.Fielding.SetProficiency(Position.SecondBase, Math.Max(p.Fielding.GetProficiency(Position.SecondBase), Rating(55, 6)));
                    p.Fielding.SetProficiency(Position.Shortstop, Math.Max(p.Fielding.GetProficiency(Position.Shortstop), Rating(52, 6)));
                    p.Fielding.SetProficiency(Position.ThirdBase, Math.Max(p.Fielding.GetProficiency(Position.ThirdBase), Rating(55, 6)));
                }

                team.Roster.Add(p);
                league.Bench.Add(p.Id);
            }

            league.Lineup = BuildLineup(starters);

            for (int i = 0; i < StartersPerTeam; i++)
            {
                Player p = Pitcher(true);
                team.Roster.Add(p);
                league.Rotation.Add(p.Id);
            }

            var relievers = new List<Player>();
            for (int i = 0; i < RelieversPerTeam; i++)
            {
                Player p = Pitcher(false);
                team.Roster.Add(p);
                relievers.Add(p);
            }

            // 불펜은 낮은 등급부터 기용, 최고 투수는 마무리, 그다음 둘은 셋업
            league.Bullpen = relievers.OrderBy(PitcherQuality).Select(p => p.Id).ToList();
            league.CloserId = league.Bullpen[league.Bullpen.Count - 1];
            league.SetupIds = new List<int> { league.Bullpen[league.Bullpen.Count - 2], league.Bullpen[league.Bullpen.Count - 3] };
            return league;
        }

        private static List<LineupSlot> BuildLineup(List<Player> starters)
        {
            List<Player> byBat = starters.OrderByDescending(BatScore).ToList();
            // 타순 배치: 2번째로 좋은 타자 1번, 3번째 2번, 최고 3번, 4번째 4번, 이후 순서대로
            int[] order = { 1, 2, 0, 3, 4, 5, 6, 7, 8 };
            return order.Select(i => new LineupSlot(byBat[i].Id, byBat[i].PrimaryPosition)).ToList();
        }

        private static double BatScore(Player p)
        {
            BatterRatings b = p.Batting;
            return 0.3 * b.Contact + 0.25 * b.Power + 0.2 * b.Eye + 0.15 * b.AvoidK + 0.1 * b.Gap;
        }

        private static double PitcherQuality(Player p)
        {
            PitcherRatings r = p.Pitching;
            return r.Velocity + r.Control + r.Repertoire.Average(x => (double)x.Stuff);
        }

        private Player PositionPlayer(Position position)
        {
            double speed = _random.Gaussian(RatingMean, RatingSd);
            double power = _random.Gaussian(RatingMean, RatingSd);
            var batting = new BatterRatings
            {
                Contact = Rating(RatingMean, RatingSd),
                Power = ScoutScale.Clamp((int)Math.Round(power)),
                Eye = Rating(RatingMean, RatingSd),
                AvoidK = Rating(RatingMean, RatingSd),
                Gap = Rating(RatingMean + 3 * ScoutScale.ToZ(power), 9),
                Speed = ScoutScale.Clamp((int)Math.Round(speed)),
                PullTendency = Rating(RatingMean, RatingSd),
                StealJump = Rating(RatingMean + 5 * ScoutScale.ToZ(speed), 8),
                Bunt = Rating(RatingMean, RatingSd),
                BaserunningInstinct = Rating(RatingMean, RatingSd),
            };

            double[] tools = ToolMeans[position];
            bool catcher = position == Position.Catcher;
            var fielding = new FielderRatings
            {
                Range = Rating(tools[0], ToolSd),
                Hands = Rating(tools[1], ToolSd),
                ArmStrength = Rating(tools[2], ToolSd),
                ArmAccuracy = Rating(tools[3], ToolSd),
                Blocking = catcher ? Rating(RatingMean, RatingSd) : 35,
                Framing = catcher ? Rating(RatingMean, RatingSd) : 35,
            };

            int primary = Rating(PrimaryProficiencyMean, PrimaryProficiencySd);
            if (position != Position.DesignatedHitter)
            {
                fielding.SetProficiency(position, primary);
            }

            foreach ((Position other, int drop) in Secondary[position])
            {
                int baseline = position == Position.DesignatedHitter ? 45 : primary;
                fielding.SetProficiency(other, ScoutScale.Clamp(baseline - drop + (int)Math.Round(_random.Gaussian(0, 4))));
            }

            bool rightOnly = position == Position.Catcher || position == Position.SecondBase
                || position == Position.ThirdBase || position == Position.Shortstop;
            double roll = _random.NextDouble();
            return new Player
            {
                Id = _nextPlayerId++,
                Name = _names.PlayerName(),
                Bats = roll < SwitchBatterRate ? BatSide.Switch : roll < SwitchBatterRate + LeftyBatterRate ? BatSide.Left : BatSide.Right,
                Throws = rightOnly || _random.NextDouble() >= 0.25 ? Hand.Right : Hand.Left,
                PrimaryPosition = position,
                Batting = batting,
                Fielding = fielding,
            };
        }

        private Player Pitcher(bool starter)
        {
            var pitching = new PitcherRatings
            {
                Velocity = Rating(RatingMean, RatingSd),
                Control = Rating(RatingMean, RatingSd),
                Stamina = Rating(starter ? StarterStaminaMean : RelieverStaminaMean, StaminaSd),
                HoldRunners = Rating(RatingMean, RatingSd),
            };

            PitchType fastball = _random.NextDouble() < 0.75 ? PitchType.FourSeam : PitchType.Sinker;
            double fastballUsage = starter ? 0.50 : 0.55;
            pitching.Repertoire.Add(new PitchRating(fastball, Rating(RatingMean, RatingSd), fastballUsage));

            int extra = starter ? 3 : 1 + _random.NextInt(2);
            var pool = SecondaryPitches.Where(t => t != fastball).ToList();
            var weights = new List<double>();
            for (int i = 0; i < extra; i++)
            {
                PitchType type = pool[_random.NextInt(pool.Count)];
                pool.Remove(type);
                weights.Add(0.5 + _random.NextDouble());
                pitching.Repertoire.Add(new PitchRating(type, Rating(RatingMean, RatingSd), 0));
            }

            double total = weights.Sum();
            for (int i = 0; i < extra; i++)
            {
                pitching.Repertoire[i + 1].Usage = (1.0 - fastballUsage) * weights[i] / total;
            }

            var fielding = new FielderRatings
            {
                Range = Rating(45, ToolSd),
                Hands = Rating(45, ToolSd),
                ArmStrength = Rating(50, ToolSd),
                ArmAccuracy = Rating(45, ToolSd),
                Blocking = 30,
                Framing = 30,
            };
            fielding.SetProficiency(Position.Pitcher, Rating(55, 6));

            return new Player
            {
                Id = _nextPlayerId++,
                Name = _names.PlayerName(),
                Bats = BatSide.Right,
                Throws = _random.NextDouble() < LeftyPitcherRate ? Hand.Left : Hand.Right,
                PrimaryPosition = Position.Pitcher,
                Batting = new BatterRatings { Contact = 25, Power = 25, Eye = 25, AvoidK = 25, Gap = 25 },
                Pitching = pitching,
                Fielding = fielding,
            };
        }

        private int Rating(double mean, double sd)
        {
            return ScoutScale.Clamp((int)Math.Round(_random.Gaussian(mean, sd)));
        }
    }
}
