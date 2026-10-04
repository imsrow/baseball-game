using System;

namespace BaseballSim.Engine.Persistence
{
    /// <summary>
    /// 저장 데이터 형식이 올바르지 않거나 지원하지 않는 버전
    /// </summary>
    public sealed class SaveFormatException : Exception
    {
        public SaveFormatException(string message)
            : base(message)
        {
        }
    }
}
