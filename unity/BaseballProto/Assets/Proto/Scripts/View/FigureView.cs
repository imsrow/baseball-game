using UnityEngine;

namespace BaseballProto.View
{
    /// <summary>
    /// 사람 하나 (큐브 몸통 + 구체 머리). 움직이는 방향으로 살짝 기울고, 지정한 지점을 바라본다.
    /// </summary>
    public sealed class FigureView
    {
        private const float BodyHeightM = 1.3f;
        private const float BodyWidthM = 0.45f;
        private const float BodyDepthM = 0.3f;
        private const float HeadDiameterM = 0.3f;
        private const float MaxLeanDeg = 14f;
        private const float LeanDegPerMps = 2f;

        private readonly GameObject _root;
        private readonly GameObject _body;
        private Color _color;

        public FigureView(Transform parent, string name, Color color)
        {
            _root = new GameObject(name);
            _root.transform.SetParent(parent, false);
            _body = PrimitiveFactory.Create(PrimitiveType.Cube, "Body", _root.transform, new Vector3(0f, BodyHeightM * 0.5f, 0f),
                new Vector3(BodyWidthM, BodyHeightM, BodyDepthM), color);
            PrimitiveFactory.Create(PrimitiveType.Sphere, "Head", _root.transform,
                new Vector3(0f, BodyHeightM + HeadDiameterM * 0.45f, 0f), Vector3.one * HeadDiameterM, ProtoColors.Skin);
            _color = color;
        }

        public Vector3 Position => _root.transform.localPosition;

        public bool Visible
        {
            get => _root.activeSelf;
            set => _root.SetActive(value);
        }

        public void SetColor(Color color)
        {
            if (color != _color)
            {
                _color = color;
                PrimitiveFactory.SetColor(_body, color);
            }
        }

        /// <summary>위치를 옮기고 lookAt을 바라본다. 이동 속도만큼 앞으로 기운다</summary>
        public void Place(Vector3 position, Vector3 lookAt, float deltaTime)
        {
            Transform t = _root.transform;
            Vector3 velocity = deltaTime > 0f ? (position - t.localPosition) / deltaTime : Vector3.zero;
            velocity.y = 0f;
            t.localPosition = position;
            Vector3 facing = lookAt - position;
            facing.y = 0f;
            float speed = velocity.magnitude;
            if (speed > 1f)
            {
                // 달리는 중엔 진행 방향을 보고 기운다
                facing = velocity;
            }

            if (facing.sqrMagnitude < 1e-4f)
            {
                return;
            }

            float lean = Mathf.Min(MaxLeanDeg, speed * LeanDegPerMps);
            t.localRotation = Quaternion.LookRotation(facing.normalized, Vector3.up) * Quaternion.Euler(lean, 0f, 0f);
        }

        public void Snap(Vector3 position, Vector3 lookAt)
        {
            _root.transform.localPosition = position;
            Vector3 facing = lookAt - position;
            facing.y = 0f;
            _root.transform.localRotation = facing.sqrMagnitude > 1e-4f
                ? Quaternion.LookRotation(facing.normalized, Vector3.up)
                : Quaternion.identity;
        }
    }
}
