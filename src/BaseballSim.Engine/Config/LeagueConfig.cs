namespace BaseballSim.Engine.Config
{
    /// <summary>
    /// 튜닝 계수 총괄. 엔진의 모든 조정 가능한 값은 여기서 시작한다.
    /// 리그 득점 환경과 목표치는 Environment(LeagueEnvironment 프리셋)로 분리되어 있다.
    /// </summary>
    public sealed class LeagueConfig
    {
        public LeagueEnvironment Environment { get; set; } = LeagueEnvironmentPresets.Standard();
        public RulesConfig Rules { get; set; } = new RulesConfig();
        public StrikeZoneConfig StrikeZone { get; set; } = new StrikeZoneConfig();
        public PitchConfig Pitch { get; set; } = new PitchConfig();
        public FatigueConfig Fatigue { get; set; } = new FatigueConfig();
        public SwingConfig Swing { get; set; } = new SwingConfig();
        public ContactConfig Contact { get; set; } = new ContactConfig();
        public BattedBallConfig BattedBall { get; set; } = new BattedBallConfig();
        public BallPhysicsConfig Physics { get; set; } = new BallPhysicsConfig();
        public FieldConfig Field { get; set; } = new FieldConfig();
        public FieldingConfig Fielding { get; set; } = new FieldingConfig();
        public BaserunningConfig Baserunning { get; set; } = new BaserunningConfig();
        public StealConfig Steal { get; set; } = new StealConfig();
        public PassedBallConfig PassedBall { get; set; } = new PassedBallConfig();
        public BuntConfig Bunt { get; set; } = new BuntConfig();
        public DefenseConfig Defense { get; set; } = new DefenseConfig();
        public PlatoonConfig Platoon { get; set; } = new PlatoonConfig();
        public ManagerAiConfig ManagerAi { get; set; } = new ManagerAiConfig();
        public InputModifierConfig InputModifier { get; set; } = new InputModifierConfig();
        public SeasonConfig Season { get; set; } = new SeasonConfig();
        public InteractionConfig Interaction { get; set; } = new InteractionConfig();

        /// <summary>
        /// 투구 위치(구역)가 타구 품질(정타 확률·타구속도)에 주는 영향 전체 배율.
        /// 0 = 영향 없음, 1 = 현실 기준(Statcast 근사), 1보다 크면 캐주얼하게 과장. 구역별 값은 BattedBallConfig
        /// </summary>
        public double LocationEffectScale { get; set; } = 1.0;

        public static LeagueConfig CreateDefault()
        {
            return new LeagueConfig();
        }
    }
}
