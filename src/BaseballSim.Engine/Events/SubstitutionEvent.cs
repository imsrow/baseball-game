using BaseballSim.Engine.Players;
using BaseballSim.Engine.State;

namespace BaseballSim.Engine.Events
{
    /// <summary>
    /// 교체 기록
    /// </summary>
    public sealed class SubstitutionEvent : GameEvent
    {
        public SubstitutionKind Kind { get; set; }

        public TeamSide Side { get; set; }

        public int OutgoingPlayerId { get; set; }

        public int IncomingPlayerId { get; set; }

        public Position Position { get; set; }

        public int OutsAtChange { get; set; }

        public bool ByHuman { get; set; }
    }
}
