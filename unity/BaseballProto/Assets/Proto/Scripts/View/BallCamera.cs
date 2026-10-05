using System.Collections.Generic;
using BaseballProto.Core;
using UnityEngine;

namespace BaseballProto.View
{
    /// <summary>
    /// 타구 카메라. 홈 뒤 높은 곳(거리는 타구 종류별)에서 공과 처리 야수(송구 중이면 받는 곳)를 함께 화면에 담는다.
    /// 시야각은 화면 비율(세로/가로)에 맞춰 관심 지점이 모두 들어오게 정한다.
    /// 시작·끝에 타석 시점과 섞어 부드럽게 넘어가고, 끝나면 FieldView 타석 시점으로 되돌린다.
    /// </summary>
    public sealed class BallCamera
    {
        private const float MinDepthM = 1f;

        private readonly Camera _camera;
        private readonly FieldView _field;
        private readonly CameraShake _shake;
        private readonly ProtoTuning _t;

        private Vector3 _platePosition;
        private Quaternion _plateRotation;
        private float _plateFov;
        private Vector2 _rig;
        private Vector3 _position;
        private Vector3 _positionVelocity;
        private Vector3 _look;
        private Vector3 _lookVelocity;
        private float _fov;
        private float _fovVelocity;
        private float _blend;
        private bool _returning;

        public BallCamera(Camera camera, FieldView field, CameraShake shake, ProtoTuning tuning)
        {
            _camera = camera;
            _field = field;
            _shake = shake;
            _t = tuning;
        }

        /// <summary>타구 카메라가 카메라를 잡고 있는지 (이 동안 FieldView.ApplyCamera로 덮어쓰지 않는다)</summary>
        public bool Active { get; private set; }

        /// <summary>타석 시점(0) ↔ 타구 시점(1) 섞인 정도</summary>
        public float Weight => Active ? _blend * _blend * (3f - 2f * _blend) : 0f;

        public void Begin(BallCamProfile profile, List<Vector3> focus)
        {
            _field.ApplyCamera();
            _shake.ClearApplied();
            Transform t = _camera.transform;
            _platePosition = t.localPosition;
            _plateRotation = t.localRotation;
            _plateFov = _camera.fieldOfView;

            _rig = profile == BallCamProfile.Far ? _t.BallCamFar : profile == BallCamProfile.Mid ? _t.BallCamMid : _t.BallCamNear;
            _look = LookPoint(focus);
            _lookVelocity = Vector3.zero;
            _position = Desired(_look);
            _positionVelocity = Vector3.zero;
            _fov = FitFov(Quaternion.LookRotation(_look - _position, Vector3.up), focus);
            _fovVelocity = 0f;
            _blend = 0f;
            _returning = false;
            Active = true;
        }

        /// <summary>타석 시점으로 돌아가기 시작</summary>
        public void Return()
        {
            _returning = true;
        }

        /// <summary>바로 타석 시점으로 (건너뛰기·모드 변경)</summary>
        public void Cancel()
        {
            if (!Active)
            {
                return;
            }

            Active = false;
            _field.ApplyCamera();
            _shake.ClearApplied();
        }

        public void Tick(float deltaTime, List<Vector3> focus)
        {
            if (!Active)
            {
                return;
            }

            float step = _t.BallCamBlendS > 0f ? deltaTime / _t.BallCamBlendS : 1f;
            _blend = Mathf.Clamp01(_blend + (_returning ? -step : step));
            if (_returning && _blend <= 0f)
            {
                Cancel();
                return;
            }

            if (focus.Count > 0)
            {
                _look = Vector3.SmoothDamp(_look, LookPoint(focus), ref _lookVelocity, _t.BallCamLookSmoothS, Mathf.Infinity,
                    deltaTime);
            }

            _position = Vector3.SmoothDamp(_position, Desired(_look), ref _positionVelocity, _t.BallCamMoveSmoothS, Mathf.Infinity,
                deltaTime);
            Quaternion follow = Quaternion.LookRotation(_look - _position, Vector3.up);
            float targetFov = FitFov(follow, focus);
            _fov = Mathf.SmoothDamp(_fov, targetFov, ref _fovVelocity, _t.BallCamFovSmoothS, Mathf.Infinity, deltaTime);

            float w = _blend * _blend * (3f - 2f * _blend);
            Transform t = _camera.transform;
            t.localPosition = Vector3.Lerp(_platePosition, _position, w);
            t.localRotation = Quaternion.Slerp(_plateRotation, follow, w);
            _camera.fieldOfView = Mathf.Lerp(_plateFov, _fov, w);
            _shake.ClearApplied();
        }

        /// <summary>모든 관심 지점이 여유 있게 들어오는 세로 시야각 (가로 화면은 좁은 세로에, 세로 화면은 좁은 가로에 맞춰진다)</summary>
        private float FitFov(Quaternion rotation, List<Vector3> points)
        {
            float aspect = Mathf.Max(0.1f, _camera.aspect);
            float half = 0f;
            Quaternion inverse = Quaternion.Inverse(rotation);
            foreach (Vector3 p in points)
            {
                Vector3 local = inverse * (p - _position);
                if (local.z < MinDepthM)
                {
                    continue;
                }

                float vertical = Mathf.Atan(Mathf.Abs(local.y) / local.z);
                float horizontal = Mathf.Atan(Mathf.Abs(local.x) / local.z);
                float horizontalAsVertical = Mathf.Atan(Mathf.Tan(horizontal) / aspect);
                half = Mathf.Max(half, Mathf.Max(vertical, horizontalAsVertical));
            }

            float fov = 2f * half * Mathf.Rad2Deg * _t.BallCamFitMargin;
            return Mathf.Clamp(fov, _t.BallCamMinFovDeg, _t.BallCamMaxFovDeg);
        }

        /// <summary>관심 지점에서 홈 쪽으로 물러나 위에서 내려다보는 위치</summary>
        private Vector3 Desired(Vector3 look)
        {
            var flat = new Vector3(look.x, 0f, look.z);
            Vector3 away = flat.magnitude > 1f ? flat.normalized : Vector3.forward;
            return flat - away * _rig.y + Vector3.up * _rig.x;
        }

        /// <summary>관심 지점 평균. 높이는 일부만 반영해 경기장을 내려다보게 한다 (높이 뜬 공도 시야각 맞춤으로 화면에 들어온다)</summary>
        private Vector3 LookPoint(List<Vector3> points)
        {
            if (points.Count == 0)
            {
                return new Vector3(0f, 0f, 30f);
            }

            Vector3 sum = Vector3.zero;
            foreach (Vector3 p in points)
            {
                sum += p;
            }

            Vector3 mean = sum / points.Count;
            mean.y *= _t.BallCamLookHeightRatio;
            return mean;
        }
    }
}
