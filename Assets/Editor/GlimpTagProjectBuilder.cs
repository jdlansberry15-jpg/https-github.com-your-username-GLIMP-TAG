using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

/// <summary>
/// Automatically prepares the GLIMP TAG scene list, Android player settings,
/// and OpenXR loader/features when the Unity project opens or a build begins.
/// </summary>
[InitializeOnLoad]
public sealed class GlimpTagProjectBuilder : IPreprocessBuildWithReport
{
    private const string ScenePath = "Assets/Scenes/GLIMP_TAG.unity";
    private const string XrSettingsFolder = "Assets/XR";
    private const string XrSettingsAsset = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";

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
            EnsureFolder("Assets/Scenes");
            EnsureScene();
            ConfigureAndroidPlayer();
            ConfigureOpenXR();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
        PlayerSettings.runInBackground = true;

        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;

        // Quest sideload builds should be APKs, not Play Store app bundles.
        EditorUserBuildSettings.buildAppBundle = false;
    }

    private static void ConfigureOpenXR()
    {
        const BuildTargetGroup group = BuildTargetGroup.Android;

        if (!EditorBuildSettings.TryGetConfigObject(
            XRGeneralSettings.k_SettingsKey,
            out XRGeneralSettingsPerBuildTarget perTarget) || perTarget == null)
        {
            EnsureFolder(XrSettingsFolder);
            perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(perTarget, XrSettingsAsset);
            EditorBuildSettings.AddConfigObject(
                XRGeneralSettings.k_SettingsKey, perTarget, true);
        }

        if (!perTarget.HasSettingsForBuildTarget(group))
            perTarget.CreateDefaultSettingsForBuildTarget(group);

        if (!perTarget.HasManagerSettingsForBuildTarget(group))
            perTarget.CreateDefaultManagerSettingsForBuildTarget(group);

        XRManagerSettings manager = perTarget.ManagerSettingsForBuildTarget(group);
        if (manager != null)
        {
            bool assigned = XRPackageMetadataStore.AssignLoader(
                manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group);

            if (!assigned)
                Debug.LogWarning("GLIMP TAG: OpenXR loader could not be assigned automatically. Check XR Plug-in Management settings.");
            EditorUtility.SetDirty(manager);
        }

        XRGeneralSettings generalSettings = perTarget.SettingsForBuildTarget(group);
        if (generalSettings != null)
        {
            generalSettings.InitManagerOnStart = true;
            EditorUtility.SetDirty(generalSettings);
        }

        OpenXRSettings openXrSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
        if (openXrSettings != null)
        {
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
        else
        {
            Debug.LogWarning("GLIMP TAG: OpenXR settings asset was not available yet. The project builder will retry on the next Editor load.");
        }

        EditorUtility.SetDirty(perTarget);
    }
}
