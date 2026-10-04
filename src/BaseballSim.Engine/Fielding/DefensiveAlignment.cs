using System;
using System.Collections.Generic;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 현재 수비 배치 (9개 포지션의 FielderProfile)
    /// </summary>
    public sealed class DefensiveAlignment
    {
        private readonly FielderProfile[] _fielders;

        public DefensiveAlignment(FielderProfile[] fielders)
        {
            _fielders = fielders;
        }

        public FielderProfile Get(Position position)
        {
            return _fielders[(int)position];
        }

        public IEnumerable<FielderProfile> All => _fielders;

        /// <summary>
        /// 포지션별 선수로부터 수비 배치를 만든다. 투수는 수비 능력치가 기본값이면 Config 기본값을 쓴다.
        /// </summary>
        public static DefensiveAlignment Build(Func<Position, Player> playerAt, FieldGeometry field, LeagueConfig config)
        {
            var fielders = new FielderProfile[PositionInfo.Fielding.Length];
            foreach (Position position in PositionInfo.Fielding)
            {
                Player player = playerAt(position);
                FielderRatings f = player.Fielding;
                int prof = f.GetProficiency(position);
                DefenseConfig dc = config.Defense;
                fielders[(int)position] = new FielderProfile(
                    player.Id,
                    position,
                    field.FielderStart(position),
                    DefenseCalculator.EffectiveRating(f.Range, prof, dc),
                    DefenseCalculator.EffectiveRating(f.Hands, prof, dc),
                    DefenseCalculator.EffectiveRating(f.ArmStrength, prof, dc),
                    DefenseCalculator.EffectiveRating(f.ArmAccuracy, prof, dc),
                    config.Fielding);
            }

            return new DefensiveAlignment(fielders);
        }
    }
}
