namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 결정 결과: 즉시 응답(Ready) 또는 입력 대기(Pending).
    /// AI는 항상 Ready, 사람 입력 컨트롤러는 Pending을 돌려주고 엔진은 그 지점에서 멈춘다.
    /// </summary>
    public readonly struct Decision<T>
    {
        private Decision(bool isReady, T value)
        {
            IsReady = isReady;
            Value = value;
        }

        public bool IsReady { get; }

        public T Value { get; }

        public static Decision<T> Pending => default;

        public static Decision<T> Ready(T value)
        {
            return new Decision<T>(true, value);
        }
    }
}
