using UnityEngine;

namespace BaseballProto.View
{
    /// <summary>
    /// 배트 (얇은 큐브). 우타자 기준 각도: +100°(포수 쪽 뒤) → 0°(홈플레이트 가로지름) → −120°(팔로스루)
    /// </summary>
    public sealed class BatView
    {
        private const float BatLengthM = 0.85f;
        private const float BatThicknessM = 0.06f;
        private const float PivotOffsetXM = 0.55f;
        private const float PivotHeightM = 0.95f;
        private const float RestAngleDeg = 100f;
        private const float FinishAngleDeg = -120f;
        private const float RestTiltDeg = 35f;

        private readonly Transform _pivot;
        private float _side = -1f;
        private float _elapsed = -1f;
        private float _duration = 0.18f;

        public BatView(Transform parent)
        {
            _pivot = new GameObject("BatPivot").transform;
            _pivot.SetParent(parent, false);
            GameObject bat = PrimitiveFactory.Create(PrimitiveType.Cube, "Bat", _pivot, new Vector3(BatLengthM * 0.5f, 0f, 0f),
                new Vector3(BatLengthM, BatThicknessM, BatThicknessM), ProtoColors.Bat);
            bat.name = "Bat";
            Pose(RestAngleDeg, RestTiltDeg);
        }

        /// <summary>타자 방향 설정 (우타자 −1, 좌타자 +1)</summary>
        public void SetSide(float side)
        {
            _side = side;
            _elapsed = -1f;
            Pose(RestAngleDeg, RestTiltDeg);
        }

        public void Swing(float duration)
        {
            _duration = Mathf.Max(0.05f, duration);
            _elapsed = 0f;
        }

        public void ResetPose()
        {
            _elapsed = -1f;
            Pose(RestAngleDeg, RestTiltDeg);
        }

        public void Tick(float deltaTime)
        {
            if (_elapsed < 0f)
            {
                return;
            }

            _elapsed += deltaTime;
            float u = Mathf.Clamp01(_elapsed / _duration);
            float eased = 1f - (1f - u) * (1f - u);
            float angle = Mathf.Lerp(RestAngleDeg, FinishAngleDeg, eased);
            float tilt = Mathf.Lerp(RestTiltDeg, 0f, Mathf.Clamp01(u * 2f));
            Pose(angle, tilt);
        }

        private void Pose(float angleRightHanded, float tiltDeg)
        {
            _pivot.localPosition = new Vector3(_side * PivotOffsetXM, PivotHeightM, 0f);

            // 배트 기본 방향(로컬 +x)을 홈플레이트 쪽(−side)으로 두고 Y축 회전, 좌타자는 각도 반전
            float yaw = -_side * angleRightHanded;
            float baseYaw = _side < 0f ? 0f : 180f;
            _pivot.localRotation = Quaternion.Euler(0f, baseYaw + yaw, tiltDeg);
        }
    }
}
