using System;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Probability;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 포수가 공을 놓치는 확률. 원바운드·크게 빠진 공을 놓치면 폭투, 정상 범위 공을 놓치면 포일.
    /// </summary>
    public sealed class PassedBallModel
    {
        private readonly LeagueConfig _config;

        public PassedBallModel(LeagueConfig config)
        {
            _config = config;
        }

        /// <summary>폭투성 공(원바운드 또는 크게 빠짐)인지</summary>
        public bool IsWildLocation(PlateLocation location)
        {
            StrikeZoneConfig zone = _config.StrikeZone;
            PassedBallConfig pc = _config.PassedBall;
            return location.Z < zone.BottomM - pc.DirtBelowZoneM
                || Math.Abs(location.X) > zone.HalfWidthM + pc.WideOutsideZoneM;
        }

        public double MissProbability(PlateLocation location, Player catcher)
        {
            PassedBallConfig pc = _config.PassedBall;
            int proficiency = catcher.Fielding.GetProficiency(Position.Catcher);
            double blocking = DefenseCalculator.EffectiveRating(catcher.Fielding.Blocking, proficiency, _config.Defense);
            double baseRate = IsWildLocation(location) ? pc.WildPitchRate : pc.PassedBallRate;
            return LogOdds.Shift(baseRate, -pc.BlockingBeta * ScoutScale.ToZ(blocking));
        }

        /// <summary>낫아웃 상황에서 타자가 1루에 살아갈 확률</summary>
        public double DroppedThirdStrikeReachProbability(BatterRatings batter)
        {
            PassedBallConfig pc = _config.PassedBall;
            return LogOdds.Shift(pc.DroppedThirdStrikeReachRate, pc.DroppedThirdStrikeSpeedBeta * ScoutScale.ToZ(batter.Speed));
        }

        /// <summary>낫아웃 규칙: 1루가 비었거나 2아웃이면 타자가 뛸 수 있다</summary>
        public static bool BatterMayRunOnDroppedThirdStrike(bool firstBaseOccupied, int outsBefore, int outsPerHalfInning)
        {
            return !firstBaseOccupied || outsBefore == outsPerHalfInning - 1;
        }
    }
}
