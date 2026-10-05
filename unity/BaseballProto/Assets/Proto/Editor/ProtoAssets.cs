using System.IO;
using BaseballProto.View;
using UnityEditor;
using UnityEngine;

namespace BaseballProto.EditorTools
{
    /// <summary>
    /// 빌드에 필요한 에셋 생성. 코드로만 만든 오브젝트는 씬에 머티리얼 참조가 없어서
    /// 빌드 때 URP Lit 셰이더가 빠지므로 Resources 폴더에 머티리얼을 둔다.
    /// </summary>
    public static class ProtoAssets
    {
        private const string Folder = "Assets/Proto/Resources";
        private const string LitShader = "Universal Render Pipeline/Lit";
        private const float Smoothness = 0.2f;

        public static void EnsureMaterial()
        {
            string path = Folder + "/" + PrimitiveFactory.MaterialResource + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            {
                return;
            }

            Shader shader = Shader.Find(LitShader);
            if (shader == null)
            {
                Debug.LogError("셰이더를 찾을 수 없습니다: " + LitShader);
                return;
            }

            Directory.CreateDirectory(Folder);
            var material = new Material(shader);
            material.SetFloat("_Smoothness", Smoothness);
            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            Debug.Log("머티리얼 생성: " + path);
        }
    }
}
