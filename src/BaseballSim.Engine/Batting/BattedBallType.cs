namespace BaseballSim.Engine.Batting
{
    /// <summary>
    /// 타구 유형 (Statcast 발사각 기준: 땅볼 &lt;10°, 라인드라이브 10~25°, 뜬공 25~50°, 팝업 &gt;50°)
    /// </summary>
    public enum BattedBallType
    {
        GroundBall = 0,
        LineDrive = 1,
        FlyBall = 2,
        PopUp = 3,
    }
}
