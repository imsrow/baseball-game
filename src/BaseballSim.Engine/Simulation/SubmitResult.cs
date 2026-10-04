namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// 사람 입력 제출 결과
    /// </summary>
    public sealed class SubmitResult
    {
        private SubmitResult(bool accepted, string reason)
        {
            Accepted = accepted;
            Reason = reason;
        }

        public bool Accepted { get; }

        /// <summary>거절 사유 (수락이면 null)</summary>
        public string Reason { get; }

        public static SubmitResult Accept() => new SubmitResult(true, null);

        public static SubmitResult Reject(string reason) => new SubmitResult(false, reason);
    }
}
