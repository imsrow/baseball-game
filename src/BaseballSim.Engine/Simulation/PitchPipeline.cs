using BaseballSim.Engine.Batting;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Control;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Fielding;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Randomness;

namespace BaseballSim.Engine.Simulation
{
    /// <summary>
    /// 투구 판정 순서. 사람이든 AI든 결정(무엇을 할지)만 다르고 결과는 모두 이 파이프라인이 정한다.
    /// 1) 실행: 목표 + 제구 오차 → 실제 위치·구속
    /// 2) 인지: 선구안 오차 → 타자가 본 위치
    /// 3) 판정: 지켜봄 → 몸맞는공/루킹 판정, 스윙 → 헛스윙/파울/인플레이 → 타구 → 수비·주루
    /// </summary>
    public sealed class PitchPipeline
    {
        private readonly LeagueConfig _config;

        public PitchPipeline(LeagueConfig config)
        {
            _config = config;
            Zone = new StrikeZone(config.StrikeZone);
            Field = new FieldGeometry(config.Field);
            Executor = new PitchExecutor(config, Zone);
            Perception = new PitchPerception(config.Swing, Zone);
            Umpire = new UmpireJudge(config.StrikeZone, Zone);
            Contact = new ContactResolver(config);
            BattedBalls = new BattedBallGenerator(config);
            Fielding = new FieldingResolver(config, Field);
            Bunts = new BuntResolver(config);
            Steals = new StealModel(config);
            PassedBalls = new PassedBallModel(config);
        }

        public StrikeZone Zone { get; }
        public FieldGeometry Field { get; }
        public PitchExecutor Executor { get; }
        public PitchPerception Perception { get; }
        public UmpireJudge Umpire { get; }
        public ContactResolver Contact { get; }
        public BattedBallGenerator BattedBalls { get; }
        public FieldingResolver Fielding { get; }
        public BuntResolver Bunts { get; }
        public StealModel Steals { get; }
        public PassedBallModel PassedBalls { get; }

        /// <summary>투구 실행과 인지</summary>
        public PitchInFlight Release(PitchCall call, Player pitcher, double fatigue, Player batter, bool byHuman,
            IRandomSource random)
        {
            ExecutedPitch executed = Executor.Execute(call.Type, call.Target, pitcher.Pitching, fatigue, call.ReleaseQuality, random);
            PerceivedPitch perceived = Perception.Perceive(executed, batter.Batting, random);
            return new PitchInFlight
            {
                Call = call,
                Executed = executed,
                Perceived = perceived,
                ByHuman = byHuman,
            };
        }

        /// <summary>타자 행동에 따른 투구 결과 판정</summary>
        public PitchResolution Resolve(PitchInFlight pitch, BatterAction action, Player batter, Hand battingHand,
            bool sameHand, int strikes, Player catcher, PlaySituationBuilder situationBuilder, IRandomSource random)
        {
            ExecutedPitch executed = pitch.Executed;
            if (action.Type == BatterActionType.Take)
            {
                if (Zone.IsInHitByPitchArea(executed.Actual, battingHand)
                    && random.NextDouble() < _config.Environment.HitByPitchProbability)
                {
                    return new PitchResolution { Result = PitchResult.HitByPitch };
                }

                double strikeProbability = Umpire.CalledStrikeProbability(executed.Actual, catcher.Fielding.Framing);
                return new PitchResolution
                {
                    Result = random.NextDouble() < strikeProbability ? PitchResult.CalledStrike : PitchResult.Ball,
                };
            }

            if (action.Type == BatterActionType.Bunt)
            {
                return ResolveBunt(executed, action, batter, situationBuilder, random);
            }

            double contact = Contact.ContactProbability(batter.Batting, executed, strikes, sameHand, action.TimingQuality);
            if (random.NextDouble() >= contact)
            {
                return new PitchResolution { Result = PitchResult.SwingingStrike };
            }

            if (random.NextDouble() < Contact.FoulProbability(executed))
            {
                return new PitchResolution { Result = PitchResult.Foul };
            }

            BattedBall ball = BattedBalls.Generate(batter.Batting, battingHand, executed, sameHand, action.TimingQuality, random,
                action.TimingDirection, action.CursorVerticalOffset);
            PlayResult play = Fielding.Resolve(ball, situationBuilder(), random);
            return new PitchResolution
            {
                Result = PitchResult.InPlay,
                BattedBall = ball,
                Play = play,
            };
        }

        private PitchResolution ResolveBunt(ExecutedPitch executed, BatterAction action, Player batter,
            PlaySituationBuilder situationBuilder, IRandomSource random)
        {
            if (random.NextDouble() >= Bunts.ContactProbability(batter.Batting, executed))
            {
                return new PitchResolution { Result = PitchResult.SwingingStrike, IsBunt = true };
            }

            if (random.NextDouble() < Bunts.FoulProbability(batter.Batting))
            {
                return new PitchResolution { Result = PitchResult.Foul, IsBunt = true };
            }

            PlayResult play = Bunts.ResolveFair(action.BuntType, batter.Batting, situationBuilder(), random, out BattedBall ball);
            return new PitchResolution
            {
                Result = PitchResult.InPlay,
                IsBunt = true,
                BattedBall = ball,
                Play = play,
            };
        }
    }

    /// <summary>인플레이일 때만 수비 상황을 만들기 위한 지연 생성 대리자</summary>
    public delegate PlaySituation PlaySituationBuilder();
}
