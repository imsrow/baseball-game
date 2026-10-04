namespace BaseballSim.Engine.Events
{
    /// <summary>
    /// 이벤트 로그 기본형. 기록(통계)은 이벤트 로그에서만 파생한다.
    /// </summary>
    public abstract class GameEvent
    {
        public int GameId { get; set; }

        /// <summary>경기 내 일련번호</summary>
        public int Sequence { get; set; }

        public int Inning { get; set; }

        public bool IsTopHalf { get; set; }
    }
}
