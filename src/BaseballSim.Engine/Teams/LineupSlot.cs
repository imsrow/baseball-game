using BaseballSim.Engine.Players;

namespace BaseballSim.Engine.Teams
{
    /// <summary>
    /// 타순 한 자리: 선수와 수비 포지션
    /// </summary>
    public sealed class LineupSlot
    {
        public LineupSlot()
        {
        }

        public LineupSlot(int playerId, Position position)
        {
            PlayerId = playerId;
            Position = position;
        }

        public int PlayerId { get; set; }

        public Position Position { get; set; }

        public LineupSlot Clone()
        {
            return new LineupSlot(PlayerId, Position);
        }
    }
}
