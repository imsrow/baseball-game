using System.Collections.Generic;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Ratings;
using BaseballSim.Engine.Teams;

namespace BaseballSim.BalanceProbe
{
    /// <summary>
    /// 비교용 평균(50) 능력치 두 팀. 능력치 차이를 빼고 투구·타격 방식의 영향만 보기 위함이다.
    /// </summary>
    public static class ProbeTeams
    {
        private const int FielderProficiency = 60;
        private const int PitcherProficiency = 55;
        private const int StarterStamina = 60;
        private const int RelieverStamina = 35;
        private const int PitcherCount = 7;
        private const int BenchCount = 3;

        private static readonly Position[] Lineup =
        {
            Position.CenterField, Position.SecondBase, Position.FirstBase, Position.DesignatedHitter, Position.ThirdBase,
            Position.RightField, Position.LeftField, Position.Catcher, Position.Shortstop,
        };

        public static GameSetup Create(int gameId)
        {
            return new GameSetup { GameId = gameId, Away = Team(1, 1000), Home = Team(2, 2000) };
        }

        private static GameTeamSetup Team(int teamId, int idBase)
        {
            var team = new Team { Id = teamId, Name = "T" + teamId };
            var setup = new GameTeamSetup { Team = team };
            for (int i = 0; i < Lineup.Length; i++)
            {
                var hitter = new Player { Id = idBase + i, Name = "H" + i, PrimaryPosition = Lineup[i] };
                if (Lineup[i] != Position.DesignatedHitter)
                {
                    hitter.Fielding.SetProficiency(Lineup[i], FielderProficiency);
                }

                team.Roster.Add(hitter);
                setup.Lineup.Add(new LineupSlot(hitter.Id, Lineup[i]));
            }

            for (int i = 0; i < PitcherCount; i++)
            {
                var pitcher = new Player
                {
                    Id = idBase + 50 + i,
                    Name = "P" + i,
                    PrimaryPosition = Position.Pitcher,
                    Pitching = new PitcherRatings
                    {
                        Stamina = i == 0 ? StarterStamina : RelieverStamina,
                        Repertoire = new List<PitchRating>
                        {
                            new PitchRating(PitchType.FourSeam, ScoutScale.Average, 0.55),
                            new PitchRating(PitchType.Slider, ScoutScale.Average, 0.25),
                            new PitchRating(PitchType.Changeup, ScoutScale.Average, 0.20),
                        },
                    },
                };
                pitcher.Fielding.SetProficiency(Position.Pitcher, PitcherProficiency);
                team.Roster.Add(pitcher);
                if (i == 0)
                {
                    setup.StartingPitcherId = pitcher.Id;
                }
                else
                {
                    setup.Bullpen.Add(pitcher.Id);
                }
            }

            setup.CloserId = setup.Bullpen[setup.Bullpen.Count - 1];
            setup.SetupIds.Add(setup.Bullpen[setup.Bullpen.Count - 2]);
            for (int i = 0; i < BenchCount; i++)
            {
                var bench = new Player { Id = idBase + 20 + i, Name = "B" + i, PrimaryPosition = Position.Catcher };
                bench.Fielding.SetProficiency(Position.Catcher, FielderProficiency);
                team.Roster.Add(bench);
                setup.Bench.Add(bench.Id);
            }

            return setup;
        }
    }
}
