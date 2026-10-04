using BaseballSim.Engine.Players;

namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 감독 지시 한 건
    /// </summary>
    public sealed class ManagerAction
    {
        public ManagerActionType Type { get; set; }

        /// <summary>들어오는 선수</summary>
        public int IncomingPlayerId { get; set; } = -1;

        /// <summary>나가는 선수 (투수 교체는 현재 투수)</summary>
        public int OutgoingPlayerId { get; set; } = -1;

        public Position Position { get; set; }

        public static ManagerAction PitchingChange(int incomingPitcherId)
        {
            return new ManagerAction
            {
                Type = ManagerActionType.PitchingChange,
                IncomingPlayerId = incomingPitcherId,
                Position = Position.Pitcher,
            };
        }
    }
}
