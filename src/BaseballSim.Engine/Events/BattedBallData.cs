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

        // ── 연출용 (판정·기록에는 쓰지 않음, 시각은 타구 순간 기준 s) ──

        /// <summary>잡히지 않은 뜬공이 땅·펜스에 처음 닿은 지점과 시각. 없으면 HasLanding = false</summary>
        public bool HasLanding { get; set; }

        public double LandingX { get; set; }

        public double LandingY { get; set; }

        public double LandingTimeS { get; set; }

        /// <summary>처리 야수가 공을 잡은 시각 (포구·땅볼 처리·외야 회수). 홈런은 0</summary>
        public double FieldedTimeS { get; set; }

        /// <summary>처리 야수가 처리 지점에 도착할 수 있는 엔진 기준 시각 (반응 + 이동)</summary>
        public double FielderArrivalS { get; set; }
    }
}
