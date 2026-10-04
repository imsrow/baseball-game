using System.Collections.Generic;

namespace BaseballSim.Engine.Events
{
    /// <summary>
    /// 여러 수신자에게 이벤트를 전달
    /// </summary>
    public sealed class CompositeEventSink : IEventSink
    {
        private readonly List<IEventSink> _sinks = new List<IEventSink>();

        public CompositeEventSink(params IEventSink[] sinks)
        {
            foreach (IEventSink sink in sinks)
            {
                if (sink != null)
                {
                    _sinks.Add(sink);
                }
            }
        }

        public void Add(IEventSink sink)
        {
            _sinks.Add(sink);
        }

        public void OnEvent(GameEvent gameEvent)
        {
            for (int i = 0; i < _sinks.Count; i++)
            {
                _sinks[i].OnEvent(gameEvent);
            }
        }
    }
}
