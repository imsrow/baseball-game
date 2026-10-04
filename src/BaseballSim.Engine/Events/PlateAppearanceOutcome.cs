namespace BaseballSim.Engine.Events
{
    /// <summary>
    /// 타석 결과
    /// </summary>
    public enum PlateAppearanceOutcome
    {
        Single = 0,
        Double = 1,
        Triple = 2,
        HomeRun = 3,
        Walk = 4,
        IntentionalWalk = 5,
        HitByPitch = 6,
        Strikeout = 7,
        GroundOut = 8,
        FlyOut = 9,
        LineOut = 10,
        PopOut = 11,
        GroundedIntoDoublePlay = 12,
        FieldersChoice = 13,
        ReachedOnError = 14,
        SacrificeFly = 15,
        SacrificeBunt = 16,
    }
}
