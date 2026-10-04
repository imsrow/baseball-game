using BaseballSim.Engine.Players;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 플레이에 관여하는 주자(또는 타자주자)
    /// </summary>
    public sealed class RunnerProfile
    {
        public RunnerProfile(int playerId, BatterRatings ratings, Hand battingHand)
        {
            PlayerId = playerId;
            Ratings = ratings;
            BattingHand = battingHand;
        }

        public int PlayerId { get; }

        public BatterRatings Ratings { get; }

        /// <summary>타자주자일 때 타석 방향 (좌타자가 1루에 더 빠르다)</summary>
        public Hand BattingHand { get; }
    }
}
