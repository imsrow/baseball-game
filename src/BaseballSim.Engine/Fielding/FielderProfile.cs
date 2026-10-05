using System;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.Ratings;

namespace BaseballSim.Engine.Fielding
{
    /// <summary>
    /// 수비 중인 야수 한 명의 실효 능력과 이동·송구 시간 계산
    /// </summary>
    public sealed class FielderProfile
    {
        private readonly FieldingConfig _config;

        public FielderProfile(int playerId, Position position, FieldPoint start, double range, double hands,
            double arm, double accuracy, FieldingConfig config)
        {
            PlayerId = playerId;
            Position = position;
            Start = start;
            Range = range;
            Hands = hands;
            Arm = arm;
            Accuracy = accuracy;
            _config = config;
        }

        public int PlayerId { get; }

        public Position Position { get; }

        public FieldPoint Start { get; }

        /// <summary>실효 수비범위 (숙련도 반영)</summary>
        public double Range { get; }

        public double Hands { get; }

        public double Arm { get; }

        public double Accuracy { get; }

        public bool IsOutfielder => PositionInfo.IsOutfield(Position);

        public double ReactionTimeS =>
            (IsOutfielder ? _config.OutfieldReactionS : _config.InfieldReactionS)
            + (Position == Position.Pitcher ? _config.PitcherExtraReactionS : 0)
            + (Position == Position.Catcher ? _config.CatcherExtraReactionS : 0)
            - _config.ReactionSPerSd * ScoutScale.ToZ(Range);

        public double SpeedMps => IsOutfielder
            ? _config.OutfieldSpeedMps + _config.OutfieldSpeedMpsPerSd * ScoutScale.ToZ(Range)
            : _config.InfieldSpeedMps + _config.InfieldSpeedMpsPerSd * ScoutScale.ToZ(Range);

        public double ReachM => IsOutfielder ? _config.OutfieldReachM : _config.InfieldReachM;

        public double ThrowSpeedMps =>
            (IsOutfielder ? _config.OutfieldThrowSpeedMps : _config.InfieldThrowSpeedMps) + _config.ThrowSpeedMpsPerSd * ScoutScale.ToZ(Arm);

        public double TransferS => IsOutfielder ? _config.OutfieldTransferS : _config.InfieldTransferS;

        /// <summary>기본 위치에서 지점까지 도달 시간 (반응 + 이동, 손이 닿는 거리 제외)</summary>
        public double TimeToReach(FieldPoint point)
        {
            double distance = Math.Max(0, FieldPoint.Distance(Start, point) - ReachM);
            return ReactionTimeS + distance / SpeedMps;
        }

        /// <summary>뜬공 추격 이동 속도. 내야수는 땅볼 횡이동보다 빠르게 달린다</summary>
        public double AirBallSpeedMps => IsOutfielder
            ? SpeedMps
            : _config.InfieldAirBallSpeedMps + _config.InfieldSpeedMpsPerSd * ScoutScale.ToZ(Range);

        /// <summary>기본 위치에서 뜬공 포구 지점까지 도달 시간 (반응 + 이동, 손이 닿는 거리 제외)</summary>
        public double TimeToReachAirBall(FieldPoint point)
        {
            double distance = Math.Max(0, FieldPoint.Distance(Start, point) - ReachM);
            return ReactionTimeS + distance / AirBallSpeedMps;
        }

        /// <summary>지점에서 목표까지 송구 비행 시간</summary>
        public double ThrowTime(FieldPoint from, FieldPoint to)
        {
            return FieldPoint.Distance(from, to) / ThrowSpeedMps;
        }
    }
}
