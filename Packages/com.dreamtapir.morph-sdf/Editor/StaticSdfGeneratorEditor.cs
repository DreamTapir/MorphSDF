#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MorphSDF.Editor
{
    [CustomEditor(typeof(StaticSdfGenerator))]
    public class StaticSdfGeneratorEditor : UnityEditor.Editor
    {
        private StaticSdfGenerator _saveManager;
        
        
        private void OnEnable()
        {
            if (target != null)
            {
                ((StaticSdfGenerator)target).SetEditorFocus(true);
            }
        }

        private void OnDisable()
        {
            if (target != null)
            {
                ((StaticSdfGenerator)target).SetEditorFocus(false);
            }
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Update Preview", GUILayout.Height(30)))
            {
                ((StaticSdfGenerator)target).Bake();
            }

            if (GUILayout.Button("Bake SDF to Asset", GUILayout.Height(30)))
            {
                BakeToAsset();
            }
        }

        private void BakeToAsset()
        {
            var generator = (StaticSdfGenerator)target;
            
            var sdfProperty = serializedObject.FindProperty("_sdf");
            var renderTexture = sdfProperty.objectReferenceValue as RenderTexture;

            if (renderTexture == null)
            {
                Debug.LogError("[Morph-SDF] SDF texture has not been generated yet.");
                return;
            }

            string path = EditorUtility.SaveFilePanelInProject(
                "Save SDF Texture",
                $"{generator.gameObject.name}_SDF",
                "asset",
                "Please select a destination to save the SDF texture"
            );

            if (string.IsNullOrEmpty(path)) return;

            AssetTool.SaveRenderTexture3DToAsset(renderTexture, path);
        }
    }
}
#endif