using UnityEngine;

namespace BaseballProto.View
{
    /// <summary>
    /// 큐브·구체 생성 도우미. 충돌체는 쓰지 않으므로 제거한다.
    /// 머티리얼은 렌더 파이프라인 기본 머티리얼을 복제해 색만 바꾼다 (빌드에서 셰이더가 빠지지 않도록).
    /// </summary>
    public static class PrimitiveFactory
    {
        public static GameObject Create(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale,
            Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            SetColor(go, color);
            return go;
        }

        public static void SetColor(GameObject go, Color color)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            renderer.material.color = color;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
