using System.IO;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds a complete network player prefab and connects it to the generated
/// GLIMP TAG scene. Runs from GlimpTagProjectBuilder in Unity Cloud Build.
/// </summary>
public static class GlimpTagNetworkPrefabBuilder
{
    private const string PrefabFolder = "Assets/Resources/GLIMP_TAG";
    private const string PrefabPath = PrefabFolder + "/NetworkPlayer.prefab";
    private const string MaterialFolder = "Assets/GLIMP_TAG/Materials";

    public static void EnsureNetworkPlayerPrefabAndScene(string scenePath)
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder(PrefabFolder);
        EnsureFolder("Assets/GLIMP_TAG");
        EnsureFolder(MaterialFolder);

        GameObject prefab = BuildNetworkPlayerPrefab();
        ConfigureNetworkScene(scenePath, prefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("GLIMP TAG/Rebuild Network Player Prefab")]
    public static void RebuildFromMenu()
    {
        const string scenePath = "Assets/Scenes/GLIMP_TAG.unity";
        EnsureNetworkPlayerPrefabAndScene(scenePath);
        Debug.Log("GLIMP TAG: Network player prefab and scene setup rebuilt.");
    }

    private static GameObject BuildNetworkPlayerPrefab()
    {
        Material bodyMaterial = EnsureMaterial("PlayerBody.mat", new Color(0.10f, 0.72f, 0.88f));
        Material headMaterial = EnsureMaterial("PlayerHead.mat", new Color(0.18f, 0.82f, 0.94f));
        Material leftMaterial = EnsureMaterial("PlayerLeftHand.mat", new Color(0.20f, 0.88f, 1f));
        Material rightMaterial = EnsureMaterial("PlayerRightHand.mat", new Color(1f, 0.48f, 0.18f));

        GameObject root = new GameObject("GLIMP TAG Network Player");
        root.transform.position = Vector3.zero;

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.mass = 65f;
        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.constraints = RigidbodyConstraints.FreezeRotation;

        CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 0.9f, 0f);
        capsule.height = 1.8f;
        capsule.radius = 0.32f;

        // NetworkObject must precede NetworkBehaviours on the root.
        root.AddComponent<NetworkObject>();
        GlimpTagNetworkPlayer networkPlayer = root.AddComponent<GlimpTagNetworkPlayer>();
        root.AddComponent<GlimpTagOwnerNetworkTransform>();
        GlimpTagNetworkAvatarPoseSync poseSync = root.AddComponent<GlimpTagNetworkAvatarPoseSync>();
        GlimpTagNetworkPlayerRig rig = root.AddComponent<GlimpTagNetworkPlayerRig>();

        Transform trackingSpace = CreateChild(root.transform, "Tracking Space");
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(trackingSpace, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 100f;
        camera.fieldOfView = 90f;
        AudioListener audioListener = cameraObject.AddComponent<AudioListener>();

        Transform leftSource = CreateHandSource(trackingSpace, "Left Hand Tracking Proxy",
            new Vector3(-0.25f, 1.1f, 0.35f), leftMaterial,
            out SphereCollider leftTrigger, out GlimpTagNetworkTagger leftTagger,
            out Renderer leftSourceRenderer);

        Transform rightSource = CreateHandSource(trackingSpace, "Right Hand Tracking Proxy",
            new Vector3(0.25f, 1.1f, 0.35f), rightMaterial,
            out SphereCollider rightTrigger, out GlimpTagNetworkTagger rightTagger,
            out Renderer rightSourceRenderer);

        GlimpTagXRHandInput handInput = root.AddComponent<GlimpTagXRHandInput>();
        handInput.Configure(trackingSpace, leftSource, rightSource);

        GlimpTagXRCameraDriver cameraDriver = root.AddComponent<GlimpTagXRCameraDriver>();
        cameraDriver.Configure(trackingSpace, cameraObject.transform);

        GlimpTagLocomotion locomotion = root.AddComponent<GlimpTagLocomotion>();
        locomotion.leftHand = leftSource;
        locomotion.rightHand = rightSource;
        locomotion.solidLayers = ~0;

        GlimpTagStartMenu startMenu = root.AddComponent<GlimpTagStartMenu>();
        startMenu.locomotion = locomotion;
        startMenu.autoStartDelay = 5f;

        Transform remoteVisualRoot = CreateChild(root.transform, "Remote Avatar Visuals");
        GameObject bodyVisual = CreatePrimitive("Remote Body", PrimitiveType.Capsule,
            remoteVisualRoot, new Vector3(0f, 0.9f, 0f),
            new Vector3(0.62f, 0.82f, 0.62f), bodyMaterial, false);
        Renderer bodyRenderer = bodyVisual.GetComponent<Renderer>();

        GameObject headVisual = CreatePrimitive("Remote Head", PrimitiveType.Sphere,
            remoteVisualRoot, new Vector3(0f, 1.6f, 0f),
            Vector3.one * 0.28f, headMaterial, false);

        GameObject leftRemoteHand = CreatePrimitive("Remote Left Hand", PrimitiveType.Sphere,
            remoteVisualRoot, new Vector3(-0.25f, 1.1f, 0.35f),
            Vector3.one * 0.22f, leftMaterial, true);

        GameObject rightRemoteHand = CreatePrimitive("Remote Right Hand", PrimitiveType.Sphere,
            remoteVisualRoot, new Vector3(0.25f, 1.1f, 0.35f),
            Vector3.one * 0.22f, rightMaterial, true);

        poseSync.Configure(cameraObject.transform, leftSource, rightSource,
            headVisual.transform, leftRemoteHand.transform, rightRemoteHand.transform);

        rig.Configure(body, camera, audioListener, trackingSpace,
            leftSource, rightSource, leftTrigger, rightTrigger,
            leftTagger, rightTagger, leftSourceRenderer, rightSourceRenderer,
            handInput, cameraDriver, locomotion, startMenu, remoteVisualRoot);

        SerializedObject playerSerialized = new SerializedObject(networkPlayer);
        SerializedProperty colorRenderers = playerSerialized.FindProperty("colorRenderers");
        if (colorRenderers != null)
        {
            colorRenderers.arraySize = 1;
            colorRenderers.GetArrayElementAtIndex(0).objectReferenceValue = bodyRenderer;
            playerSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Transform CreateChild(Transform parent, string objectName)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static Transform CreateHandSource(
        Transform parent,
        string objectName,
        Vector3 localPosition,
        Material material,
        out SphereCollider trigger,
        out GlimpTagNetworkTagger tagger,
        out Renderer handRenderer)
    {
        GameObject hand = CreatePrimitive(objectName, PrimitiveType.Sphere,
            parent, localPosition, Vector3.one * 0.22f, material, true);

        trigger = hand.GetComponent<SphereCollider>();
        trigger.isTrigger = true;
        tagger = hand.AddComponent<GlimpTagNetworkTagger>();
        handRenderer = hand.GetComponent<Renderer>();
        return hand.transform;
    }

    private static GameObject CreatePrimitive(
        string objectName,
        PrimitiveType type,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material,
        bool keepCollider)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = objectName;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = localPosition;
        item.transform.localRotation = Quaternion.identity;
        item.transform.localScale = localScale;

        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        if (!keepCollider)
        {
            Collider collider = item.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);
        }

        return item;
    }

    private static Material EnsureMaterial(string fileName, Color color)
    {
        string path = MaterialFolder + "/" + fileName;
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        Shader shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");

        material = new Material(shader);
        material.color = color;
        material.enableInstancing = true;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void ConfigureNetworkScene(string scenePath, GameObject playerPrefab)
    {
        if (!File.Exists(scenePath))
        {
            Debug.LogError("GLIMP TAG: Main scene was not found: " + scenePath);
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        GlimpTagRuntimeBootstrap bootstrap = Object.FindObjectOfType<GlimpTagRuntimeBootstrap>();
        if (bootstrap != null)
        {
            bootstrap.createPlayerRig = false;
            EditorUtility.SetDirty(bootstrap);
        }

        GameObject managerObject = GameObject.Find("GLIMP TAG Network Manager");
        if (managerObject == null)
            managerObject = new GameObject("GLIMP TAG Network Manager");

        UnityTransport transport = managerObject.GetComponent<UnityTransport>();
        if (transport == null)
            transport = managerObject.AddComponent<UnityTransport>();

        NetworkManager manager = managerObject.GetComponent<NetworkManager>();
        if (manager == null)
            manager = managerObject.AddComponent<NetworkManager>();

        manager.NetworkConfig.NetworkTransport = transport;
        manager.NetworkConfig.PlayerPrefab = playerPrefab;
        manager.NetworkConfig.TickRate = 30;
        manager.NetworkConfig.EnableSceneManagement = false;

        if (managerObject.GetComponent<GlimpTagLobbyUI>() == null)
            managerObject.AddComponent<GlimpTagLobbyUI>();

        if (managerObject.GetComponent<GlimpTagNetworkRoundHud>() == null)
            managerObject.AddComponent<GlimpTagNetworkRoundHud>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
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
}
