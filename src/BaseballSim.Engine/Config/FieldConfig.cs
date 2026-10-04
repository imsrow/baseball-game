namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 구장 규격과 야수 기본 수비 위치 (중립 구장)
    /// 좌표: 홈플레이트 원점, +y는 2루·중견수 방향, +x는 1루·우익수 방향. 단위 m.
    /// </summary>
    public sealed class FieldConfig
    {
        /// <summary>베이스 간 거리. 원본 90 ft</summary>
        public double BaseDistanceM { get; set; } = 27.43;

        /// <summary>펜스 거리를 정의하는 방향각 (도)</summary>
        public double[] FenceAnglesDeg { get; set; } = { -45.0, -22.5, 0.0, 22.5, 45.0 };

        /// <summary>방향각별 펜스 거리. 원본 325 / 375 / 400 / 375 / 325 ft</summary>
        public double[] FenceDistancesM { get; set; } = { 99.1, 114.3, 121.9, 114.3, 99.1 };

        /// <summary>펜스 높이. 원본 8 ft</summary>
        public double FenceHeightM { get; set; } = 2.44;

        /// <summary>
        /// 야수 기본 위치 (Position 순서: P, C, 1B, 2B, 3B, SS, LF, CF, RF): 홈에서의 거리 (m).
        /// 포수는 음수(홈플레이트 뒤).
        /// </summary>
        public double[] FielderDistancesM { get; set; } = { 18.0, -1.2, 33.0, 44.5, 32.0, 45.0, 89.0, 99.0, 89.0 };

        /// <summary>야수 기본 위치 방향각 (도)</summary>
        public double[] FielderAnglesDeg { get; set; } = { 0.0, 0.0, 37.0, 15.0, -36.0, -14.0, -27.0, 0.0, 27.0 };
    }
}
