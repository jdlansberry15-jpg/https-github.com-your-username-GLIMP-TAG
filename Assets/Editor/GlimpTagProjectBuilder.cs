using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

/// <summary>
/// Automatically prepares the GLIMP TAG scene list, network player prefab,
/// Android player settings, and OpenXR loader/features for cloud builds.
/// </summary>
[InitializeOnLoad]
public sealed class GlimpTagProjectBuilder : IPreprocessBuildWithReport
{
    private const string ScenePath = "Assets/Scenes/GLIMP_TAG.unity";
    private const string XrSettingsAssetPath = "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset";
    private static bool isPreparing;

    static GlimpTagProjectBuilder()
    {
        EditorApplication.delayCall += EnsureProjectReady;
    }

    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        EnsureProjectReady();
    }

    [MenuItem("GLIMP TAG/Prepare Quest Project")]
    public static void PrepareFromMenu()
    {
        EnsureProjectReady();
        Debug.Log("GLIMP TAG: Quest project preparation finished.");
    }

    /// <summary>
    /// Explicit Unity Build Automation pre-export hook. Configure the Android
    /// target's Pre-Export Method Name as "PreExport" in Unity Dashboard.
    /// This runs after Editor script compilation and fails the cloud build if
    /// the generated scene, network prefab, Android settings, or XR setup fail.
    /// </summary>
    public static void PreExport()
    {
        if (isPreparing)
            throw new BuildFailedException("GLIMP TAG project preparation is already running.");

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new BuildFailedException("GLIMP TAG pre-export ran before Unity finished compiling/updating assets.");

        isPreparing = true;
        try
        {
            PrepareProject();
            AssetDatabase.SaveAssets();
            Debug.Log("GLIMP TAG: Unity Build Automation pre-export preparation completed.");
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            throw new BuildFailedException("GLIMP TAG cloud pre-export preparation failed: " + exception.Message);
        }
        finally
        {
            isPreparing = false;
        }
    }

    private static void EnsureProjectReady()
    {
        if (isPreparing || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += EnsureProjectReady;
            return;
        }

        isPreparing = true;
        try
        {
            PrepareProject();
        }
        catch (System.Exception exception)
        {
            Debug.LogError("GLIMP TAG project preparation failed: " + exception);
        }
        finally
        {
            isPreparing = false;
        }
    }

    private static void PrepareProject()
    {
        EnsureFolder("Assets/Scenes");
        EnsureScene();
        GlimpTagNetworkPrefabBuilder.EnsureNetworkPlayerPrefabAndScene(ScenePath);
        ConfigureAndroidPlayer();
        ConfigureOpenXR();
        AssetDatabase.SaveAssets();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folderName = Path.GetFileName(path);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        if (!string.IsNullOrEmpty(parent) && AssetDatabase.IsValidFolder(parent))
            AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void EnsureScene()
    {
        if (!File.Exists(ScenePath))
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject bootstrapObject = new GameObject("GLIMP TAG Runtime Bootstrap");
            bootstrapObject.AddComponent<GlimpTagRuntimeBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        List<EditorBuildSettingsScene> scenes =
            new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        bool found = false;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == ScenePath)
            {
                scenes[i] = new EditorBuildSettingsScene(ScenePath, true);
                found = true;
                break;
            }
        }

        if (!found)
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void ConfigureAndroidPlayer()
    {
        PlayerSettings.productName = "GLIMP TAG";
        PlayerSettings.companyName = "GLIMP";
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.glimp.tag");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Low);
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.colorSpace = ColorSpace.Gamma;

        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;

        // Quest sideload builds should be APKs, not Play Store app bundles.
        EditorUserBuildSettings.buildAppBundle = false;
    }

    private static void ConfigureOpenXR()
    {
        const BuildTargetGroup group = BuildTargetGroup.Android;

        XRGeneralSettingsPerBuildTarget perTarget;
        if (!EditorBuildSettings.TryGetConfigObject(
            XRGeneralSettings.k_SettingsKey, out perTarget) || perTarget == null)
        {
            string[] existingAssets = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            if (existingAssets.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(existingAssets[0]);
                perTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(assetPath);
            }

            if (perTarget == null)
            {
                EnsureFolder("Assets/XR/Settings");
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, XrSettingsAssetPath);
            }

            EditorBuildSettings.AddConfigObject(
                XRGeneralSettings.k_SettingsKey, perTarget, true);
        }

        if (!perTarget.HasSettingsForBuildTarget(group))
            perTarget.CreateDefaultSettingsForBuildTarget(group);

        if (!perTarget.HasManagerSettingsForBuildTarget(group))
            perTarget.CreateDefaultManagerSettingsForBuildTarget(group);

        XRGeneralSettings generalSettings = perTarget.SettingsForBuildTarget(group);
        XRManagerSettings manager = perTarget.ManagerSettingsForBuildTarget(group);

        if (generalSettings == null || manager == null)
        {
            Debug.LogWarning("GLIMP TAG: Could not create Android XR settings. Check XR Plug-in Management package resolution.");
            return;
        }

        bool assigned = XRPackageMetadataStore.AssignLoader(
            manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group);

        if (!assigned)
            Debug.LogWarning("GLIMP TAG: OpenXR loader could not be assigned automatically. Check XR Plug-in Management settings.");

        SerializedObject generalSerialized = new SerializedObject(generalSettings);
        SerializedProperty initializeOnStart = generalSerialized.FindProperty("m_InitManagerOnStart");
        if (initializeOnStart != null)
        {
            initializeOnStart.boolValue = true;
            generalSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(generalSettings);

        OpenXRSettings openXrSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
        if (openXrSettings == null)
        {
            Debug.LogWarning("GLIMP TAG: OpenXR settings asset is not available yet. The project builder will retry on the next Editor load.");
            return;
        }

        openXrSettings.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
        OpenXRFeature[] features = openXrSettings.GetFeatures<OpenXRFeature>();
        for (int i = 0; i < features.Length; i++)
        {
            OpenXRFeature feature = features[i];
            if (feature == null)
                continue;

            string typeName = feature.GetType().Name;
            bool requiredForQuest = typeName == "MetaQuestFeature" ||
                typeName == "OculusTouchControllerProfile" ||
                typeName == "MetaQuestTouchPlusControllerProfile" ||
                typeName == "HandTracking" ||
                typeName == "MetaHandTrackingAim";

            if (requiredForQuest)
            {
                feature.enabled = true;
                EditorUtility.SetDirty(feature);
            }
        }

        EditorUtility.SetDirty(openXrSettings);
    }
}
