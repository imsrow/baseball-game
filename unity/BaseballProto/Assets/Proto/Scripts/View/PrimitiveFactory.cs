using System.Collections.Generic;
using UnityEngine;

namespace BaseballProto.View
{
    /// <summary>
    /// 큐브·구체 생성 도우미.
    /// - 머티리얼: Resources/ProtoLit(URP Lit)을 복제해 색만 바꾼다. 씬에 참조가 없으면 빌드에서 셰이더가 빠져
    ///   분홍색으로 나오므로, Resources에 둔 머티리얼로 셰이더를 빌드에 포함시킨다 (에디터 메뉴가 생성).
    /// - 메시: CreatePrimitive는 충돌체를 붙이는데 물리 모듈이 빌드에서 빠지면 오류가 나므로 내장 메시를 직접 쓴다.
    /// </summary>
    public static class PrimitiveFactory
    {
        public const string MaterialResource = "ProtoLit";

        private static readonly Dictionary<PrimitiveType, Mesh> Meshes = new Dictionary<PrimitiveType, Mesh>();
        private static Material s_baseMaterial;

        public static GameObject Create(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale,
            Color color)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = MeshFor(type);
            go.AddComponent<MeshRenderer>().sharedMaterial = BaseMaterial();
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

        private static Mesh MeshFor(PrimitiveType type)
        {
            if (Meshes.TryGetValue(type, out Mesh mesh) && mesh != null)
            {
                return mesh;
            }

            mesh = Resources.GetBuiltinResource<Mesh>(type == PrimitiveType.Sphere ? "Sphere.fbx" : "Cube.fbx");
            if (mesh == null)
            {
                // 내장 메시를 못 찾으면 한 번만 CreatePrimitive로 얻는다
                GameObject temp = GameObject.CreatePrimitive(type);
                mesh = temp.GetComponent<MeshFilter>().sharedMesh;
                Object.Destroy(temp);
            }

            Meshes[type] = mesh;
            return mesh;
        }

        private static Material BaseMaterial()
        {
            if (s_baseMaterial == null)
            {
                s_baseMaterial = Resources.Load<Material>(MaterialResource);
                if (s_baseMaterial == null)
                {
                    Debug.LogError("Resources/" + MaterialResource + " 머티리얼이 없습니다. 메뉴 Baseball > Setup Proto Scene을 실행하세요.");
                    s_baseMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                }
            }

            return s_baseMaterial;
        }
    }
}
