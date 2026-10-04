namespace BaseballSim.Engine.Batting
{
    /// <summary>
    /// 발사각으로 타구 유형 분류 (Statcast 정의값)
    /// </summary>
    public static class BattedBallClassifier
    {
        public const double LineDriveMinDeg = 10.0;
        public const double FlyBallMinDeg = 25.0;
        public const double PopUpMinDeg = 50.0;

        public static BattedBallType Classify(double launchAngleDeg)
        {
            if (launchAngleDeg < LineDriveMinDeg)
            {
                return BattedBallType.GroundBall;
            }

            if (launchAngleDeg < FlyBallMinDeg)
            {
                return BattedBallType.LineDrive;
            }

            if (launchAngleDeg < PopUpMinDeg)
            {
                return BattedBallType.FlyBall;
            }

            return BattedBallType.PopUp;
        }
    }
}
