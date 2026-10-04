namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 플레이 중 주자 한 명의 이동. FromBase 0은 타자, ToBase 4는 홈(득점).
    /// 아웃이면 ToBase는 아웃된 베이스.
    /// </summary>
    public sealed class RunnerMovement
    {
        public RunnerMovement()
        {
        }

        public RunnerMovement(int playerId, int fromBase, int toBase, bool isOut)
        {
            PlayerId = playerId;
            FromBase = fromBase;
            ToBase = toBase;
            IsOut = isOut;
        }

        public int PlayerId { get; set; }

        public int FromBase { get; set; }

        public int ToBase { get; set; }

        public bool IsOut { get; set; }

        public bool Scored => !IsOut && ToBase == 4;
    }
}
