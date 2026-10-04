namespace BaseballSim.Engine.Events
{
    /// <summary>
    /// 이벤트 수신자 (통계 집계, 저장, UI 등)
    /// </summary>
    public interface IEventSink
    {
        void OnEvent(GameEvent gameEvent);
    }
}
