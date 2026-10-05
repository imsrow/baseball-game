using BaseballSim.Engine.Fielding;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Ratings;
using UnityEngine;

namespace BaseballProto.Core
{
    /// <summary>
    /// 연출 대본을 만드는 동안의 주자 계획. 기본 시간은 엔진 주루 모델(그 주자의 스피드 능력치)에서 오고,
    /// 송구와 맞추려고 속도를 바꿀 때는 스피드 ± RangeSd 표준편차에 해당하는 시간 안에서만 바꾼다.
    /// </summary>
    internal sealed class RunnerPlan
    {
        private readonly BaserunningModel _model;
        private readonly RunnerProfile _profile;
        private readonly RunnerProfile _fast;
        private readonly RunnerProfile _slow;

        public RunnerPlan(RunnerMovement movement, BatterRatings ratings, Hand hand, BaserunningModel model, float rangeSd)
        {
            PlayerId = movement.PlayerId;
            From = movement.FromBase;
            To = movement.ToBase;
            IsOut = movement.IsOut;
            _model = model;
            _profile = new RunnerProfile(PlayerId, ratings, hand);
            int delta = Mathf.RoundToInt(rangeSd * ScoutScale.PointsPerStandardDeviation);
            _fast = new RunnerProfile(PlayerId, new BatterRatings { Speed = ScoutScale.Clamp(ratings.Speed + delta) }, hand);
            _slow = new RunnerProfile(PlayerId, new BatterRatings { Speed = ScoutScale.Clamp(ratings.Speed - delta) }, hand);
        }

        public int PlayerId { get; }

        public int From { get; }

        public int To { get; }

        public bool IsOut { get; }

        public bool IsBatter => From == 0;

        /// <summary>출발 시각 (타구 순간 기준)</summary>
        public float Start { get; set; }

        /// <summary>기본 시간 대비 배율 (1 = 능력치 그대로, 클수록 느림)</summary>
        public float Scale { get; set; } = 1f;

        /// <summary>속도를 송구에 맞춰 바꾸지 않는다 (홈런 일주 등)</summary>
        public bool Fixed { get; set; }

        /// <summary>송구 도착에 맞춰 아웃이 확정되는 시각 (송구가 없으면 베이스 도착 시각)</summary>
        public float OutTime { get; set; } = float.NaN;

        public float Natural(int toBase)
        {
            return (float)_model.TimeToBase(_profile, From, toBase);
        }

        public float ArrivalAt(int toBase)
        {
            return Start + Scale * Natural(toBase);
        }

        /// <summary>toBase 도착 시각을 target에 맞춘다 (능력치 범위 안에서). 실제로 맞춘 도착 시각을 돌려준다</summary>
        public float SetArrival(int toBase, float target)
        {
            float natural = Natural(toBase);
            if (Fixed || natural <= 0f)
            {
                return ArrivalAt(toBase);
            }

            float min = (float)_model.TimeToBase(_fast, From, toBase) / natural;
            float max = (float)_model.TimeToBase(_slow, From, toBase) / natural;
            Scale = Mathf.Clamp((target - Start) / natural, min, max);
            return ArrivalAt(toBase);
        }
    }
}
