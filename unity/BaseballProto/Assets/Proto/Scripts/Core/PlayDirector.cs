using System.Collections.Generic;
using BaseballProto.View;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Events;
using BaseballSim.Engine.Fielding;
using BaseballSim.Engine.Players;
using BaseballSim.Engine.State;
using BaseballSim.Engine.Teams;
using UnityEngine;

namespace BaseballProto.Core
{
    /// <summary>
    /// 인플레이 연출 재생. 엔진 결과로 만든 대본(PlayScript)을 연출 시계로 재생하고, 건너뛰면 마지막 상태로 바로 간다.
    /// 재생 배속은 FX 패널 값(1x/1.5x/2x)이고, 그래도 MaxPlayDisplayS보다 길면 더 빨리 돌려 그 안에 끝낸다.
    /// </summary>
    public sealed class PlayDirector
    {
        private readonly ProtoTuning _t;
        private readonly FieldView _field;
        private readonly BallView _ball;
        private readonly FieldersView _fielders;
        private readonly RunnersView _runners;
        private readonly BallCamera _camera;
        private readonly PlayScriptBuilder _builder;
        private readonly List<Vector3> _focus = new List<Vector3>();

        // 포수 모양은 타석 시점 카메라 바로 앞이라, 카메라가 이만큼 타구 시점으로 넘어간 뒤에만 보인다
        private const float CatcherVisibleCameraWeight = 0.7f;

        private PlayScript _script;
        private float _time;
        private float _rate = 1f;

        public PlayDirector(LeagueConfig config, FieldGeometry geometry, ProtoTuning tuning, FieldView field, BallView ball,
            FieldersView fielders, RunnersView runners, BallCamera camera)
        {
            _t = tuning;
            _field = field;
            _ball = ball;
            _fielders = fielders;
            _runners = runners;
            _camera = camera;
            _builder = new PlayScriptBuilder(config, geometry, tuning);
        }

        public bool IsPlaying { get; private set; }

        public BallCamera Camera => _camera;

        /// <summary>다음 투구 준비: 야수는 기본 위치, 주자는 현재 베이스, 타자·투수 큐브 표시</summary>
        public void ResetForPitch(GameState state)
        {
            _script = null;
            IsPlaying = false;
            _camera.Cancel();
            _ball.SetScale(1f);
            _fielders.ResetForPitch();
            _runners.ShowBases(state);
            _field.SetActorsVisible(true);
        }

        /// <summary>도루 지시를 건 주자 표시 (0이면 해제)</summary>
        public void MarkSteal(int fromBase)
        {
            _runners.MarkSteal(fromBase);
        }

        public PlayScript Build(PitchEvent ev, Vector3 contact, float batterSide, PlayerDirectory players)
        {
            return _builder.Build(ev, contact, batterSide, players);
        }

        public void Begin(PlayScript script)
        {
            _script = script;
            _time = 0f;
            float speed = Mathf.Max(0.1f, _t.PlaySpeed);
            _rate = speed * Mathf.Max(1f, script.End / speed / Mathf.Max(1f, _t.MaxPlayDisplayS));
            IsPlaying = true;

            // 타자·투수 큐브 대신 주자·야수 모양으로
            _field.SetActorsVisible(false);
            _fielders.ShowForPlay();
            _fielders.SetCatcherVisible(false);
            _runners.HideAll();
            foreach (RunnerScript runner in script.Runners)
            {
                FigureView figure = _runners.Get(runner.PlayerId);
                figure.SetColor(ProtoColors.Runner);
                figure.Visible = true;
                figure.Snap(runner.Track.Start, Vector3.zero);
            }

            Apply(0f);
            if (_t.BallCam)
            {
                script.Focus(0f, _focus);
                _camera.Begin(script.CameraProfile, _focus);
            }
        }

        /// <summary>매 프레임 (연출 시계 dt, 히트스톱 중 0). 재생이 끝나도 카메라 복귀는 계속 진행한다</summary>
        public void Tick(float deltaTime)
        {
            if (IsPlaying)
            {
                float step = deltaTime * _rate;
                _time = Mathf.Min(_script.End, _time + step);
                Apply(step);
                if (_time >= _script.End)
                {
                    Finish();
                }
            }

            if (_camera.Active)
            {
                if (_script != null && IsPlaying)
                {
                    _script.Focus(_time, _focus);
                }

                _camera.Tick(deltaTime, _focus);
            }

            if (_script != null)
            {
                _fielders.SetCatcherVisible(_camera.Weight > CatcherVisibleCameraWeight);

                // 공은 카메라가 멀어지는 만큼 키운다 (타석 시점에서는 실제 크기)
                _ball.SetScale(Mathf.Lerp(1f, _t.BallCamBallScale, _camera.Weight));
            }
        }

        /// <summary>결과로 바로: 마지막 상태를 보여주고 카메라도 바로 타석 시점으로</summary>
        public void Skip()
        {
            if (!IsPlaying)
            {
                return;
            }

            _time = _script.End;
            Apply(0f);
            Finish();
            _camera.Cancel();
        }

        private void Finish()
        {
            IsPlaying = false;
            _ball.Hide();
            foreach (RunnerScript runner in _script.Runners)
            {
                FigureView figure = _runners.Get(runner.PlayerId);
                figure.Visible = runner.FinalBase > 0;
                if (runner.FinalBase > 0)
                {
                    figure.Snap(runner.Track.EndPosition, Vector3.zero);
                }
            }

            _camera.Return();
        }

        private void Apply(float step)
        {
            float t = _time;
            Vector3 ball = _script.Ball.Evaluate(t);
            if (t < _script.BallHideTime)
            {
                _ball.Show(ball);
            }
            else
            {
                _ball.Hide();
            }

            // 프레임 간 이동량으로 기울기를 정하므로 실제 경과 시간(재생 배속 반영 전) 대신 대본 시간 간격을 쓴다
            float dt = step;
            foreach (KeyValuePair<Position, MotionTrack> pair in _script.Fielders)
            {
                // 숨긴 포수도 위치는 따라간다 (카메라가 넘어가면 바로 그 자리에 보이게)
                _fielders.Get(pair.Key).Place(pair.Value.Evaluate(t), ball, dt);
            }

            foreach (RunnerScript runner in _script.Runners)
            {
                FigureView figure = _runners.Get(runner.PlayerId);
                bool visible = t < runner.HideTime;
                figure.Visible = visible;
                if (!visible)
                {
                    continue;
                }

                figure.SetColor(t >= runner.OutTime ? ProtoColors.RunnerOut : ProtoColors.Runner);
                figure.Place(runner.Track.Evaluate(t), ball, dt);
            }
        }
    }
}
