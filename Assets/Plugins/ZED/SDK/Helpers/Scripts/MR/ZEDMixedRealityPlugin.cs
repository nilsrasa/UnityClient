//======= Copyright (c) Stereolabs Corporation, All rights reserved. ===============

using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.VR;

/// <summary>
/// Manages the two finals cameras
/// </summary>
public class ZEDMixedRealityPlugin : MonoBehaviour
{
	const string nameDll = "sl_unitywrapper";
	[DllImport(nameDll, EntryPoint = "dllz_compute_size_plane_with_gamma")]
	private static extern System.IntPtr dllz_compute_size_plane_with_gamma(sl.Resolution resolution, float perceptionDistance, float eyeToZedDistance, float planeDistance, float HMDFocal, float zedFocal);

	[DllImport(nameDll, EntryPoint = "dllz_compute_hmd_focal")]
	private static extern float dllz_compute_hmd_focal(sl.Resolution r, float w, float h);

	/*****LATENCY CORRECTOR***/
	[DllImport(nameDll, EntryPoint = "dllz_latency_corrector_add_key_pose")]
	private static extern void dllz_latency_corrector_add_key_pose(ref Vector3 translation, ref Quaternion rotation, ulong timeStamp);

	[DllImport(nameDll, EntryPoint = "dllz_latency_corrector_get_transform")]
	private static extern void dllz_latency_corrector_get_transform(ulong timeStamp, bool useLatency,out Vector3 translation, out Quaternion rotation);

	[DllImport(nameDll, EntryPoint = "dllz_latency_corrector_initialize")]
	private static extern void dllz_latency_corrector_initialize();

	[DllImport(nameDll, EntryPoint = "dllz_latency_corrector_shutdown")]
	private static extern void dllz_latency_corrector_shutdown();

	/****ANTI DRIFT ***/
	[DllImport(nameDll, EntryPoint = "dllz_drift_corrector_initialize")]
	public static extern void dllz_drift_corrector_initialize();

	[DllImport(nameDll, EntryPoint = "dllz_drift_corrector_shutdown")]
	public static extern void dllz_drift_corrector_shutdown();

	[DllImport(nameDll, EntryPoint = "dllz_drift_corrector_get_tracking_data")]
	public static extern void dllz_drift_corrector_get_tracking_data(ref TrackingData trackingData, ref Pose HMDTransform, ref Pose latencyCorrectorTransform, int hasValidTrackingPosition,bool checkDrift);

	[DllImport(nameDll, EntryPoint = "dllz_drift_corrector_set_calibration_transform")]
	public static extern void dllz_drift_corrector_set_calibration_transform(ref Pose pose);

	[DllImport(nameDll, EntryPoint = "dllz_drift_corrector_set_calibration_const_offset_transform")]
	public static extern void dllz_drift_corrector_set_calibration_const_offset_transform(ref Pose pose);

	/// <summary>
	/// Container for the latency corrector
	/// </summary>
	public struct KeyPose
	{
		public Quaternion Orientation;
		public Vector3 Translation;
		public ulong Timestamp;
	};


	[StructLayout(LayoutKind.Sequential)]
	public struct Pose
	{
		public Vector3 translation;
		public Quaternion rotation;

		public Pose(Vector3 t, Quaternion q)
		{
			translation = t;
			rotation = q;
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct TrackingData
	{
		public Pose zedPathTransform;
		public Pose zedWorldTransform;
		public Pose offsetZedWorldTransform;

		public int trackingState;
	}
	/// <summary>
	/// Final GameObject Left
	/// </summary>
	public GameObject finalCameraLeft;
	/// <summary>
	/// Final GameObject right
	/// </summary>
	public GameObject finalCameraRight;

	/// <summary>
	/// Intermediate camera left
	/// </summary>
	public GameObject ZEDEyeLeft;
	/// <summary>
	/// Inytermediate camera right
	/// </summary>
	public GameObject ZEDEyeRight;

	/// <summary>
	/// Intermediate Left screen
	/// </summary>
	public ZEDRenderingPlane leftScreen;
	/// <summary>
	/// Intermediate right screen
	/// </summary>
	public ZEDRenderingPlane rightScreen;

	/// <summary>
	/// Final quad left
	/// </summary>
	public Transform quadLeft;
	/// <summary>
	/// Final quad right
	/// </summary>
	public Transform quadRight;

	/// <summary>
	/// Final camera left
	/// </summary>
	public Camera finalLeftEye;
	/// <summary>
	/// Final camera right
	/// </summary>
	public Camera finalRightEye;

	/// <summary>
	/// Material from the final right plane
	/// </summary>
	public Material rightMaterial;
	/// <summary>
	/// Material from the final left plane
	/// </summary>
	public Material leftMaterial;

	/// <summary>
	/// Offset between the final plane and the camera
	/// </summary>
	public Vector3 offset = new Vector3(0, 0, (float)sl.Constant.PLANE_DISTANCE);

	/// <summary>
	/// Half baseilne offset to set betwwen the two intermediate cameras
	/// </summary>
	public Vector3 halfBaselineOffset;

	/// <summary>
	/// Reference to the ZEDCamera instance
	/// </summary>
	public sl.ZEDCamera zedCamera;

	/// <summary>
	/// Reference to the ZEDManager
	/// </summary>
	public ZEDManager manager;

	/// <summary>
	/// Flag set to true when the target textures from the overlays are ready
	/// </summary>
	public bool ready = false;

	/// <summary>
	/// Flag grab ready, used to collect pose the latest time possible
	/// </summary>
	public bool grabSucceeded = false;

	/// <summary>
	/// Flag the ZED is ready
	/// </summary>
	public bool zedReady = false;

	/// <summary>
	/// The latency pose.
	/// </summary>
	private Pose latencyPose;

	/// <summary>
	/// Contains the last position computed by the anti drift
	/// </summary>
	public TrackingData trackingData = new TrackingData();
	#if UNITY_2017_OR_NEWER
	List<UnityEngine.VR.VRNodeState> nodes = new List<UnityEngine.VR.VRNodeState>();

	UnityEngine.VR.VRNodeState nodeState = new UnityEngine.VR.VRNodeState();
	#endif
	private void Awake()
	{
		dllz_latency_corrector_initialize();
		dllz_drift_corrector_initialize();
		#if UNITY_2017_OR_NEWER

		nodeState.nodeType = VRNode.Head;
		nodes.Add(nodeState);
		#endif
	}

	/// <summary>
	/// Start this instance.
	/// </summary>
	void Start()
	{
		manager = transform.parent.GetComponent<ZEDManager>();
		zedCamera = sl.ZEDCamera.GetInstance();
		leftScreen = ZEDEyeLeft.GetComponent<ZEDRenderingPlane>();
		rightScreen = ZEDEyeRight.GetComponent<ZEDRenderingPlane>();
		finalLeftEye = finalCameraLeft.GetComponent<Camera>();
		finalRightEye = finalCameraRight.GetComponent<Camera>();

		rightMaterial = quadRight.GetComponent<Renderer>().material;
		leftMaterial = quadLeft.GetComponent<Renderer>().material;
		finalLeftEye.SetReplacementShader(leftMaterial.shader, "");
		finalRightEye.SetReplacementShader(rightMaterial.shader, "");

		float plane_dist = (float)sl.Constant.PLANE_DISTANCE;
		scale(quadLeft.gameObject, finalLeftEye, new Vector2(1.78f*plane_dist, 1.0f*plane_dist));
		scale(quadRight.gameObject, finalRightEye, new Vector2(1.78f*plane_dist, 1.0f*plane_dist));
		zedReady = false;
		Camera.onPreRender += PreRender;
	}

	/// <summary>
	/// Compute the size of the final planes
	/// </summary>
	/// <param name="resolution"></param>
	/// <param name="perceptionDistance"></param>
	/// <param name="eyeToZedDistance"></param>
	/// <param name="planeDistance"></param>
	/// <param name="HMDFocal"></param>
	/// <param name="zedFocal"></param>
	/// <returns></returns>
	public Vector2 ComputeSizePlaneWithGamma(sl.Resolution resolution, float perceptionDistance, float eyeToZedDistance, float planeDistance, float HMDFocal, float zedFocal)
	{
		System.IntPtr p = dllz_compute_size_plane_with_gamma(resolution, perceptionDistance, eyeToZedDistance, planeDistance, HMDFocal, zedFocal);

		if (p == System.IntPtr.Zero)
		{
			return new Vector2();
		}
		Vector2 parameters = (Vector2)Marshal.PtrToStructure(p, typeof(Vector2));
		return parameters;

	}

	/// <summary>
	/// Compute the focal
	/// </summary>
	/// <param name="targetSize"></param>
	/// <returns></returns>
	public float ComputeFocal(sl.Resolution targetSize)
	{
		float focal_hmd = dllz_compute_hmd_focal(targetSize, finalLeftEye.projectionMatrix.m00,finalLeftEye.projectionMatrix.m11);
		return focal_hmd;
	}

	void ZEDReady()
	{

		Vector2 scaleFromZED;
		halfBaselineOffset.x = zedCamera.Baseline / 2.0f;

		float perception_distance =1.0f; //meters
		float zed2eye_distance = 0.1f;

		if (UnityEngine.XR.XRDevice.isPresent) 
		{
			Debug.Log ("Headset Detected : " + UnityEngine.XR.XRDevice.model);
			sl.CalibrationParameters parameters = zedCamera.CalibrationParametersRectified;

			scaleFromZED = ComputeSizePlaneWithGamma (new sl.Resolution ((uint)zedCamera.ImageWidth, (uint)zedCamera.ImageHeight),
				perception_distance, zed2eye_distance, offset.z,
				ComputeFocal (new sl.Resolution ((uint)UnityEngine.XR.XRSettings.eyeTextureWidth, (uint)UnityEngine.XR.XRSettings.eyeTextureHeight)),//571.677612f,
				parameters.leftCam.fx);

			//Move the plane with the optical centers
			scale (quadLeft.gameObject, finalLeftEye, scaleFromZED);
			scale (quadRight.gameObject, finalRightEye, scaleFromZED);
			ready = false;


			Debug.Log ("Screen size : " + Screen.width);

			zedCamera.ResetTracking (UnityEngine.XR.InputTracking.GetLocalRotation (UnityEngine.XR.XRNode.Head), UnityEngine.XR.InputTracking.GetLocalPosition (UnityEngine.XR.XRNode.Head));

		}

		zedReady = true;

	}

	public void OnEnable()
	{
		ZEDManager.OnZEDReady += ZEDReady;
	}

	public void OnDisable()
	{
		ZEDManager.OnZEDReady -= ZEDReady;
	}

	void OnGrab()
	{
		grabSucceeded = true;
	}

	/// <summary>
	/// Collect positions used in the latency corrector
	/// </summary>
	public void CollectPose()
	{
		KeyPose k = new KeyPose();
		k.Orientation = UnityEngine.XR.InputTracking.GetLocalRotation(UnityEngine.XR.XRNode.Head);
		k.Translation = UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.Head);
		if (sl.ZEDCamera.GetInstance().IsCameraReady)
		{
			k.Timestamp = sl.ZEDCamera.GetInstance().GetCurrentTimeStamp();
			if (k.Timestamp >= 0)
			{
				dllz_latency_corrector_add_key_pose(ref k.Translation, ref k.Orientation, k.Timestamp);
			}
		}
	}

	/// <summary>
	/// Returns a pose at a specific time
	/// </summary>
	/// <param name="r"></param>
	/// <param name="t"></param>
	public void LatencyCorrector(out Quaternion r, out Vector3 t, ulong cameraTimeStamp,bool useLatency)
	{
		dllz_latency_corrector_get_transform(cameraTimeStamp,useLatency,out t, out r);
	}

	public void scale(GameObject screen, Camera cam, Vector2 s)
	{
		screen.transform.localScale = new Vector3(s.x, s.y, 1);
	}

	/// <summary>
	/// Set the pose to the final planes with the latency corrector
	/// </summary>
	public void UpdateRenderPlane()
	{
		//Debug.Log("ENTER AR ZED UpdateLatencyCorrector");
		if (!ZEDManager.IsStereoRig) return;

		Quaternion r;
		r = latencyPose.rotation;
	 
		quadLeft.localRotation = r;
		quadLeft.localPosition = finalLeftEye.transform.localPosition + r * (offset);
		quadRight.localRotation = r;
		quadRight.localPosition = finalRightEye.transform.localPosition + r * (offset);

	}

	/// <summary>
	/// Init the tracking with the HMD IMU
	/// </summary>
	/// <returns></returns>
	public Pose InitTrackingAR()
	{
		Transform tmpHMD = transform;
		tmpHMD.position = UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.Head);
		tmpHMD.rotation = UnityEngine.XR.InputTracking.GetLocalRotation (UnityEngine.XR.XRNode.Head);
			

		Quaternion rot = Quaternion.identity;
		Vector3 pos = Vector3.zero;
		pos.x = -zedCamera.Baseline/2;
		pos.y = 0.0f;
		pos.z = 0.135f;
		Pose calib = new Pose(pos, rot);
		dllz_drift_corrector_set_calibration_transform(ref calib);


		Quaternion r = Quaternion.Inverse(tmpHMD.rotation) * rot;
		Vector3 t = tmpHMD.InverseTransformPoint(pos);
		Pose const_offset = new Pose(t, r);
		dllz_drift_corrector_set_calibration_const_offset_transform(ref const_offset);

		return new Pose(tmpHMD.position, tmpHMD.rotation);
	}



	public void ExtractLatencyPose(ulong cameraTimeStamp)
	{
		Quaternion latency_rot;
		Vector3 latency_pos;
		LatencyCorrector(out latency_rot, out latency_pos, cameraTimeStamp,true);     
		latencyPose = new Pose(latency_pos, latency_rot);
	}

	public Pose LatencyPose()
	{
		return latencyPose;
	}

	public void AdjustTrackingAR(Vector3 position, Quaternion orientation, out Quaternion r, out Vector3 t)
	{

		Pose hmdTransform = new Pose(UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.Head), UnityEngine.XR.InputTracking.GetLocalRotation(UnityEngine.XR.XRNode.Head));
		trackingData.trackingState = (int)manager.LastTrackingState;
		trackingData.zedPathTransform = new Pose(position, orientation);
		dllz_drift_corrector_get_tracking_data(ref trackingData, ref hmdTransform, ref latencyPose, 1,true);
		r = trackingData.offsetZedWorldTransform.rotation;
		t = trackingData.offsetZedWorldTransform.translation;

	}



	private void OnApplicationQuit()
	{
		dllz_latency_corrector_shutdown();
		dllz_drift_corrector_shutdown();

	}

	public void LateUpdateHdmRendering()
	{
		//Debug.Log("ENTER AR ZED LateUpdate");
		if (!ready)
		{
			if (leftScreen.target != null && leftScreen.target.IsCreated())
			{
				leftMaterial.SetTexture("_MainTex", leftScreen.target);
				ready = true;
			}
			else ready = false;
			if (rightScreen.target != null && rightScreen.target.IsCreated())
			{
				rightMaterial.SetTexture("_MainTex", rightScreen.target);
				ready = true;
			}
			else ready = false;
		}


		if (UnityEngine.XR.XRDevice.isPresent)
		{
			CollectPose ();
			UpdateRenderPlane();
		}
	}


	/// <summary>
	/// Update Before ZED is actually ready
	/// </summary>
	/// <param name="cam">Cam.</param>
	public void PreRender(Camera cam)
	{
		if (cam == finalLeftEye || cam == finalRightEye)
		{
			if ((!zedReady && ZEDManager.IsStereoRig))
			{
				quadLeft.localRotation = UnityEngine.XR.InputTracking.GetLocalRotation(UnityEngine.XR.XRNode.Head);
				quadLeft.localPosition = UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.Head) + quadLeft.localRotation * offset;

				quadRight.localRotation = UnityEngine.XR.InputTracking.GetLocalRotation(UnityEngine.XR.XRNode.Head);
				quadRight.localPosition = UnityEngine.XR.InputTracking.GetLocalPosition(UnityEngine.XR.XRNode.Head) + quadRight.localRotation * offset;

			}
		}
	}
}
