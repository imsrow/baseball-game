using UnityEngine;

namespace BaseballProto.View
{
    /// <summary>
    /// 화면 좌표 ↔ 홈플레이트 평면(z = 0) 좌표 변환.
    /// 월드 x = 엔진 PlateLocation.X (포수 시점 1루 쪽 +), 월드 y = 엔진 PlateLocation.Z (높이)
    /// </summary>
    public sealed class PlateMapper
    {
        private const float ProbePixels = 100f;

        private readonly Camera _camera;

        public PlateMapper(Camera camera)
        {
            _camera = camera;
        }

        public bool TryScreenToPlate(Vector2 screen, out Vector2 plate)
        {
            Ray ray = _camera.ScreenPointToRay(screen);
            plate = Vector2.zero;
            if (Mathf.Abs(ray.direction.z) < 1e-5f)
            {
                return false;
            }

            float t = -ray.origin.z / ray.direction.z;
            if (t < 0f)
            {
                return false;
            }

            Vector3 p = ray.origin + ray.direction * t;
            plate = new Vector2(p.x, p.y);
            return true;
        }

        /// <summary>홈플레이트 평면에서 화면 1픽셀이 몇 m인지 (드래그 이동량 변환용)</summary>
        public float MetersPerPixel()
        {
            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (TryScreenToPlate(center, out Vector2 a) && TryScreenToPlate(center + new Vector2(ProbePixels, 0f), out Vector2 b))
            {
                return Mathf.Abs(b.x - a.x) / ProbePixels;
            }

            return 0.002f;
        }
    }
}
