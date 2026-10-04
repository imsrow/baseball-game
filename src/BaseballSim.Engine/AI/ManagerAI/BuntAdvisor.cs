using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Ratings;
using BaseballSim.Engine.State;

namespace BaseballSim.Engine.AI.ManagerAI
{
    /// <summary>
    /// 번트 사인 판단
    /// 희생번트: 무사, 1·2루 주자(3루 비어 있음), 약한 타자 (후반 접전이면 기준 완화)
    /// 기습번트: 주자 없음, 초구, 빠르고 번트 잘 대는 타자
    /// </summary>
    public static class BuntAdvisor
    {
        public static BuntType Advise(ManagerContext context)
        {
            GameState state = context.State;
            ManagerAiConfig mc = context.Config.ManagerAi;
            if (state.Strikes >= context.Config.Rules.StrikesForStrikeout - 1)
            {
                return BuntType.None;
            }

            Player batter = context.Players.Get(state.CurrentBatterId);
            BatterRatings b = batter.Batting;
            bool runnerToAdvance = state.Bases[0] != null || state.Bases[1] != null;

            if (state.Outs == 0 && runnerToAdvance && state.Bases[2] == null)
            {
                double batterZ = (ScoutScale.ToZ(b.Contact) + ScoutScale.ToZ(b.Power)) / 2.0;
                double limit = GameSituation.IsLateAndClose(context) ? mc.LateCloseSacrificeMaxBatterZ : mc.SacrificeMaxBatterZ;
                if (batterZ < limit && context.Random.NextDouble() < mc.SacrificeSignPerPitch)
                {
                    return BuntType.Sacrifice;
                }

                return BuntType.None;
            }

            bool basesEmpty = state.Bases[0] == null && state.Bases[1] == null && state.Bases[2] == null;
            if (basesEmpty && state.Balls == 0 && state.Strikes == 0
                && b.Speed >= mc.BuntForHitMinSpeed && b.Bunt >= mc.BuntForHitMinBunt
                && context.Random.NextDouble() < mc.BuntForHitSignRate)
            {
                return BuntType.ForHit;
            }

            return BuntType.None;
        }
    }
}
