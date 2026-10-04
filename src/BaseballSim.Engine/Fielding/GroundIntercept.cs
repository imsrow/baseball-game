namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 굴러가는 공을 야수가 막는 지점과 시각
    /// </summary>
    public sealed class GroundIntercept
    {
        public GroundIntercept(FielderProfile fielder, FieldPoint point, double ballTimeS, double marginS)
        {
            Fielder = fielder;
            Point = point;
            BallTimeS = ballTimeS;
            MarginS = marginS;
        }

        public FielderProfile Fielder { get; }

        public FieldPoint Point { get; }

        /// <summary>공을 잡는 시각 (타구 순간 기준)</summary>
        public double BallTimeS { get; }

        /// <summary>야수가 공보다 먼저 도착한 여유 시간</summary>
        public double MarginS { get; }
    }
}
