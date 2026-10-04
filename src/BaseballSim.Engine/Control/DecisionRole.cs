namespace BaseballSim.Engine.Control
{
    /// <summary>
    /// 결정 역할. 주루·수비 결정은 이후 이 목록과 ControllerSet에 슬롯을 추가해 확장한다.
    /// </summary>
    public enum DecisionRole
    {
        Pitching = 0,
        Batting = 1,
        Manager = 2,
    }
}
