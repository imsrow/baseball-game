using UnityEngine;

namespace BaseballProto.View
{
    /// <summary>
    /// 홈플레이트 평면 위 고리 표시 (타격 커서, 투구 목표, 실제 통과 위치).
    /// 셰이더 의존을 피하려고 작은 큐브를 원형으로 늘어놓는다.
    /// </summary>
    public sealed class RingView
    {
        private const int Segments = 24;
        private const float SegmentThicknessM = 0.012f;

        // 공보다 살짝 카메라 쪽에 그려 가려지지 않게
        private const float FrontOffsetM = -0.03f;

        private readonly Transform _root;
        private readonly GameObject[] _segments = new GameObject[Segments];
        private readonly GameObject _center;
        private float _radius;

        public RingView(Transform parent, string name, float radius, Color color)
        {
            _root = new GameObject(name).transform;
            _root.SetParent(parent, false);
            for (int i = 0; i < Segments; i++)
            {
                _segments[i] = PrimitiveFactory.Create(PrimitiveType.Cube, "Seg", _root, Vector3.zero, Vector3.one, color);
            }

            _center = PrimitiveFactory.Create(PrimitiveType.Cube, "Dot", _root, Vector3.zero,
                new Vector3(SegmentThicknessM * 1.5f, SegmentThicknessM * 1.5f, SegmentThicknessM), color);
            SetRadius(radius);
        }

        public void SetRadius(float radius)
        {
            if (Mathf.Approximately(radius, _radius))
            {
                return;
            }

            _radius = radius;
            float segmentLength = 2f * Mathf.PI * radius / Segments * 0.7f;
            for (int i = 0; i < Segments; i++)
            {
                float a = i * Mathf.PI * 2f / Segments;
                Transform t = _segments[i].transform;
                t.localPosition = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
                t.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + 90f);
                t.localScale = new Vector3(segmentLength, SegmentThicknessM, SegmentThicknessM);
            }
        }

        public void SetColor(Color color)
        {
            foreach (GameObject segment in _segments)
            {
                PrimitiveFactory.SetColor(segment, color);
            }

            PrimitiveFactory.SetColor(_center, color);
        }

        public void Show(Vector2 plate)
        {
            _root.gameObject.SetActive(true);
            _root.localPosition = new Vector3(plate.x, plate.y, FrontOffsetM);
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }
    }
}
