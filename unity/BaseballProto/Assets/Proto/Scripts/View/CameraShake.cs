using UnityEngine;

namespace BaseballProto.View
{
    /// <summary>
    /// 카메라 흔들림. 기준 위치는 FieldView가 정하고, 여기서는 매 프레임 오프셋만 더한다.
    /// </summary>
    public sealed class CameraShake
    {
        private readonly Transform _camera;
        private Vector3 _applied;
        private float _amplitude;
        private float _duration;
        private float _elapsed;

        public CameraShake(Transform camera)
        {
            _camera = camera;
        }

        public void Shake(float amplitude, float duration)
        {
            if (amplitude <= 0f || duration <= 0f)
            {
                return;
            }

            _amplitude = Mathf.Max(_amplitude * (1f - Progress()), amplitude);
            _duration = duration;
            _elapsed = 0f;
        }

        public void Tick(float unscaledDeltaTime)
        {
            _camera.localPosition -= _applied;
            _applied = Vector3.zero;
            if (_elapsed >= _duration)
            {
                return;
            }

            _elapsed += unscaledDeltaTime;
            float strength = _amplitude * (1f - Progress());
            _applied = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * strength;
            _camera.localPosition += _applied;
        }

        private float Progress()
        {
            return _duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / _duration);
        }
    }
}
