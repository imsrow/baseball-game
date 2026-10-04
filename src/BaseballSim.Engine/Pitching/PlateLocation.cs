namespace BaseballSim.Engine.Pitching
{
    /// <summary>
    /// 홈플레이트 통과 위치 (m). X: 포수 시점 좌우(1루 쪽 +), Z: 지면으로부터 높이
    /// </summary>
    public struct PlateLocation
    {
        public PlateLocation(double x, double z)
        {
            X = x;
            Z = z;
        }

        public double X { get; set; }

        public double Z { get; set; }

        public override string ToString()
        {
            return "(" + X.ToString("0.000") + ", " + Z.ToString("0.000") + ")";
        }
    }
}
