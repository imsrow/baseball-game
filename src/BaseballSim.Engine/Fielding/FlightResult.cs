namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 뜬 타구 비행 결과
    /// </summary>
    public sealed class FlightResult
    {
        /// <summary>펜스를 넘긴 홈런</summary>
        public bool IsHomeRun { get; set; }

        /// <summary>펜스에 맞음 (넘기지 못함)</summary>
        public bool HitWall { get; set; }

        /// <summary>포구 지점이 펜스 바로 앞 (펜스에 맞기 전 포구 높이 위에 있음)</summary>
        public bool CatchAtWall { get; set; }

        /// <summary>야수가 잡을 수 있는 높이로 내려오는 지점</summary>
        public FieldPoint CatchPoint { get; set; }

        /// <summary>CatchPoint 도달 시간 (체공시간)</summary>
        public double CatchTimeS { get; set; }

        /// <summary>첫 지면(또는 펜스) 도달 지점</summary>
        public FieldPoint LandingPoint { get; set; }

        public double LandingTimeS { get; set; }

        /// <summary>지면 도달 시 수평 속도 (m/s)</summary>
        public double LandingHorizontalSpeedMps { get; set; }

        /// <summary>최고 높이 (m)</summary>
        public double ApexM { get; set; }

        /// <summary>비거리 (홈런은 펜스가 없다고 가정한 낙하 거리)</summary>
        public double DistanceM { get; set; }
    }
}
