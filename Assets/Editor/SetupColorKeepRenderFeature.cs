using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// One-off setup: adds a RenderObjects feature to the active (PC) renderer that redraws
// everything on the "ColorKeep" layer AFTER the whole post-process stack (including the
// global greyscale Volume) has already run, so those objects show their real texture color
// while the rest of the game stays desaturated. No second camera involved.
public static class SetupColorKeepRenderFeature
{
    const string RendererPath = "Assets/Settings/PC_Renderer.asset";
    const string FeatureName = "ColorKeep_RenderObjects";
    const string LayerName = "ColorKeep";

    [MenuItem("Tools/Dear Anna/Setup ColorKeep Render Feature")]
    public static void Run()
    {
        var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        if (rendererData == null)
        {
            Debug.LogError("Could not load UniversalRendererData at " + RendererPath);
            return;
        }

        foreach (var existing in rendererData.rendererFeatures)
        {
            if (existing != null && existing.name == FeatureName)
            {
                Debug.Log("ColorKeep feature already present, skipping.");
                return;
            }
        }

        int layer = LayerMask.NameToLayer(LayerName);
        if (layer < 0)
        {
            Debug.LogError("Layer '" + LayerName + "' does not exist. Create it first.");
            return;
        }

        var feature = ScriptableObject.CreateInstance<RenderObjects>();
        feature.name = FeatureName;
        feature.hideFlags |= HideFlags.HideInHierarchy;

        feature.settings.Event = RenderPassEvent.AfterRenderingPostProcessing;
        feature.settings.filterSettings.LayerMask = 1 << layer;
        feature.settings.filterSettings.RenderQueueType = RenderQueueType.Opaque;
        feature.settings.overrideMode = RenderObjects.RenderObjectsSettings.OverrideMaterialMode.None;
        feature.settings.overrideMaterial = null;
        feature.settings.overrideShader = null;

        AssetDatabase.AddObjectToAsset(feature, rendererData);
        rendererData.rendererFeatures.Add(feature);
        rendererData.SetDirty();

        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("ColorKeep RenderObjects feature added to PC_Renderer.asset.");
    }
}
