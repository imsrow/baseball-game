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

        /// <summary>도루: 출발 베이스 (1 또는 2)</summary>
        public int FromBase { get; set; }

        /// <summary>번트 사인 종류</summary>
        public BuntType BuntType { get; set; }

        public static ManagerAction Steal(int fromBase)
        {
            return new ManagerAction { Type = ManagerActionType.StealAttempt, FromBase = fromBase };
        }

        public static ManagerAction IntentionalWalk()
        {
            return new ManagerAction { Type = ManagerActionType.IntentionalWalk };
        }

        public static ManagerAction PinchHit(int incomingPlayerId, int outgoingPlayerId)
        {
            return new ManagerAction
            {
                Type = ManagerActionType.PinchHitter,
                IncomingPlayerId = incomingPlayerId,
                OutgoingPlayerId = outgoingPlayerId,
            };
        }

        /// <param name="fromBase">대주자가 들어갈 베이스 (1~3)</param>
        public static ManagerAction PinchRun(int incomingPlayerId, int fromBase)
        {
            return new ManagerAction { Type = ManagerActionType.PinchRunner, IncomingPlayerId = incomingPlayerId, FromBase = fromBase };
        }

        public static ManagerAction DefensiveSub(int incomingPlayerId, int outgoingPlayerId)
        {
            return new ManagerAction
            {
                Type = ManagerActionType.DefensiveSubstitution,
                IncomingPlayerId = incomingPlayerId,
                OutgoingPlayerId = outgoingPlayerId,
            };
        }

        public static ManagerAction Bunt(BuntType type)
        {
            return new ManagerAction { Type = ManagerActionType.BuntSign, BuntType = type };
        }

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
