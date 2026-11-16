using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

/// <summary>
/// Lightweight owner-only wrist menu for Quest controllers and hand tracking.
/// Left X / left-hand pinch opens or closes the menu. Right trigger / pinch
/// selects a button by pointing the right hand at it.
/// </summary>
[DisallowMultipleComponent]
public sealed class GlimpTagWristMenu : NetworkBehaviour
{
    private const float MenuRayDistance = 2.0f;
    private const float PinchThreshold = 0.025f;
    private const int MenuLayer = 2; // Ignore Raycast layer; menu colliders are triggers.

    private readonly Dictionary<Collider, Action> buttonActions = new Dictionary<Collider, Action>();
    private readonly RaycastHit[] raycastHits = new RaycastHit[16];
    private readonly List<XRHandSubsystem> handSubsystems = new List<XRHandSubsystem>(2);

    private Transform trackingSpace;
    private Transform leftHandSource;
    private Transform rightHandSource;
    private Transform menuRoot;
    private LineRenderer pointerLine;
    private TextMesh statusText;
    private TextMesh roomCodeText;
    private TextMesh microphoneButtonText;
    private TextMesh roomButtonText;

    private GlimpTagRelaySession relaySession;
    private GlimpTagVoiceChat voiceChat;
    private XRHandSubsystem handSubsystem;
    private InputDevice leftController;
    private InputDevice rightController;

    private Action hoveredAction;
    private bool menuOpen;
    private bool leftToggleWasPressed;
    private bool rightSelectWasPressed;
    private float nextDeviceScan;
    private float nextHandSubsystemScan;
    private float nextStatusRefresh;

    public void Configure(Transform playerTrackingSpace, Transform leftHand, Transform rightHand)
    {
        trackingSpace = playerTrackingSpace;
        leftHandSource = leftHand;
        rightHandSource = rightHand;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        RefreshReferences();
        RefreshInputDevices();
        RefreshHandSubsystem();
        BuildMenu();
    }

    public override void OnNetworkDespawn()
    {
        if (menuRoot != null)
            Destroy(menuRoot.gameObject);
        if (pointerLine != null)
            Destroy(pointerLine.gameObject);
    }

    private void Update()
    {
        if (!IsOwner || menuRoot == null)
            return;

        if (Time.unscaledTime >= nextDeviceScan)
            RefreshInputDevices();
        if (Time.unscaledTime >= nextHandSubsystemScan)
            RefreshHandSubsystem();
        if (Time.unscaledTime >= nextStatusRefresh)
        {
            RefreshMenuText();
            nextStatusRefresh = Time.unscaledTime + 0.25f;
        }

        bool leftTogglePressed = ReadButton(leftController, CommonUsages.primaryButton) ||
            IsPinching(Handedness.Left);
        if (leftTogglePressed && !leftToggleWasPressed)
            SetMenuOpen(!menuOpen);
        leftToggleWasPressed = leftTogglePressed;

        if (!menuOpen)
            return;

        bool rightSelectPressed = ReadButton(rightController, CommonUsages.triggerButton) ||
            IsPinching(Handedness.Right);
        UpdatePointer();

        if (rightSelectPressed && !rightSelectWasPressed && hoveredAction != null)
            hoveredAction.Invoke();

        rightSelectWasPressed = rightSelectPressed;
    }

    private void LateUpdate()
    {
        if (!menuOpen || menuRoot == null)
            return;

        Camera viewCamera = Camera.main;
        if (viewCamera == null)
            return;

        Vector3 towardViewer = viewCamera.transform.position - menuRoot.position;
        if (towardViewer.sqrMagnitude > 0.0001f)
            menuRoot.rotation = Quaternion.LookRotation(towardViewer.normalized, viewCamera.transform.up);
    }

    private void RefreshReferences()
    {
        if (relaySession == null)
            relaySession = FindObjectOfType<GlimpTagRelaySession>();
        if (voiceChat == null)
            voiceChat = FindObjectOfType<GlimpTagVoiceChat>();
    }

    private void RefreshInputDevices()
    {
        leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        nextDeviceScan = Time.unscaledTime + 0.5f;
    }

    private void RefreshHandSubsystem()
    {
        handSubsystems.Clear();
        SubsystemManager.GetSubsystems(handSubsystems);
        handSubsystem = null;

        for (int i = 0; i < handSubsystems.Count; i++)
        {
            if (handSubsystems[i] != null && handSubsystems[i].running)
            {
                handSubsystem = handSubsystems[i];
                break;
            }
        }

        nextHandSubsystemScan = Time.unscaledTime + 1f;
    }

    private static bool ReadButton(InputDevice device, InputFeatureUsage<bool> usage)
    {
        return device.isValid && device.TryGetFeatureValue(usage, out bool pressed) && pressed;
    }

    private bool IsPinching(Handedness handedness)
    {
        if (handSubsystem == null || !handSubsystem.running)
            return false;

        XRHand hand = handedness == Handedness.Left
            ? handSubsystem.leftHand
            : handSubsystem.rightHand;
        if (!hand.isTracked)
            return false;

        XRHandJoint indexTip = hand.GetJoint(XRHandJointID.IndexTip);
        XRHandJoint thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
        if (!indexTip.TryGetPose(out Pose indexPose) || !thumbTip.TryGetPose(out Pose thumbPose))
            return false;

        Vector3 indexPosition = trackingSpace != null
            ? trackingSpace.TransformPoint(indexPose.position)
            : indexPose.position;
        Vector3 thumbPosition = trackingSpace != null
            ? trackingSpace.TransformPoint(thumbPose.position)
            : thumbPose.position;

        return Vector3.Distance(indexPosition, thumbPosition) <= PinchThreshold;
    }

    private void BuildMenu()
    {
        if (leftHandSource == null)
        {
            Debug.LogWarning("GLIMP TAG wrist menu: left hand tracking proxy was not assigned.", this);
            return;
        }

        menuRoot = new GameObject("GLIMP TAG Wrist Menu").transform;
        menuRoot.SetParent(leftHandSource, false);
        menuRoot.localPosition = new Vector3(0f, 0.10f, 0.07f);
        menuRoot.localRotation = Quaternion.identity;

        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Cube);
        background.name = "Wrist Menu Background";
        background.transform.SetParent(menuRoot, false);
        background.transform.localScale = new Vector3(0.46f, 0.70f, 0.025f);
        Collider backgroundCollider = background.GetComponent<Collider>();
        if (backgroundCollider != null)
            Destroy(backgroundCollider);
        SetRendererMaterial(background.GetComponent<Renderer>(), new Color(0.035f, 0.075f, 0.065f));

        CreateText(menuRoot, "GLIMP TAG", new Vector3(0f, 0.275f, 0.018f),
            48, 0.010f, Color.white, out _);
        statusText = CreateText(menuRoot, "NOT IN ROOM", new Vector3(0f, 0.16f, 0.019f),
            34, 0.008f, new Color(0.72f, 0.95f, 0.82f), out _);
        roomCodeText = CreateText(menuRoot, "", new Vector3(0f, 0.055f, 0.019f),
            32, 0.008f, new Color(1f, 0.82f, 0.36f), out _);

        CreateButton("Microphone", "MIC OFFLINE", -0.075f,
            new Color(0.10f, 0.36f, 0.42f), ToggleMicrophone, out microphoneButtonText);
        CreateButton("Room", "CREATE ROOM", -0.19f,
            new Color(0.28f, 0.34f, 0.18f), ToggleRoom, out roomButtonText);
        CreateButton("Close", "CLOSE MENU", -0.305f,
            new Color(0.30f, 0.20f, 0.16f), () => SetMenuOpen(false), out _);

        SetLayerRecursively(menuRoot.gameObject, MenuLayer);
        BuildPointer();
        SetMenuOpen(false);
        RefreshMenuText();
    }

    private void CreateButton(string buttonName, string label, float y,
        Color color, Action action, out TextMesh labelText)
    {
        GameObject button = new GameObject("Wrist Button " + buttonName);
        button.transform.SetParent(menuRoot, false);
        button.transform.localPosition = new Vector3(0f, y, 0.025f);

        BoxCollider buttonCollider = button.AddComponent<BoxCollider>();
        buttonCollider.size = new Vector3(0.38f, 0.078f, 0.035f);
        buttonCollider.isTrigger = true;
        buttonActions.Add(buttonCollider, action);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Button Surface";
        visual.transform.SetParent(button.transform, false);
        visual.transform.localScale = new Vector3(0.38f, 0.078f, 0.018f);
        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
            Destroy(visualCollider);
        SetRendererMaterial(visual.GetComponent<Renderer>(), color);

        labelText = CreateText(button.transform, label,
            new Vector3(0f, 0f, 0.012f), 36, 0.008f, Color.white, out _);
    }

    private static TextMesh CreateText(Transform parent, string value, Vector3 localPosition,
        int fontSize, float characterSize, Color color, out GameObject textObject)
    {
        textObject = new GameObject("Menu Text");
        textObject.transform.SetParent(parent, false);
        textObject.transform.localPosition = localPosition;

        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = value;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.color = color;
        textMesh.richText = false;
        return textMesh;
    }

    private void BuildPointer()
    {
        GameObject pointerObject = new GameObject("GLIMP TAG Wrist Menu Pointer");
        pointerObject.transform.SetParent(transform, false);
        pointerObject.layer = MenuLayer;

        pointerLine = pointerObject.AddComponent<LineRenderer>();
        pointerLine.useWorldSpace = true;
        pointerLine.positionCount = 2;
        pointerLine.startWidth = 0.004f;
        pointerLine.endWidth = 0.0015f;
        pointerLine.startColor = new Color(0.25f, 0.95f, 1f, 0.95f);
        pointerLine.endColor = new Color(0.25f, 0.95f, 1f, 0.2f);
        pointerLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pointerLine.receiveShadows = false;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader != null)
            pointerLine.material = new Material(shader);

        pointerLine.enabled = false;
    }

    private void UpdatePointer()
    {
        hoveredAction = null;
        if (pointerLine == null || rightHandSource == null || !rightHandSource.gameObject.activeInHierarchy)
        {
            if (pointerLine != null)
                pointerLine.enabled = false;
            return;
        }

        Ray ray = new Ray(rightHandSource.position + rightHandSource.forward * 0.08f,
            rightHandSource.forward);
        Physics.SyncTransforms();
        int hitCount = Physics.RaycastNonAlloc(ray, raycastHits, MenuRayDistance,
            1 << MenuLayer, QueryTriggerInteraction.Collide);

        float nearestDistance = float.MaxValue;
        Vector3 pointerEnd = ray.GetPoint(MenuRayDistance);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = raycastHits[i];
            if (hit.collider == null || !buttonActions.TryGetValue(hit.collider, out Action action))
                continue;

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                pointerEnd = hit.point;
                hoveredAction = action;
            }
        }

        pointerLine.enabled = true;
        pointerLine.SetPosition(0, ray.origin);
        pointerLine.SetPosition(1, pointerEnd);
    }

    private void ToggleMicrophone()
    {
        RefreshReferences();
        if (voiceChat == null || !voiceChat.IsConnected)
        {
            if (statusText != null)
                statusText.text = "VOICE NOT CONNECTED";
            return;
        }

        voiceChat.ToggleMicrophone();
        RefreshMenuText();
    }

    private void ToggleRoom()
    {
        RefreshReferences();
        NetworkManager manager = NetworkManager.Singleton;
        if (relaySession == null)
        {
            if (statusText != null)
                statusText.text = "LOBBY SERVICE MISSING";
            return;
        }

        if (manager != null && manager.IsListening)
            relaySession.LeaveRoom();
        else
            _ = relaySession.HostRoomAsync();

        RefreshMenuText();
    }

    private void RefreshMenuText()
    {
        if (statusText == null)
            return;

        RefreshReferences();
        NetworkManager manager = NetworkManager.Singleton;
        bool inRoom = manager != null && manager.IsListening &&
            (manager.IsHost || manager.IsConnectedClient);

        if (manager == null || !manager.IsListening)
        {
            statusText.text = "NOT IN ROOM\nOPEN LOBBY OR CREATE";
            roomCodeText.text = "";
            roomButtonText.text = "CREATE ROOM";
        }
        else if (!inRoom)
        {
            statusText.text = "CONNECTING TO ROOM...";
            roomCodeText.text = "";
            roomButtonText.text = "CANCEL CONNECTION";
        }
        else
        {
            GlimpTagNetworkPlayer localPlayer = null;
            if (manager.SpawnManager != null)
            {
                NetworkObject localObject = manager.SpawnManager.GetLocalPlayerObject();
                if (localObject != null)
                    localPlayer = localObject.GetComponent<GlimpTagNetworkPlayer>();
            }

            string safety = localPlayer != null && localPlayer.IsTagged.Value
                ? "INFECTED"
                : "SAFE";
            int roundNumber = 0;
            int players = manager.ConnectedClientsIds.Count;
            GlimpTagNetworkPlayer coordinator = FindRoundCoordinator(manager);
            if (coordinator != null)
            {
                roundNumber = coordinator.RoundNumber.Value;
                players = coordinator.PlayerCount.Value;
            }

            statusText.text = "ROUND " + roundNumber + "  |  " + players + " PLAYERS\nYOU ARE " + safety;
            roomButtonText.text = "LEAVE ROOM";
            roomCodeText.text = manager.IsHost && relaySession != null &&
                !string.IsNullOrEmpty(relaySession.LastJoinCode)
                ? "ROOM CODE: " + relaySession.LastJoinCode
                : (manager.IsHost ? "YOU ARE HOST" : "CONNECTED TO HOST");
        }

        if (microphoneButtonText != null)
        {
            if (voiceChat == null || !voiceChat.IsConnected)
                microphoneButtonText.text = "MIC: NOT CONNECTED";
            else
                microphoneButtonText.text = voiceChat.IsMuted ? "UNMUTE MIC" : "MUTE MIC";
        }
    }

    private static GlimpTagNetworkPlayer FindRoundCoordinator(NetworkManager manager)
    {
        if (manager == null || manager.SpawnManager == null)
            return null;

        foreach (NetworkObject networkObject in manager.SpawnManager.SpawnedObjectsList)
        {
            if (networkObject == null || networkObject.OwnerClientId != NetworkManager.ServerClientId)
                continue;

            GlimpTagNetworkPlayer player = networkObject.GetComponent<GlimpTagNetworkPlayer>();
            if (player != null)
                return player;
        }

        return null;
    }

    private void SetMenuOpen(bool open)
    {
        menuOpen = open;
        if (menuRoot != null)
            menuRoot.gameObject.SetActive(open);
        if (pointerLine != null)
            pointerLine.enabled = open;
        if (!open)
            hoveredAction = null;
    }

    private static void SetRendererMaterial(Renderer renderer, Color color)
    {
        if (renderer == null)
            return;

        Shader shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        if (shader == null)
            return;

        Material material = new Material(shader);
        material.color = color;
        material.enableInstancing = true;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
