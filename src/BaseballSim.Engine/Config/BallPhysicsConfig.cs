namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 타구 비행 물리 (항력 + 백스핀 양력, 수치적분)
    /// </summary>
    public sealed class BallPhysicsConfig
    {
        /// <summary>공 질량 (kg)</summary>
        public double MassKg { get; set; } = 0.145;

        /// <summary>공 반지름 (m)</summary>
        public double RadiusM { get; set; } = 0.0366;

        /// <summary>공기 밀도 (kg/m³), 중립 구장 기준</summary>
        public double AirDensity { get; set; } = 1.2;

        /// <summary>항력 계수</summary>
        public double DragCoefficient { get; set; } = 0.35;

        /// <summary>최대 양력 계수 (백스핀)</summary>
        public double LiftCoefficientMax { get; set; } = 0.17;

        /// <summary>이 발사각에서 양력 계수가 최대에 도달 (0도부터 선형 증가)</summary>
        public double LiftRampDeg { get; set; } = 30.0;

        public double Gravity { get; set; } = 9.81;

        /// <summary>적분 시간 간격 (s)</summary>
        public double TimeStepS { get; set; } = 0.005;

        /// <summary>타격 지점 높이 (m)</summary>
        public double ContactHeightM { get; set; } = 0.9;

        /// <summary>야수가 뜬공을 잡는 높이 (m)</summary>
        public double CatchHeightM { get; set; } = 1.0;

        /// <summary>비행 계산 최대 시간 (s, 안전장치)</summary>
        public double MaxFlightTimeS { get; set; } = 12.0;
    }
}
