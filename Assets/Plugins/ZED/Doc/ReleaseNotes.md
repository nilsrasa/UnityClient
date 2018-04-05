<!------------------------- Release notes ------------------------------------->

### 2.3.1

- **Minor Bug fixes and Features** :
   - Fix GreenScreen broken prefab in Canvas.
   - Fix default spatial memory path when enableTracking. Could throw an exception when tracking was activated in green screen prefab.
   - Fix missing but unused script in Planetarium prefab
   - Added Unity Plugin version in ZEDCamera.cs 

### 2.3.0

- **Features**:
   - Added support for ZED mini.
   - Added beta stereo passthrough feature with optimized rendering in VR headsets (only with ZED mini)

- **Prefabs**:
   - Added ZED_Rig_Stereo prefab, with stereo capture and stereo rendering to VR headsets (beta version)
   - Renamed ZED_Rig_Left in ZED_Rig_Mono, for better mono/stereo distinction.

- **Examples**:
   - Added Planetarium scene to demonstrate how to re-create the solar system in the real world. This is a simplified version of the ZEDWorld app.
   - Added Dark Room scene to demonstrate how to decrease the brightness of the real world using the "Real Light Brightness" setting in ZEDManager.cs.
   - Added Object Placement scene to demonstrate how to place an object on a horizontal plane in the real world.

- **Scripts**:
   - Added ZEDSupportFunctions.cs to help using depth and normals at a screen or world position. Some of these functions are used in ObjectPlacement scene.
   - Added ZEDMixedRealityPlugin.cs to handle stereo passthrough in Oculus Rift or HTC Vive Headset.

- **Renaming**:
  -  ZEDTextureOverlay.cs has been renamed ZEDRenderingPlane.cs.

- **Compatibility**:
  - Supports ZED SDK v2.3.0
  - Supports Unity 2017.x.y (with automatic updates from Unity).

- **Known Issues**:
  - On certain configurations, VRCompositor in SteamVR can freeze when using HTC Vive and ZED. Disabling Async Reprojection in SteamVR's settings can fix the issue.
  - The stereo passthrough experience is highly sensitive to Capture/Engine FPS. Here are some tips:
            * Make sure your PC meets the requirements for stereo pass-through AR (GTx 1070 or better).
            * Make sure you don't have other resource-intensive applications running on your PC at the same time as Unity.
            * Test your application in Build mode, rather than the Unity editor, to have the best FPS available.


### 2.2.0/2.1.0/2.0.0

See ZED SDK release notes.

