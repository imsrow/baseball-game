using BaseballSim.Engine.Config;
using BaseballSim.Engine.Pitching;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Probability;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 도루 성공 확률. 감독 AI의 시도 판단과 엔진 판정이 같은 식을 쓴다.
    /// </summary>
    public sealed class StealModel
    {
        private readonly LeagueConfig _config;

        public StealModel(LeagueConfig config)
        {
            _config = config;
        }

        /// <param name="toBase">2 또는 3</param>
        /// <param name="pitchType">투구 종류를 모르면(AI 사전 판단) null</param>
        public double SuccessProbability(BatterRatings runner, int toBase, PitcherRatings pitcher, Player catcher,
            PitchType? pitchType)
        {
            StealConfig sc = _config.Steal;
            int proficiency = catcher.Fielding.GetProficiency(Position.Catcher);
            double arm = DefenseCalculator.EffectiveRating(catcher.Fielding.ArmStrength, proficiency, _config.Defense);
            double accuracy = DefenseCalculator.EffectiveRating(catcher.Fielding.ArmAccuracy, proficiency, _config.Defense);

            double logit = LogOdds.Logit(sc.BaseSuccessRate)
                + sc.JumpBeta * ScoutScale.ToZ(runner.StealJump)
                + sc.SpeedBeta * ScoutScale.ToZ(runner.Speed)
                - sc.HoldRunnersBeta * ScoutScale.ToZ(pitcher.HoldRunners)
                - sc.CatcherArmBeta * ScoutScale.ToZ(arm)
                - sc.CatcherAccuracyBeta * ScoutScale.ToZ(accuracy);
            if (toBase == 3)
            {
                logit += sc.StealThirdShift;
            }

            if (pitchType.HasValue && PitchTypeInfo.FamilyOf(pitchType.Value) != PitchFamily.Fastball)
            {
                logit += sc.SlowPitchShift;
            }

            return LogOdds.Logistic(logit);
        }
    }
}
