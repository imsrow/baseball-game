using System;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 주자 시간과 진루 판단 기준
    /// </summary>
    public sealed class BaserunningModel
    {
        private readonly BaserunningConfig _config;

        public BaserunningModel(BaserunningConfig config)
        {
            _config = config;
        }

        public BaserunningConfig Config => _config;

        public double HomeToFirst(RunnerProfile runner)
        {
            double t = _config.HomeToFirstS - _config.HomeToFirstPerSd * ScoutScale.ToZ(runner.Ratings.Speed);
            if (runner.BattingHand == Hand.Left)
            {
                t -= _config.LeftyHomeToFirstBonusS;
            }

            return t;
        }

        public double BaseToBase(RunnerProfile runner)
        {
            return _config.BaseToBaseS - _config.BaseToBasePerSd * ScoutScale.ToZ(runner.Ratings.Speed);
        }

        /// <summary>전력 질주 중 추가 베이스 시간</summary>
        public double ExtraBase(RunnerProfile runner)
        {
            return _config.ExtraBaseS - _config.ExtraBasePerSd * ScoutScale.ToZ(runner.Ratings.Speed);
        }

        /// <summary>fromBase(0 = 타자)에서 toBase까지 걸리는 시간 (타구 순간부터)</summary>
        public double TimeToBase(RunnerProfile runner, int fromBase, int toBase)
        {
            if (toBase <= fromBase)
            {
                return 0;
            }

            double first = fromBase == 0 ? HomeToFirst(runner) : BaseToBase(runner);
            return first + (toBase - fromBase - 1) * ExtraBase(runner);
        }

        /// <summary>여유 시간 추정 오차 (주루 센스가 높을수록 작다)</summary>
        public double EstimateNoise(RunnerProfile runner)
        {
            return _config.EstimateNoiseS * Math.Exp(-_config.InstinctNoiseBeta * ScoutScale.ToZ(runner.Ratings.BaserunningInstinct));
        }

        /// <summary>추가 진루 시도에 필요한 예상 여유 시간</summary>
        public double RequiredMargin(RunnerProfile runner)
        {
            return _config.AdvanceMarginS - _config.InstinctMarginPerSdS * ScoutScale.ToZ(runner.Ratings.BaserunningInstinct);
        }
    }
}
