namespace BaseballSim.Engine.Units
{
    /// <summary>
    /// 단위 변환. 엔진 내부와 표시는 미터법(km/h, m)을 기본으로 쓰고,
    /// Statcast 등 원본 자료(mph, ft, inch)를 옮길 때만 이 변환을 사용한다.
    /// </summary>
    public static class UnitConversion
    {
        /// <summary>1 mph = 1.609344 km/h (정의값)</summary>
        public const double KmhPerMph = 1.609344;

        /// <summary>1 ft = 0.3048 m (정의값)</summary>
        public const double MetersPerFoot = 0.3048;

        /// <summary>1 inch = 0.0254 m (정의값)</summary>
        public const double MetersPerInch = 0.0254;

        /// <summary>1 m/s = 3.6 km/h</summary>
        public const double KmhPerMetersPerSecond = 3.6;

        public static double MphToKmh(double mph) => mph * KmhPerMph;

        public static double KmhToMph(double kmh) => kmh / KmhPerMph;

        public static double FeetToMeters(double feet) => feet * MetersPerFoot;

        public static double MetersToFeet(double meters) => meters / MetersPerFoot;

        public static double InchesToMeters(double inches) => inches * MetersPerInch;

        public static double KmhToMetersPerSecond(double kmh) => kmh / KmhPerMetersPerSecond;

        public static double MetersPerSecondToKmh(double metersPerSecond) => metersPerSecond * KmhPerMetersPerSecond;
    }
}
