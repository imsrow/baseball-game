using System.Collections.Generic;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Ratings;
using BaseballSim.Engine.Teams;

namespace BaseballSim.Engine.Tests
{
    /// <summary>
    /// 테스트용 평균(50) 능력치 팀과 경기 구성
    /// </summary>
    public static class TestData
    {
        private static readonly Position[] LineupPositions =
        {
            Position.CenterField, Position.SecondBase, Position.FirstBase, Position.DesignatedHitter, Position.ThirdBase,
            Position.RightField, Position.LeftField, Position.Catcher, Position.Shortstop,
        };

        private static readonly Position[] BenchPositions = { Position.Catcher, Position.Shortstop, Position.CenterField };

        public static GameSetup AverageGame(int gameId = 1)
        {
            return new GameSetup
            {
                GameId = gameId,
                Away = AverageTeam(1, 1000),
                Home = AverageTeam(2, 2000),
            };
        }

        public static GameTeamSetup AverageTeam(int teamId, int idBase)
        {
            var team = new Team { Id = teamId, Name = "테스트" + teamId };
            var setup = new GameTeamSetup { Team = team };
            for (int i = 0; i < LineupPositions.Length; i++)
            {
                Player p = Hitter(idBase + i, LineupPositions[i]);
                team.Roster.Add(p);
                setup.Lineup.Add(new LineupSlot(p.Id, LineupPositions[i]));
            }

            Player starter = Pitcher(idBase + 50, 60);
            team.Roster.Add(starter);
            setup.StartingPitcherId = starter.Id;
            for (int i = 0; i < 6; i++)
            {
                Player reliever = Pitcher(idBase + 60 + i, 35);
                team.Roster.Add(reliever);
                setup.Bullpen.Add(reliever.Id);
            }

            setup.CloserId = setup.Bullpen[5];
            setup.SetupIds.Add(setup.Bullpen[4]);

            foreach (Position position in BenchPositions)
            {
                Player p = Hitter(idBase + 20 + setup.Bench.Count, position);
                team.Roster.Add(p);
                setup.Bench.Add(p.Id);
            }

            return setup;
        }

        public static Player Hitter(int id, Position position)
        {
            var p = new Player { Id = id, Name = "타자" + id, PrimaryPosition = position };
            if (position != Position.DesignatedHitter)
            {
                p.Fielding.SetProficiency(position, 60);
            }

            return p;
        }

        public static Player Pitcher(int id, int stamina)
        {
            var p = new Player
            {
                Id = id,
                Name = "투수" + id,
                PrimaryPosition = Position.Pitcher,
                Pitching = new PitcherRatings
                {
                    Stamina = stamina,
                    Repertoire = new List<PitchRating>
                    {
                        new PitchRating(PitchType.FourSeam, 50, 0.55),
                        new PitchRating(PitchType.Slider, 50, 0.25),
                        new PitchRating(PitchType.Changeup, 50, 0.20),
                    },
                },
            };
            p.Fielding.SetProficiency(Position.Pitcher, 55);
            return p;
        }
    }
}
