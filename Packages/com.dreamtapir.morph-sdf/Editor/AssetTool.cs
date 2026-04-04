using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MorphSDF.Editor
{
    public static class AssetTool
    {
#if UNITY_EDITOR
        /// <summary>
        /// Saves a 3D RenderTexture as a Texture3D asset
        /// </summary>
        public static void SaveRenderTexture3DToAsset(RenderTexture rt, string path)
        {
            try
            {
                EditorUtility.DisplayProgressBar("Baking RenderTexture", "Waiting for GPU readback...", 0.0f);

                int width = rt.width;
                int height = rt.height;
                int depth = rt.volumeDepth;
                
                var format = rt.format == RenderTextureFormat.RHalf ? UnityEngine.TextureFormat.RHalf : UnityEngine.TextureFormat.RFloat;
                Texture3D tex3D = new Texture3D(rt.width, rt.height, rt.volumeDepth, format, mipChain: false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear
                };

                var request = AsyncGPUReadback.Request(rt);
                request.WaitForCompletion();

                if (request.hasError)
                {
                    Debug.LogError("An error occurred during GPU Readback.");
                    return;
                }

                // Calculate bytes per pixel (RHalf = 2 bytes, RFloat = 4 bytes)
                int bytesPerPixel = (rt.format == RenderTextureFormat.RHalf) ? 2 : 4;
                int sliceSize = width * height * bytesPerPixel;
                int totalSize = sliceSize * depth;

                // Temporarily allocate an array to store data for all slices
                using (var allData = new NativeArray<byte>(totalSize, Allocator.Temp))
                {
                    // Retrieve data for each Z-depth (slice) and merge it into allData
                    for (int z = 0; z < depth; z++)
                    {
                        float progress = (float)z / depth;
                        EditorUtility.DisplayProgressBar("Baking RenderTexture", $"Processing slice {z + 1} / {depth}...", progress);

                        var sliceData = request.GetData<byte>(z);
                        NativeArray<byte>.Copy(sliceData, 0, allData, z * sliceSize, sliceSize);
                    }

                    EditorUtility.DisplayProgressBar("Baking RenderTexture", "Applying texture data...", 1.0f);

                    // Feed the combined large array into the Texture3D
                    tex3D.SetPixelData(allData, 0);
                    tex3D.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                }

                AssetDatabase.CreateAsset(tex3D, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"Texture3D asset saved successfully! Path: {path}");
                
                EditorGUIUtility.PingObject(tex3D);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
#endif
    }
}