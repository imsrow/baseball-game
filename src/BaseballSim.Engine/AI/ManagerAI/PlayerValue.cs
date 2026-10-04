using BaseballSim.Engine.Config;
using BaseballSim.Engine.Fielding;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.AI.ManagerAI
{
    /// <summary>
    /// 감독 판단용 선수 가치 (단순 합산)
    /// </summary>
    public static class PlayerValue
    {
        /// <summary>타격 가치 z = (컨택 + 파워 + 0.5·선구안 + 0.5·삼진회피) / 3</summary>
        public static double BattingZ(BatterRatings b)
        {
            return (ScoutScale.ToZ(b.Contact) + ScoutScale.ToZ(b.Power)
                + 0.5 * ScoutScale.ToZ(b.Eye) + 0.5 * ScoutScale.ToZ(b.AvoidK)) / 3.0;
        }

        /// <summary>상대 투수 손을 반영한 타격 가치 (반대 손이면 보너스, 같은 손이면 감점)</summary>
        public static double BattingZAgainst(Player batter, Hand pitcherThrows, ManagerAiConfig config)
        {
            bool sameHand = batter.BattingHandAgainst(pitcherThrows) == pitcherThrows;
            return BattingZ(batter.Batting) + (sameHand ? -config.PlatoonValueZ : config.PlatoonValueZ);
        }

        /// <summary>포지션 실효 수비력 (범위·포구·송구 강도 평균, 숙련도 반영)</summary>
        public static double DefenseAt(Player player, Position position, DefenseConfig config)
        {
            FielderRatings f = player.Fielding;
            int proficiency = f.GetProficiency(position);
            return (DefenseCalculator.EffectiveRating(f.Range, proficiency, config)
                + DefenseCalculator.EffectiveRating(f.Hands, proficiency, config)
                + DefenseCalculator.EffectiveRating(f.ArmStrength, proficiency, config)) / 3.0;
        }
    }
}
