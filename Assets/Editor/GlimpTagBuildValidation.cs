using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

/// <summary>
/// Runs before an Android build and checks that the generated Quest player,
/// scene, network manager, and OpenXR configuration are wired.
/// </summary>
public sealed class GlimpTagBuildValidation : IPreprocessBuildWithReport
{
    private const string ScenePath = "Assets/Scenes/GLIMP_TAG.unity";
    private const string PlayerPrefabPath = "Assets/Resources/GLIMP_TAG/NetworkPlayer.prefab";

    public int callbackOrder => -900;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            throw new BuildFailedException("GLIMP TAG is configured for Meta Quest Android builds. Select Android as the Build Automation target.");

        ValidateProject();
    }

    [MenuItem("GLIMP TAG/Validate Quest Build Setup")]
    public static void ValidateFromMenu()
    {
        try
        {
            ValidateProject();
            Debug.Log("GLIMP TAG: Quest build preflight passed. Unity compilation and device testing are still required.");
        }
        catch (BuildFailedException exception)
        {
            Debug.LogError(exception.Message);
        }
    }

    private static void ValidateProject()
    {
        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();

        if (EditorUserBuildSettings.buildAppBundle)
            errors.Add("Android App Bundle output is enabled. Turn it off to produce a sideloadable APK.");

        if (PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android) != ScriptingImplementation.IL2CPP)
            errors.Add("Android scripting backend must be IL2CPP.");

        if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
            errors.Add("Android ARM64 architecture is not enabled.");

        bool sceneInBuild = false;
        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (buildScene.path == ScenePath && buildScene.enabled)
            {
                sceneInBuild = true;
                break;
            }
        }
        if (!sceneInBuild)
            errors.Add("The GLIMP TAG main scene is missing or disabled in Build Settings.");

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (playerPrefab == null)
        {
            errors.Add("Generated NetworkPlayer prefab is missing. Run GLIMP TAG > Prepare Quest Project.");
        }
        else
        {
            ValidatePlayerPrefab(playerPrefab, errors);
        }

        if (System.IO.File.Exists(ScenePath))
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateMainScene(playerPrefab, errors);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        else
        {
            errors.Add("Generated GLIMP TAG scene is missing: " + ScenePath);
        }

        ValidateOpenXR(errors, warnings);

        if (warnings.Count > 0)
        {
            foreach (string warning in warnings)
                Debug.LogWarning("GLIMP TAG preflight: " + warning);
        }

        if (errors.Count > 0)
        {
            throw new BuildFailedException(
                "GLIMP TAG pre-build validation failed:\n - " +
                string.Join("\n - ", errors.ToArray()));
        }
    }

    private static void ValidatePlayerPrefab(GameObject prefab, List<string> errors)
    {
        Require(prefab.GetComponent<NetworkObject>() != null, "Player prefab root needs NetworkObject.", errors);
        Require(prefab.GetComponent<Rigidbody>() != null, "Player prefab root needs Rigidbody.", errors);
        Require(prefab.GetComponent<CapsuleCollider>() != null, "Player prefab root needs CapsuleCollider.", errors);
        Require(prefab.GetComponent<GlimpTagNetworkPlayer>() != null, "Player prefab needs GlimpTagNetworkPlayer.", errors);
        Require(prefab.GetComponent<GlimpTagOwnerNetworkTransform>() != null, "Player prefab needs GlimpTagOwnerNetworkTransform.", errors);
        Require(prefab.GetComponent<GlimpTagNetworkAvatarPoseSync>() != null, "Player prefab needs GlimpTagNetworkAvatarPoseSync.", errors);
        Require(prefab.GetComponent<GlimpTagNetworkPlayerRig>() != null, "Player prefab needs GlimpTagNetworkPlayerRig.", errors);
        Require(prefab.GetComponent<GlimpTagXRHandInput>() != null, "Player prefab needs GlimpTagXRHandInput.", errors);
        Require(prefab.GetComponent<GlimpTagXRCameraDriver>() != null, "Player prefab needs GlimpTagXRCameraDriver.", errors);
        Require(prefab.GetComponent<GlimpTagLocomotion>() != null, "Player prefab needs GlimpTagLocomotion.", errors);
        Require(prefab.GetComponent<GlimpTagStartMenu>() != null, "Player prefab needs GlimpTagStartMenu.", errors);

        Transform trackingSpace = prefab.transform.Find("Tracking Space");
        Require(trackingSpace != null, "Player prefab is missing Tracking Space.", errors);

        if (trackingSpace != null)
        {
            ValidateHandSource(trackingSpace, "Left Hand Tracking Proxy", errors);
            ValidateHandSource(trackingSpace, "Right Hand Tracking Proxy", errors);
            Transform cameraTransform = trackingSpace.Find("Main Camera");
            Require(cameraTransform != null, "Player prefab is missing its Main Camera.", errors);
            if (cameraTransform != null)
            {
                Require(cameraTransform.GetComponent<Camera>() != null, "Player Main Camera component is missing.", errors);
                Require(cameraTransform.GetComponent<AudioListener>() != null, "Player AudioListener is missing.", errors);
            }
        }

        Transform remoteVisuals = prefab.transform.Find("Remote Avatar Visuals");
        Require(remoteVisuals != null, "Player prefab is missing Remote Avatar Visuals.", errors);
        if (remoteVisuals != null)
        {
            Require(remoteVisuals.Find("Remote Head") != null, "Remote head visual is missing.", errors);
            Require(remoteVisuals.Find("Remote Left Hand") != null, "Remote left hand visual is missing.", errors);
            Require(remoteVisuals.Find("Remote Right Hand") != null, "Remote right hand visual is missing.", errors);
        }

        GlimpTagNetworkAvatarPoseSync poseSync = prefab.GetComponent<GlimpTagNetworkAvatarPoseSync>();
        if (poseSync != null)
        {
            ValidateSerializedReferences(poseSync, errors,
                "headSource", "leftHandSource", "rightHandSource",
                "remoteHead", "remoteLeftHand", "remoteRightHand");
        }

        GlimpTagNetworkPlayerRig rig = prefab.GetComponent<GlimpTagNetworkPlayerRig>();
        if (rig != null)
        {
            ValidateSerializedReferences(rig, errors,
                "playerBody", "localCamera", "localAudioListener", "trackingSpace",
                "leftHandSource", "rightHandSource", "leftHandTrigger", "rightHandTrigger",
                "leftHandTagger", "rightHandTagger", "leftHandSourceRenderer",
                "rightHandSourceRenderer", "handInput", "cameraDriver",
                "locomotion", "startMenu", "remoteAvatarRoot");
        }

        GlimpTagNetworkPlayer networkPlayer = prefab.GetComponent<GlimpTagNetworkPlayer>();
        if (networkPlayer != null)
        {
            SerializedObject serialized = new SerializedObject(networkPlayer);
            SerializedProperty renderers = serialized.FindProperty("colorRenderers");
            Require(renderers != null && renderers.arraySize > 0 &&
                renderers.GetArrayElementAtIndex(0).objectReferenceValue != null,
                "Network player avatar color renderer is not assigned.", errors);
        }
    }

    private static void ValidateHandSource(Transform trackingSpace, string handName, List<string> errors)
    {
        Transform hand = trackingSpace.Find(handName);
        Require(hand != null, handName + " is missing.", errors);
        if (hand == null)
            return;

        SphereCollider sphere = hand.GetComponent<SphereCollider>();
        Require(sphere != null && sphere.isTrigger, handName + " needs a trigger SphereCollider.", errors);
        Require(hand.GetComponent<GlimpTagNetworkTagger>() != null,
            handName + " needs GlimpTagNetworkTagger.", errors);
    }

    private static void ValidateMainScene(GameObject playerPrefab, List<string> errors)
    {
        GlimpTagRuntimeBootstrap bootstrap = Object.FindObjectOfType<GlimpTagRuntimeBootstrap>();
        Require(bootstrap != null, "Main scene is missing GlimpTagRuntimeBootstrap.", errors);
        if (bootstrap != null)
        {
            Require(bootstrap.buildArenaOnStart, "Forest arena generation is disabled.", errors);
            Require(!bootstrap.createPlayerRig,
                "The old offline player rig must be disabled when using the network player prefab.", errors);
        }

        GameObject managerObject = GameObject.Find("GLIMP TAG Network Manager");
        Require(managerObject != null, "Main scene is missing GLIMP TAG Network Manager.", errors);
        if (managerObject == null)
            return;

        NetworkManager manager = managerObject.GetComponent<NetworkManager>();
        UnityTransport transport = managerObject.GetComponent<UnityTransport>();
        Require(manager != null, "NetworkManager component is missing from the network manager object.", errors);
        Require(transport != null, "UnityTransport component is missing from the network manager object.", errors);

        if (manager != null)
        {
            Require(manager.NetworkConfig != null,
                "NetworkManager has no NetworkConfig.", errors);
            if (manager.NetworkConfig != null)
            {
                if (transport != null)
                    Require(manager.NetworkConfig.NetworkTransport == transport,
                        "NetworkManager NetworkTransport is not assigned to the scene UnityTransport.", errors);
                if (playerPrefab != null)
                    Require(manager.NetworkConfig.PlayerPrefab == playerPrefab,
                        "NetworkManager PlayerPrefab does not reference the generated NetworkPlayer prefab.", errors);
            }
        }

        Require(managerObject.GetComponent<GlimpTagRelaySession>() != null,
            "Relay room-code session is missing from the network manager object.", errors);

        Require(managerObject.GetComponent<GlimpTagLobbyUI>() != null,
            "Lobby UI is missing from the network manager object.", errors);
        Require(managerObject.GetComponent<GlimpTagNetworkRoundHud>() != null,
            "Infection round HUD is missing from the network manager object.", errors);

        GameObject lobbyCameraObject = GameObject.Find("GLIMP TAG Lobby Camera");
        Require(lobbyCameraObject != null, "Main scene is missing the temporary XR lobby camera.", errors);
        if (lobbyCameraObject != null)
        {
            Require(lobbyCameraObject.GetComponent<Camera>() != null, "Lobby camera component is missing.", errors);
            Require(lobbyCameraObject.GetComponent<AudioListener>() != null, "Lobby camera AudioListener is missing.", errors);
            Require(lobbyCameraObject.GetComponent<GlimpTagXRCameraDriver>() != null,
                "Lobby camera XR driver is missing.", errors);
            Require(lobbyCameraObject.GetComponent<GlimpTagLobbyCamera>() != null,
                "Lobby camera handoff component is missing.", errors);
        }
    }

    private static void ValidateOpenXR(List<string> errors, List<string> warnings)
    {
        XRGeneralSettingsPerBuildTarget perTarget;
        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perTarget) ||
            perTarget == null || !perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
        {
            errors.Add("Android XR Plug-in Management settings were not found. OpenXR cannot be initialized for Quest.");
            return;
        }

        XRManagerSettings managerSettings = perTarget.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        bool hasOpenXRLoader = false;
        if (managerSettings != null && managerSettings.activeLoaders != null)
        {
            foreach (XRLoader loader in managerSettings.activeLoaders)
            {
                if (loader is OpenXRLoader)
                {
                    hasOpenXRLoader = true;
                    break;
                }
            }
        }

        if (!hasOpenXRLoader)
            errors.Add("OpenXR loader is not assigned for Android. The Quest APK would not start in XR.");

        OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (settings == null)
        {
            errors.Add("OpenXR Android settings asset was not found.");
            return;
        }

        bool metaQuestFeatureEnabled = false;
        OpenXRFeature[] features = settings.GetFeatures<OpenXRFeature>();
        foreach (OpenXRFeature feature in features)
        {
            if (feature != null && feature.GetType().Name == "MetaQuestFeature" && feature.enabled)
            {
                metaQuestFeatureEnabled = true;
                break;
            }
        }

        if (!metaQuestFeatureEnabled)
            warnings.Add("Meta Quest Support feature was not detected as enabled. Verify the Android OpenXR feature group.");
    }

    private static void ValidateSerializedReferences(
        Object target, List<string> errors, params string[] propertyNames)
    {
        SerializedObject serialized = new SerializedObject(target);
        foreach (string propertyName in propertyNames)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            Require(property != null && property.objectReferenceValue != null,
                target.GetType().Name + " has an unassigned " + propertyName + " reference.", errors);
        }
    }

    private static void Require(bool condition, string message, List<string> errors)
    {
        if (!condition)
            errors.Add(message);
    }
}
