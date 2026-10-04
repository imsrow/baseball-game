namespace BaseballSim.Engine.Teams
{
    /// <summary>
    /// 경기 시작 구성
    /// </summary>
    public sealed class GameSetup
    {
        public int GameId { get; set; }

        public GameTeamSetup Away { get; set; }

        public GameTeamSetup Home { get; set; }
    }
}
