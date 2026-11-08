using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Creates the original GLIMP TAG forest test arena and a lightweight Quest XR player rig.
/// The scene only needs this component on one active GameObject.
/// </summary>
public sealed class GlimpTagRuntimeBootstrap : MonoBehaviour
{
    [Header("Scene generation")]
    public bool buildArenaOnStart = true;
    public bool createPlayerRig = true;
    public int arenaSeed = 31;

    public Transform[] SpawnPoints { get; private set; }

    private Transform worldRoot;
    private Material grass;
    private Material bark;
    private Material leaves;
    private Material wood;
    private Material rock;
    private Material cave;
    private Material playerMaterial;
    private Material leftHandMaterial;
    private Material rightHandMaterial;

    private void Awake()
    {
        Application.targetFrameRate = 72;
        QualitySettings.vSyncCount = 0;
        QualitySettings.antiAliasing = 2;
        RenderSettings.ambientLight = new Color(0.52f, 0.60f, 0.48f);

        CreateMaterials();
        CreateLighting();

        if (buildArenaOnStart)
            BuildForestArena();

        if (createPlayerRig)
            CreateQuestPlayerRig();
    }

    private void CreateMaterials()
    {
        grass = MakeMaterial("Forest Grass", new Color(0.20f, 0.34f, 0.16f));
        bark = MakeMaterial("Tree Bark", new Color(0.30f, 0.18f, 0.09f));
        leaves = MakeMaterial("Forest Canopy", new Color(0.12f, 0.30f, 0.13f));
        wood = MakeMaterial("Warm Wood", new Color(0.46f, 0.28f, 0.13f));
        rock = MakeMaterial("Boundary Stone", new Color(0.31f, 0.34f, 0.32f));
        cave = MakeMaterial("Cave Shadow", new Color(0.055f, 0.065f, 0.06f));
        playerMaterial = MakeMaterial("Player Body", new Color(0.12f, 0.75f, 0.90f));
        leftHandMaterial = MakeMaterial("Left Hand", new Color(0.20f, 0.85f, 1f));
        rightHandMaterial = MakeMaterial("Right Hand", new Color(1f, 0.55f, 0.22f));
    }

    private static Material MakeMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);
        material.name = materialName;
        material.color = color;
        material.enableInstancing = true;
        return material;
    }

    private void CreateLighting()
    {
        GameObject lightObject = new GameObject("GLIMP TAG Sun");
        Light sun = lightObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 0.9f;
        sun.shadows = LightShadows.None;
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
    }

    private void BuildForestArena()
    {
        worldRoot = new GameObject("GLIMP TAG Original Forest Arena").transform;
        worldRoot.SetParent(transform, false);

        // A broad, flat floor keeps the first prototype easy to navigate.
        MakePrimitive("Forest Floor", PrimitiveType.Cylinder,
            new Vector3(0f, -0.35f, 0f), new Vector3(54f, 0.35f, 54f),
            grass, true);

        BuildCentralTreehouse();
        BuildCircularCanopyRoute();
        BuildTrees();
        BuildStumpGazebo();
        BuildCaveTunnel();
        BuildRockPerimeter();
        BuildSpawnPoints();
    }

    private void BuildCentralTreehouse()
    {
        MakePrimitive("Treehouse Main Trunk", PrimitiveType.Cylinder,
            new Vector3(0f, 4.8f, 0f), new Vector3(1.5f, 4.8f, 1.5f), bark, true);

        MakePrimitive("Treehouse Platform", PrimitiveType.Cube,
            new Vector3(0f, 5.1f, 0f), new Vector3(7.5f, 0.45f, 5.8f), wood, true);

        MakePrimitive("Treehouse Roof", PrimitiveType.Cube,
            new Vector3(0f, 8.4f, 0f), new Vector3(5.8f, 0.35f, 4.6f), bark, true);

        CreateBeam("Treehouse Branch North", new Vector3(0f, 3.4f, 0f),
            new Vector3(0f, 5.2f, 4.5f), 0.42f, bark);
        CreateBeam("Treehouse Branch East", new Vector3(0f, 3.7f, 0f),
            new Vector3(5f, 5.3f, 0f), 0.42f, bark);
        CreateBeam("Treehouse Branch West", new Vector3(0f, 3.7f, 0f),
            new Vector3(-5f, 5.3f, 0f), 0.42f, bark);
        CreateBeam("Treehouse Branch South", new Vector3(0f, 3.5f, 0f),
            new Vector3(0f, 5.0f, -4.5f), 0.42f, bark);

        MakePrimitive("Treehouse Lookout", PrimitiveType.Cube,
            new Vector3(0f, 6.2f, -1.7f), new Vector3(3.4f, 0.35f, 2.4f), wood, true);
    }

    private void BuildCircularCanopyRoute()
    {
        const int plankCount = 24;
        const float routeRadius = 13.5f;

        for (int i = 0; i < plankCount; i++)
        {
            float angle = i * Mathf.PI * 2f / plankCount;
            Vector3 position = new Vector3(
                Mathf.Cos(angle) * routeRadius,
                2.25f,
                Mathf.Sin(angle) * routeRadius);

            Quaternion rotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg + 90f, 0f);
            GameObject plank = MakePrimitive("Circular Forest Bridge Plank",
                PrimitiveType.Cube, position, new Vector3(3.2f, 0.35f, 1.8f),
                wood, true);
            plank.transform.rotation = rotation;
        }
    }

    private void BuildTrees()
    {
        System.Random random = new System.Random(arenaSeed);
        const int treeCount = 12;

        for (int i = 0; i < treeCount; i++)
        {
            float angle = (i / (float)treeCount) * Mathf.PI * 2f + 0.22f;
            float radius = 9.5f + (i % 3) * 3.3f;
            float height = 6.2f + (float)random.NextDouble() * 2.0f;
            Vector3 basePosition = new Vector3(
                Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            MakePrimitive("Climbable Forest Tree", PrimitiveType.Cylinder,
                basePosition + Vector3.up * (height * 0.5f),
                new Vector3(0.85f, height * 0.5f, 0.85f), bark, true);

            MakePrimitive("Tree Canopy", PrimitiveType.Sphere,
                basePosition + Vector3.up * (height + 1.3f),
                new Vector3(3.6f, 2.7f, 3.6f), leaves, false);

            float branchAngle = angle + 1.15f;
            Vector3 branchEnd = basePosition + new Vector3(
                Mathf.Cos(branchAngle) * 3.4f, height * 0.78f,
                Mathf.Sin(branchAngle) * 3.4f);
            CreateBeam("Climbable Tree Branch",
                basePosition + Vector3.up * (height * 0.58f),
                branchEnd, 0.32f, bark);

            Vector3 smallPlatform = basePosition + new Vector3(
                Mathf.Cos(angle + 2.1f) * 2.5f, height * 0.72f,
                Mathf.Sin(angle + 2.1f) * 2.5f);
            MakePrimitive("Tree Branch Rest Platform", PrimitiveType.Cube,
                smallPlatform, new Vector3(2.5f, 0.32f, 1.8f), wood, true);
        }
    }

    private void BuildStumpGazebo()
    {
        Vector3 center = new Vector3(-10f, 0f, 8f);
        MakePrimitive("Hangout Stump", PrimitiveType.Cylinder,
            center + Vector3.up * 1.0f, new Vector3(2.4f, 1.0f, 2.4f), bark, true);

        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                MakePrimitive("Gazebo Support", PrimitiveType.Cylinder,
                    center + new Vector3(x * 2.5f, 2.0f, z * 2.5f),
                    new Vector3(0.18f, 2.0f, 0.18f), bark, true);
            }
        }

        MakePrimitive("Gazebo Roof", PrimitiveType.Cube,
            center + Vector3.up * 4.1f, new Vector3(6.2f, 0.35f, 6.2f), wood, true);

        MakePrimitive("Hangout Bench A", PrimitiveType.Cube,
            center + new Vector3(0f, 0.75f, 3.4f), new Vector3(3.2f, 0.35f, 0.75f), wood, true);
        MakePrimitive("Hangout Bench B", PrimitiveType.Cube,
            center + new Vector3(3.4f, 0.75f, 0f), new Vector3(0.75f, 0.35f, 3.2f), wood, true);
    }

    private void BuildCaveTunnel()
    {
        float z = 22f;

        MakePrimitive("Cave Entrance Left", PrimitiveType.Cube,
            new Vector3(-2.3f, 1.7f, z), new Vector3(0.8f, 3.4f, 1.2f), rock, true);
        MakePrimitive("Cave Entrance Right", PrimitiveType.Cube,
            new Vector3(2.3f, 1.7f, z), new Vector3(0.8f, 3.4f, 1.2f), rock, true);
        MakePrimitive("Cave Entrance Lintel", PrimitiveType.Cube,
            new Vector3(0f, 3.5f, z), new Vector3(5.4f, 0.8f, 1.2f), rock, true);

        for (int section = 0; section < 3; section++)
        {
            float sectionZ = z + 2.0f + section * 2.0f;
            MakePrimitive("Cave Tunnel Left Wall", PrimitiveType.Cube,
                new Vector3(-2.2f, 1.7f, sectionZ), new Vector3(0.5f, 3.4f, 2.4f), rock, true);
            MakePrimitive("Cave Tunnel Right Wall", PrimitiveType.Cube,
                new Vector3(2.2f, 1.7f, sectionZ), new Vector3(0.5f, 3.4f, 2.4f), rock, true);
            MakePrimitive("Cave Tunnel Ceiling", PrimitiveType.Cube,
                new Vector3(0f, 3.5f, sectionZ), new Vector3(4.8f, 0.5f, 2.4f), cave, true);
        }
    }

    private void BuildRockPerimeter()
    {
        const int rockCount = 28;
        const float radius = 26f;

        for (int i = 0; i < rockCount; i++)
        {
            float angle = i * Mathf.PI * 2f / rockCount;
            float degrees = angle * Mathf.Rad2Deg;

            // Leave a gap around the cave entrance.
            if (degrees > 68f && degrees < 112f)
                continue;

            Vector3 position = new Vector3(
                Mathf.Cos(angle) * radius,
                1.2f,
                Mathf.Sin(angle) * radius);

            GameObject stone = MakePrimitive("Perimeter Boundary Rock",
                PrimitiveType.Sphere, position,
                new Vector3(3.2f, 2.5f, 3.2f), rock, true);
            stone.transform.rotation = Quaternion.Euler(i * 13f, i * 19f, i * 7f);
        }
    }

    private void BuildSpawnPoints()
    {
        Vector3[] positions =
        {
            new Vector3(0f, 0.15f, -8f),
            new Vector3(7f, 0.15f, -5f),
            new Vector3(-7f, 0.15f, -5f),
            new Vector3(7f, 0.15f, 6f),
            new Vector3(-7f, 0.15f, 6f),
            new Vector3(0f, 0.15f, 10f)
        };

        SpawnPoints = new Transform[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            GameObject spawn = new GameObject("Spawn Point " + (i + 1));
            spawn.transform.SetParent(worldRoot, false);
            spawn.transform.position = positions[i];
            SpawnPoints[i] = spawn.transform;
        }
    }

    private void CreateQuestPlayerRig()
    {
        GameObject player = new GameObject("GLIMP Player Rig");
        player.layer = 2;
        player.transform.position = SpawnPoints != null && SpawnPoints.Length > 0
            ? SpawnPoints[0].position
            : new Vector3(0f, 0.15f, -8f);

        Rigidbody body = player.AddComponent<Rigidbody>();
        body.mass = 65f;
        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.constraints = RigidbodyConstraints.FreezeRotation;

        CapsuleCollider capsule = player.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 0.9f, 0f);
        capsule.height = 1.8f;
        capsule.radius = 0.32f;

        GameObject bodyVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        bodyVisual.name = "Local Player Body Visual";
        bodyVisual.layer = 2;
        bodyVisual.transform.SetParent(player.transform, false);
        bodyVisual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        bodyVisual.transform.localScale = new Vector3(0.62f, 0.82f, 0.62f);
        bodyVisual.GetComponent<Renderer>().sharedMaterial = playerMaterial;
        Destroy(bodyVisual.GetComponent<Collider>());

        Transform trackingSpace = new GameObject("Tracking Space").transform;
        trackingSpace.SetParent(player.transform, false);
        trackingSpace.localPosition = Vector3.zero;
        trackingSpace.localRotation = Quaternion.identity;
        trackingSpace.gameObject.layer = 2;

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.layer = 2;
        cameraObject.transform.SetParent(trackingSpace, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        cameraObject.transform.localRotation = Quaternion.identity;

        Camera cameraComponent = cameraObject.AddComponent<Camera>();
        cameraComponent.nearClipPlane = 0.05f;
        cameraComponent.farClipPlane = 100f;
        cameraComponent.fieldOfView = 90f;
        cameraObject.AddComponent<AudioListener>();

        GlimpTagXRCameraDriver cameraDriver = player.AddComponent<GlimpTagXRCameraDriver>();
        cameraDriver.Configure(trackingSpace, cameraObject.transform);

        Transform leftProxy = CreateHandProxy(trackingSpace, "Left Hand Proxy",
            new Vector3(-0.25f, 1.1f, 0.35f), leftHandMaterial);
        Transform rightProxy = CreateHandProxy(trackingSpace, "Right Hand Proxy",
            new Vector3(0.25f, 1.1f, 0.35f), rightHandMaterial);

        GlimpTagXRHandInput handInput = player.AddComponent<GlimpTagXRHandInput>();
        handInput.Configure(trackingSpace, leftProxy, rightProxy);

        GlimpTagLocomotion locomotion = player.AddComponent<GlimpTagLocomotion>();
        locomotion.leftHand = leftProxy;
        locomotion.rightHand = rightProxy;
        locomotion.solidLayers = ~(1 << 2);
        locomotion.enabled = false;

        GlimpTagStartMenu startMenu = player.AddComponent<GlimpTagStartMenu>();
        startMenu.locomotion = locomotion;
        startMenu.autoStartDelay = 8f;
    }

    private Transform CreateHandProxy(Transform trackingSpace, string objectName,
        Vector3 localPosition, Material handMaterial)
    {
        GameObject proxy = new GameObject(objectName);
        proxy.layer = 2;
        proxy.transform.SetParent(trackingSpace, false);
        proxy.transform.localPosition = localPosition;

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Hand Contact Visual";
        visual.layer = 2;
        visual.transform.SetParent(proxy.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * 0.22f;
        visual.GetComponent<Renderer>().sharedMaterial = handMaterial;
        Destroy(visual.GetComponent<Collider>());

        return proxy.transform;
    }

    private GameObject MakePrimitive(string objectName, PrimitiveType type,
        Vector3 position, Vector3 scale, Material material, bool keepCollider)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = objectName;
        item.transform.SetParent(worldRoot, false);
        item.transform.position = position;
        item.transform.localScale = scale;

        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        if (!keepCollider)
        {
            Collider itemCollider = item.GetComponent<Collider>();
            if (itemCollider != null)
                Destroy(itemCollider);
        }

        return item;
    }

    private void CreateBeam(string objectName, Vector3 start, Vector3 end,
        float thickness, Material material)
    {
        Vector3 direction = end - start;
        GameObject beam = MakePrimitive(objectName, PrimitiveType.Cube,
            (start + end) * 0.5f,
            new Vector3(thickness, thickness, direction.magnitude), material, true);
        beam.transform.rotation = Quaternion.LookRotation(direction.normalized);
    }
}
