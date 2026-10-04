using BaseballProto.Core;
using BaseballSim.Engine.Events;
using UnityEngine;

namespace BaseballProto.View
{
    /// <summary>
    /// 타구 비행 연출. 엔진이 정한 낙하·처리 지점(EndX, EndY)과 체공시간을 따라 공을 옮긴다.
    /// 필드 좌표(+x 1루 쪽, +y 중견수 쪽)는 월드 (x, z)에 그대로 대응한다. 연출 시계(히트스톱)로 움직인다.
    /// </summary>
    public sealed class BattedBallFlight
    {
        private const float Gravity = 9.81f;
        private const float KmhToMs = 1f / 3.6f;
        private const float GroundBallHeightM = 0.05f;
        private const float FoulBackDistanceM = 15f;
        private const float FoulSideDistanceM = 8f;
        private const float FoulApexM = 10f;

        private readonly BallView _ball;
        private readonly ProtoTuning _tuning;
        private Vector3 _from;
        private Vector3 _to;
        private float _apex;
        private float _duration;
        private float _elapsed;

        public BattedBallFlight(BallView ball, ProtoTuning tuning)
        {
            _ball = ball;
            _tuning = tuning;
        }

        public bool IsActive { get; private set; }

        public float Duration => _duration;

        public void Launch(Vector3 contact, BattedBallData data)
        {
            _from = contact;
            _to = new Vector3((float)data.EndX, 0f, (float)data.EndY);
            if (data.HangTimeS > 0)
            {
                float hang = (float)data.HangTimeS;
                _apex = Gravity * hang * hang / 8f;
                _duration = Mathf.Min(hang, _tuning.MaxBattedBallDisplayS);
            }
            else
            {
                _apex = 0f;
                _to.y = GroundBallHeightM;
                float speed = Mathf.Max(1f, (float)data.ExitVelocityKmh * KmhToMs * _tuning.GroundBallSpeedRatio);
                _duration = Mathf.Min((_to - _from).magnitude / speed, _tuning.MaxGroundBallDisplayS);
            }

            Begin();
        }

        /// <summary>파울: 엔진은 방향을 정하지 않으므로 뒤쪽 위로 넘어가는 연출만 한다</summary>
        public void LaunchFoul(Vector3 contact, float side)
        {
            _from = contact;
            _to = new Vector3(side * FoulSideDistanceM, 0f, -FoulBackDistanceM);
            _apex = FoulApexM;
            _duration = _tuning.FoulDisplayS;
            Begin();
        }

        public void Tick(float deltaTime)
        {
            if (!IsActive)
            {
                return;
            }

            _elapsed += deltaTime;
            float u = Mathf.Clamp01(_elapsed / _duration);
            Vector3 p = Vector3.Lerp(_from, _to, u) + Vector3.up * (4f * _apex * u * (1f - u));
            _ball.Show(p);
            if (u >= 1f)
            {
                IsActive = false;
            }
        }

        public void Stop()
        {
            IsActive = false;
        }

        private void Begin()
        {
            _duration = Mathf.Max(0.1f, _duration);
            _elapsed = 0f;
            IsActive = true;
            _ball.Show(_from);
        }
    }
}
