using BaseballProto.Core;
using BaseballSim.Engine.Config;
using BaseballSim.Engine.Players;
using UnityEngine;

namespace BaseballProto.View
{
    /// <summary>
    /// 경기장(큐브 수준)과 포수 뒤 카메라 구성.
    /// 좌표: 홈플레이트 앞면 중심이 원점, +z는 투수 방향, +x는 1루 쪽, +y는 위.
    /// </summary>
    public sealed class FieldView
    {
        private const float LineThicknessM = 0.008f;
        private const float BatterOffsetXM = 0.85f;
        private const float MoundDistanceM = 18.44f;

        private readonly ProtoTuning _tuning;
        private readonly GameObject _batter;
        private readonly GameObject _pitcher;

        public FieldView(Transform root, Camera camera, StrikeZoneConfig zone, ProtoTuning tuning)
        {
            _tuning = tuning;
            Root = root;
            Camera = camera;

            PrimitiveFactory.Create(PrimitiveType.Cube, "Grass", root, new Vector3(0f, -0.06f, 60f),
                new Vector3(160f, 0.1f, 160f), ProtoColors.Grass);
            PrimitiveFactory.Create(PrimitiveType.Cube, "HomeDirt", root, new Vector3(0f, -0.05f, 0f),
                new Vector3(5f, 0.1f, 5f), ProtoColors.Dirt);
            PrimitiveFactory.Create(PrimitiveType.Cube, "Mound", root, new Vector3(0f, -0.02f, MoundDistanceM),
                new Vector3(5f, 0.1f, 5f), ProtoColors.Dirt);
            PrimitiveFactory.Create(PrimitiveType.Cube, "Plate", root, new Vector3(0f, 0f, -0.2f),
                new Vector3(0.43f, 0.02f, 0.43f), ProtoColors.Chalk);

            BuildZoneFrame(root, zone);

            _pitcher = PrimitiveFactory.Create(PrimitiveType.Cube, "Pitcher", root, new Vector3(0f, 0.95f, MoundDistanceM - 0.5f),
                new Vector3(0.5f, 1.9f, 0.35f), ProtoColors.Pitcher);
            _batter = PrimitiveFactory.Create(PrimitiveType.Cube, "Batter", root, new Vector3(-BatterOffsetXM, 0.9f, -0.1f),
                new Vector3(0.4f, 1.8f, 0.3f), ProtoColors.Batter);

            ApplyCamera();
        }

        public Transform Root { get; }

        public Camera Camera { get; }

        /// <summary>타자 위치 부호: 우타자는 3루 쪽(−1), 좌타자는 1루 쪽(+1)</summary>
        public float BatterSide { get; private set; } = -1f;

        public void SetBatter(Hand battingHand)
        {
            BatterSide = battingHand == Hand.Right ? -1f : 1f;
            Vector3 p = _batter.transform.localPosition;
            _batter.transform.localPosition = new Vector3(BatterSide * BatterOffsetXM, p.y, p.z);
        }

        public void SetPitcherHand(Hand throws)
        {
            Vector3 p = _pitcher.transform.localPosition;
            float side = throws == Hand.Right ? -0.15f : 0.15f;
            _pitcher.transform.localPosition = new Vector3(side, p.y, p.z);
        }

        /// <summary>
        /// 화면 비율에 맞춰 FOV를 정한다. 홈플레이트 위치에서 폭(PlateViewWidthM)과 높이(PlateViewHeightM)가 모두 들어오게:
        /// 세로 화면은 폭에 맞춰지고, 가로 화면은 높이에 맞춰져 좌우 시야가 넓어진다.
        /// </summary>
        public void ApplyCamera()
        {
            Camera.transform.localPosition = new Vector3(0f, _tuning.CameraHeightM, -_tuning.CameraBackM);
            float lookHeight = Camera.aspect > 1f ? _tuning.CameraLookHeightLandscapeM : _tuning.CameraLookHeightM;
            Camera.transform.LookAt(new Vector3(0f, lookHeight, _tuning.CameraLookAheadM));
            float horizontalFov = 2f * Mathf.Atan(_tuning.PlateViewWidthM * 0.5f / _tuning.CameraBackM) * Mathf.Rad2Deg;
            float fromWidth = Camera.HorizontalToVerticalFieldOfView(horizontalFov, Mathf.Max(0.1f, Camera.aspect));
            float fromHeight = 2f * Mathf.Atan(_tuning.PlateViewHeightM * 0.5f / _tuning.CameraBackM) * Mathf.Rad2Deg;
            Camera.fieldOfView = Mathf.Max(_tuning.MinVerticalFovDeg, Mathf.Max(fromWidth, fromHeight));
        }

        private static void BuildZoneFrame(Transform root, StrikeZoneConfig zone)
        {
            float w = (float)zone.HalfWidthM;
            float bottom = (float)zone.BottomM;
            float top = (float)zone.TopM;
            float height = top - bottom;
            Transform frame = new GameObject("StrikeZone").transform;
            frame.SetParent(root, false);

            Line(frame, new Vector3(0f, top, 0f), new Vector3(2f * w, LineThicknessM, LineThicknessM));
            Line(frame, new Vector3(0f, bottom, 0f), new Vector3(2f * w, LineThicknessM, LineThicknessM));
            Line(frame, new Vector3(-w, bottom + height * 0.5f, 0f), new Vector3(LineThicknessM, height, LineThicknessM));
            Line(frame, new Vector3(w, bottom + height * 0.5f, 0f), new Vector3(LineThicknessM, height, LineThicknessM));
        }

        private static void Line(Transform parent, Vector3 position, Vector3 scale)
        {
            PrimitiveFactory.Create(PrimitiveType.Cube, "ZoneLine", parent, position, scale, ProtoColors.Zone);
        }
    }
}
