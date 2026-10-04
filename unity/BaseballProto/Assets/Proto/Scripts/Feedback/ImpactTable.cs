namespace BaseballProto.Feedback
{
    /// <summary>
    /// 연출 종류별 세기 (진동 ms·세기, 히트스톱 s, 흔들림 m·s, 볼륨)
    /// </summary>
    public static class ImpactTable
    {
        private static readonly ImpactSpec WhiffSpec = new ImpactSpec(12, 50, 0f, 0f, 0f, 0.6f);
        private static readonly ImpactSpec FoulSpec = new ImpactSpec(25, 120, 0.04f, 0.015f, 0.12f, 0.8f);
        private static readonly ImpactSpec WeakSpec = new ImpactSpec(30, 150, 0.05f, 0.02f, 0.15f, 0.9f);
        private static readonly ImpactSpec SolidSpec = new ImpactSpec(45, 255, 0.09f, 0.04f, 0.22f, 1f);
        private static readonly ImpactSpec HomeRunSpec = new ImpactSpec(80, 255, 0.14f, 0.06f, 0.35f, 1f);
        private static readonly ImpactSpec MittSpec = new ImpactSpec(10, 40, 0f, 0.005f, 0.06f, 0.7f);

        public static ImpactSpec Get(ImpactKind kind)
        {
            switch (kind)
            {
                case ImpactKind.Whiff: return WhiffSpec;
                case ImpactKind.Foul: return FoulSpec;
                case ImpactKind.WeakContact: return WeakSpec;
                case ImpactKind.SolidContact: return SolidSpec;
                case ImpactKind.HomeRun: return HomeRunSpec;
                default: return MittSpec;
            }
        }
    }
}
