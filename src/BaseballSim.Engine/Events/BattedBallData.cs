using BaseballSim.Engine.Batting;
using BaseballSim.Engine.Players;

namespace BaseballSim.Engine.Events
{
    /// <summary>
    /// 이벤트 로그용 타구 데이터
    /// </summary>
    public sealed class BattedBallData
    {
        public double ExitVelocityKmh { get; set; }

        public double LaunchAngleDeg { get; set; }

        public double SprayAngleDeg { get; set; }

        public BattedBallType Type { get; set; }

        public bool IsSolid { get; set; }

        /// <summary>처리·낙하 지점 (m)</summary>
        public double EndX { get; set; }

        public double EndY { get; set; }

        /// <summary>체공시간 (땅볼 0)</summary>
        public double HangTimeS { get; set; }

        /// <summary>비거리 (땅볼은 처리 지점까지 거리)</summary>
        public double DistanceM { get; set; }

        public Position? FieldedBy { get; set; }
    }
}
