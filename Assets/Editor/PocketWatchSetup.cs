using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PocketWatchSetup
{
    private const string ModelPath = "Assets/ExternalModels/PocketWatchSource/Pocket Watch.fbx";
    private const string PrefabPath = "Assets/Prefabs/SmallObjects/PocketWatch.prefab";
    private const string ControllerPath = "Assets/ExternalModels/PocketWatchSource/PocketWatchOpening.controller";

    [MenuItem("Tools/Pocket Watch/Configure Opening Animation")]
    public static void Configure()
    {
        AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);
        Object[] importedAssets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
        AnimationClip clip = importedAssets.OfType<AnimationClip>()
            .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__"));

        if (clip == null)
        {
            Debug.LogError("Pocket watch FBX imported without an animation clip.");
            return;
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }

        AnimatorState state = controller.layers[0].stateMachine.states
            .Select(entry => entry.state)
            .FirstOrDefault(existing => existing.name == "Pickup");
        if (state == null)
        {
            state = controller.layers[0].stateMachine.AddState("Pickup");
        }
        state.motion = clip;
        state.speed = 1f;
        controller.layers[0].stateMachine.defaultState = state;
        EditorUtility.SetDirty(controller);

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            foreach (Transform child in prefabRoot.transform.Cast<Transform>().ToArray())
            {
                Object.DestroyImmediate(child.gameObject);
            }

            GameObject animatedModel = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), prefabRoot.transform);
            animatedModel.name = "AnimatedPocketWatch";

            Animator animator = prefabRoot.GetComponent<Animator>();
            if (animator == null) animator = prefabRoot.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Configured pocket watch opening animation: {clip.name}");
    }
}