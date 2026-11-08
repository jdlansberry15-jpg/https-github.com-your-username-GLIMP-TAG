# Quest XR hand and controller input

The project now includes GlimpTagXRHandInput.cs. It reads tracked poses through Unity's built-in UnityEngine.XR.InputDevices API, so it does not require a separate hand-input code package.

## Intended rig wiring
1. Use an XR Origin with a working OpenXR runtime.
2. Assign the XR Origin's Camera Offset / tracking-space transform to trackingSpace.
3. Create two empty proxy GameObjects under the tracking-space object: LeftHandProxy and RightHandProxy.
4. Assign those transforms to leftHandTarget and rightHandTarget.
5. Assign the same two proxy transforms to GlimpTagLocomotion.leftHand and rightHand.
6. Keep the proxies separate from the actual XR controller objects. The script turns the proxy GameObjects off when tracking is unavailable.
7. Leave preferHandTracking enabled to use tracked hands when the runtime exposes them; Touch controllers are the fallback.

## Quest / OpenXR project configuration
Meta's current Unity guidance recommends Unity OpenXR for new Quest projects. Install XR Plug-in Management and OpenXR, enable OpenXR for Android / Meta Quest, enable the Meta Quest feature group, and add the Oculus Touch Controller interaction profile. Enable the Hand Interaction Profile as well if hand tracking is required by the project.

The repository's package manifest pins Unity 6-compatible versions for OpenXR, XR Plug-in Management, NGO, and Unity Transport. Package installation alone does not automatically configure all XR Plug-in Management project settings.

## Compatibility notes
- Uses Unity's built-in XR input API (UnityEngine.XR), not the newer Input System.
- Hand tracking depends on the headset runtime, OpenXR feature configuration, and device settings exposing a hand-tracking InputDevice.
- The controller fallback is intended for Quest Touch controllers.
- The script supplies wrist / controller device poses only. Finger curls, pinch gestures, hand meshes, and hand physics are not implemented.
- A tracking-space transform must be assigned for correct world-space placement when the XR Origin is moved or rotated.
- This source has not been compiled in Unity or tested on a Quest headset in this environment.
