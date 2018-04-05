using UnityEngine;
using System;
using System.Threading;
using UnityEngine.VR;

#if UNITY_EDITOR
using UnityEditor;
#endif


public class ZEDManager : MonoBehaviour
{
	// set to true to activate dll wrapper verbose file (C:/ProgramData/STEREOLABS/SL_Unity_wrapper.txt)
	// Default  : false 
	// Warning : this can decrease performances. Only for Debug
    private bool wrapperVerbose = true;
    /// <summary>
    /// Current instance of the ZED Camera
    /// </summary>
    public sl.ZEDCamera zedCamera;
	/// <summary>
	/// Init parameters to the ZED (parameters of open())
	/// </summary>
	private sl.InitParameters initParameters;
	/// <summary>
	/// Runtime parameters to the ZED (parameters of grab())
	/// </summary>
	private sl.RuntimeParameters runtimeParameters;


    [Header("Camera")]
    /// <summary>
    /// Selected resolution
    /// </summary>
    public sl.RESOLUTION resolution = sl.RESOLUTION.HD720;
    /// <summary>
    /// Targeted FPS
    /// </summary>
    private int FPS = 60;
    /// <summary>
    /// Depth mode
    /// </summary>
    public sl.DEPTH_MODE depthMode = sl.DEPTH_MODE.PERFORMANCE;
    /// <summary>
    /// Sensing mode
	/// Always use the sensing mode FILL, since we need a depth without holes
    /// </summary>
    private sl.SENSING_MODE sensingMode = sl.SENSING_MODE.FILL;


    [Header("Motion Tracking")]
    /// <summary>
    /// Enables the tracking, if true, the tracking computed will be set to the gameObject. 
    /// If false, the tracking will be computed for the depth wrapping but not used
    /// </summary>
    public bool enableTracking = true;
	/// <summary>
	/// Enables the spatial memory
	/// </summary>
	public bool enableSpatialMemory = false;
	/// <summary>
	/// Enables the pose smoothing during drift correction (for MR experience, it is advised to leave it at true)
	/// </summary>
	private bool enablePoseSmoothing = false;
	/// <summary>
	/// Area file path
	/// </summary>
	public string pathSpatialMemory = "ZED_spatial_memory";
	/// <summary>
	/// Initialize the tracking referentiel, prefer use CENTER in AR mode, and LEFT in non VR mode
	/// </summary>
	private sl.TRACKING_FRAME trackingFrame;


	public enum ZEDRenderingMode
	{
		FORWARD = RenderingPath.Forward,
		DEFERRED = RenderingPath.DeferredShading
	};

	[Header("Rendering")]
	/// <summary>
	/// Enable camera overlay
	/// </summary>
	[Tooltip("Enable ZED images")]
	public bool videoOverlay = true;

	public bool depthOcclusion = true;

	[LabelOverride("AR Post-processing")]
	public bool postProcessing = true;

	[Range(0, 1)]
	public float cameraBrightness = 1.0f;

	public ZEDRenderingMode renderingPath = ZEDRenderingMode.FORWARD;




	////////////////////////////
	/// 
	/// <summary>
	/// Activate/DeActivate depth stabilizer
	/// </summary>
	private bool depthStabilizer = true;
    /// <summary>
    /// Is camera moving
    /// </summary>
    private bool isZEDTracked = false;
    /// <summary>
    /// Checks if the tracking has been activated
    /// </summary>
    private bool isTrackingEnable = false;

    /// <summary>
    /// Orientation returned by the tracker
    /// </summary>
	private Quaternion zedOrientation = Quaternion.identity;
    /// <summary>
    /// Position returned by the tracker
    /// </summary>
	private Vector3 zedPosition = new Vector3();
    /// <summary>
    /// Manages the read and write of SVO
    /// </summary>
    private ZEDSVOManager zedSVOManager;

    /// <summary>
    /// First position registered, in AR the position is the headset
    /// </summary>
    private Vector3 initialPosition = new Vector3();

    /// <summary>
    /// First rotation registered, in AR the rotation is the headset
    /// </summary>
	private Quaternion initialRotation = Quaternion.identity;



    /// <summary>
    /// Rotation offset used to retrieve the tracking with an offset of rotation
    /// </summary>
    private Quaternion rotationOffset;

    /// <summary>
    /// Position offset used to retrieve the tracking with an offset of position
    /// </summary>
    private Vector3 positionOffset;



    /// <summary>
    /// Flag to check is AR mode is activated
    /// </summary>
    private static bool isStereoRig = false;
    public static bool IsStereoRig
    {
        get { return isStereoRig; }
    }


	/// <summary>
	/// Flag to check is AR with Headset mode is activated
	/// </summary>
	private static bool isHMDRig = false;
	public static bool IsHMDRig
	{
		get { return isHMDRig; }
	}



	// Thread for grab() call
	private Thread threadGrab = null;
	// Mutex for grab() call
	public object grabLock = new object();


	//True if the thread is running
	private bool running = false;

    /// <summary>
    /// Contains the transform of the camera left
    /// </summary>
	private Transform cameraLeft = null;

    /// <summary>
    /// Contains the transform of the right camera if enabled
    /// </summary>
	private Transform cameraRight = null;

    /// <summary>
	///Contains the position of the player's head, different from ZED's position
	/// But the position of the ZED regarding this transform does not change during use (rigid transform)
    /// </summary>
	private Transform zedRigRoot = null;


	/// <summary>
	/// Get the center transform, the only one moved by the tracker on AR
	/// </summary>
	/// <returns></returns>
	public Transform GetZedRootTansform()
	{
		return zedRigRoot;
	}

	/// <summary>
	/// Get the left camera, better use this one, always available
	/// </summary>
	/// <returns></returns>
	public Transform GetLeftCameraTransform()
	{
		return cameraLeft;
	}

	/// <summary>
	/// Get the right camera, only available on AR
	/// </summary>
	/// <returns></returns>
	public Transform GetRightCameraTransform()
	{
		return cameraRight;
	}

    /// <summary>
    /// Last message received from the ZED during the Init
    /// </summary>
	public static sl.ERROR_CODE LastErrorMessageInit = sl.ERROR_CODE.ERROR_CODE_LAST;
	private bool openingLaunched; 
	private Thread threadOpening = null;

    public delegate void OnZEDManagerReady();
    public static event OnZEDManagerReady OnZEDReady;

    /* Layers used in AR mode, the layerLeftScreen is used everywhere */
    private int layerLeftScreen = 8;
    private int layerRightScreen = 10;
    private int layerLeftFinalScreen = 9;
    private int layerRightFinalScreen = 11;


    /// <summary>
    /// Counter of tries to open the ZED
    /// </summary>
    private uint numberTriesOpening = 0;

    /// <summary>
    /// Thread to init the tracking  (the tracking takes some time to Init)
    /// </summary>
    private Thread trackerThread;



    /// <summary>
    /// Checks if the thread init is over
    /// </summary>
    private bool zedInitOver = false;
    public bool IsZEDInitialized
    {
        get { return zedInitOver; }
    }


    /// <summary>
    /// Tracking state used by the Anti-drift
    /// </summary>
    private sl.TRACKING_FRAME_STATE lastTrackingState = sl.TRACKING_FRAME_STATE.TRACKING_OFF;
    public sl.TRACKING_FRAME_STATE LastTrackingState
    {
        get { return lastTrackingState; }
    }


    private Vector3 originPose;
    /// <summary>
    /// First position registered after enabling the tracking
    /// </summary>
    public Vector3 OriginPose { get { return originPose; } }

    private Quaternion originRotation;
    /// <summary>
    /// First rotation registered after enabling the tracking
    /// </summary>
    public Quaternion OriginRotation { get { return originRotation; } }

    [HideInInspector]
    public Quaternion gravityRotation = Quaternion.identity;


   



	public static ZEDManager instance;
	private static object lock_ = new object();
	public static ZEDManager GetInstance()
	{
		lock (lock_)
		{
			if (instance == null)
			{
				instance = new ZEDManager();
			}
			return instance;
		}

	}


#if UNITY_EDITOR
    void OnValidate()
    {
		 
        if (zedCamera != null)
        {
			
            if (!isTrackingEnable && enableTracking)
            {
                //Enables the tracking and initializes the first position of the camera
                Quaternion quat = Quaternion.identity;
                Vector3 vec = new Vector3(0, 0, 0);
				enablePoseSmoothing = enableSpatialMemory;
				if (!(enableTracking = (zedCamera.EnableTracking(ref quat, ref vec, enableSpatialMemory,enablePoseSmoothing, pathSpatialMemory) == sl.ERROR_CODE.SUCCESS)))
                {
					isZEDTracked = false;
                    throw new Exception(ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.TRACKING_NOT_INITIALIZED));
                }
                else
                {
                    isZEDTracked = true;
                    isTrackingEnable = true;
                }
            }


            if (isTrackingEnable && !enableTracking)
            {
				isZEDTracked = false;
				lock (grabLock) {
					zedCamera.DisableTracking ();
				}
                isTrackingEnable = false;
            }

 
			//Create ZEDTextureOverlay object to handle left images
			ZEDRenderingPlane textureLeftOverlay = GetLeftCameraTransform().GetComponent<ZEDRenderingPlane>();
			textureLeftOverlay.SetPostProcess(postProcessing);
            Shader.SetGlobalFloat("_ZEDFactorAffectReal", cameraBrightness);
           
			//Create ZEDTextureOverlay object to handle Right images if a right camera is present
			if (IsStereoRig)
            {
				ZEDRenderingPlane textureRightOverlay = GetRightCameraTransform().GetComponent<ZEDRenderingPlane>();
				textureRightOverlay.SetPostProcess(postProcessing);
            }

			if (renderingPath == ZEDRenderingMode.FORWARD) {
				if (depthOcclusion) {
					textureLeftOverlay.ManageKeyWordForwardPipe (false, "NO_DEPTH_OCC");
					if (IsStereoRig)
						textureLeftOverlay.ManageKeyWordForwardPipe (false, "NO_DEPTH_OCC");

				} else {
					textureLeftOverlay.ManageKeyWordForwardPipe (true, "NO_DEPTH_OCC");
					if (IsStereoRig)
						textureLeftOverlay.ManageKeyWordForwardPipe (true, "NO_DEPTH_OCC");
				}
    		}

        }

    }
#endif
    /// <summary>
    /// Set the referential, per default the tracking comes from the left camera.
    /// </summary>
    void SetTrackingRef()
    {
        rotationOffset = Quaternion.identity;
        switch (trackingFrame)
        {
		case sl.TRACKING_FRAME.LEFT_EYE:
                positionOffset = new Vector3(0, 0, 0);
                break;
		case sl.TRACKING_FRAME.RIGHT_EYE:
                positionOffset = new Vector3(zedCamera.Baseline, 0, 0);
                break;
		case sl.TRACKING_FRAME.CENTER_EYE:
                positionOffset = new Vector3(zedCamera.Baseline / 2.0f, 0, 0);
                break;
        }
    }


    #region CHECK_AR
    /// <summary>
    /// Check if there are two cameras, one for each eye as child
    /// </summary>
    private void CheckStereoMode()
    {

		zedRigRoot = gameObject.transform;

        bool devicePresent = UnityEngine.XR.XRDevice.isPresent;
        if (gameObject.transform.childCount > 0 && gameObject.transform.GetChild(0).gameObject.name.Contains("Camera_eyes"))
        {

            Component[] cams = gameObject.transform.GetChild(0).GetComponentsInChildren(typeof(Camera));
            foreach (Camera cam in cams)
            {
                if (cam.stereoTargetEye == StereoTargetEyeMask.Left)
                {
					cameraLeft = cam.transform;
					SetLayerRecursively(cameraLeft.gameObject, layerLeftScreen);

                        cam.cullingMask &= ~(1 << layerRightScreen);
                        cam.cullingMask &= ~(1 << layerRightFinalScreen);
                        cam.cullingMask &= ~(1 << layerLeftFinalScreen);
                        cam.cullingMask &= ~(1 << sl.ZEDCamera.TagOneObject);
                }
                else if (cam.stereoTargetEye == StereoTargetEyeMask.Right)
                {
					cameraRight = cam.transform;
					SetLayerRecursively(cameraRight.gameObject, layerRightScreen);
                        cam.cullingMask &= ~(1 << layerLeftScreen);
                        cam.cullingMask &= ~(1 << layerLeftFinalScreen);
                        cam.cullingMask &= ~(1 << layerRightFinalScreen);
                        cam.cullingMask &= ~(1 << sl.ZEDCamera.TagOneObject);

                }
            }
        }
        else
        {
            Component[] cams = gameObject.transform.GetComponentsInChildren(typeof(Camera));
            foreach (Camera cam in cams)
            {
                if (cam.stereoTargetEye == StereoTargetEyeMask.None)
                {
					cameraLeft = cam.transform;
                    cam.cullingMask = -1;
                    cam.cullingMask &= ~(1 << sl.ZEDCamera.TagOneObject);
                }
            }
        }



        if (cameraLeft && cameraRight)
        {
            isStereoRig = true;
			trackingFrame = sl.TRACKING_FRAME.LEFT_EYE;
			if (cameraLeft.transform.parent!=null)
				zedRigRoot = cameraLeft.transform.parent;
         }
        else
        {
			trackingFrame = sl.TRACKING_FRAME.LEFT_EYE;
            isStereoRig = false;
            Camera temp = cameraLeft.gameObject.GetComponent<Camera>();

			if (cameraLeft.transform.parent!=null)
				zedRigRoot = cameraLeft.transform.parent;
			
            foreach (Camera c in Camera.allCameras)
            {
                if (c != temp)
                {
                    c.cullingMask &= ~(1 << 8);
                    c.cullingMask &= ~(1 << sl.ZEDCamera.Tag);
                }
            }
            if (cameraLeft.gameObject.transform.childCount > 0)
            {
                cameraLeft.transform.GetChild(0).gameObject.layer = 8;
            }
        }
    }
    #endregion


	/// <summary>
	/// Set the layer number to the game object layer
	/// </summary>
    public static void SetLayerRecursively(GameObject go, int layerNumber)
    {
        if (go == null) return;
        foreach (Transform trans in go.GetComponentsInChildren<Transform>(true))
        {
            trans.gameObject.layer = layerNumber;
        }
    }

 

	private ulong cameraTimeStamp = 0;
	public ulong CameraTimeStamp
	{
		get { return cameraTimeStamp; }
	}


	private ulong lastcameraTimeStamp = 0;
	public ulong LastCameraTimeStamp
	{
		get { return lastcameraTimeStamp; }
	}

	private ulong imageTimeStamp = 0;
	public ulong ImageTimeStamp
	{
		get { return imageTimeStamp; }
	}


 
	//public delegate void OnGrabAction();
	//public static event OnGrabAction OnGrab;

	//public delegate void OnZEDDisconnectedAction();
	//public static event OnZEDDisconnectedAction OnZEDDisconnected;

	private bool requestNewFrame = false;
	private bool newFrameAvailable = false;

	/// <summary>
	/// Stops the current thread
	/// </summary>
	public void Destroy()
	{
		running = false;

		// In case the opening thread is still running
		if (threadOpening != null) 
		{
			threadOpening.Join();
			threadOpening = null;
		}

		// Shutdown grabbing thread
		if (threadGrab != null)
		{
			threadGrab.Join();
			threadGrab = null;
		}
	}

	/// <summary>
	/// Raises the application quit event.
	/// </summary>
	void OnApplicationQuit()
	{
		Destroy ();

		if (zedCamera != null)
		{
			if (zedSVOManager != null)
			{
				if (zedSVOManager.record)
				{
					zedCamera.DisableRecording();
				}
			}

			zedCamera.Destroy();
			zedCamera = null;
		}

	}



    void Awake()
    {
        //If you want the ZEDRig not to be destroyed
        DontDestroyOnLoad(transform.root);
	
        //Init the first parameters
        initParameters = new sl.InitParameters();
        initParameters.cameraFPS = FPS;
        initParameters.resolution = resolution;
        initParameters.depthMode = depthMode;
		initParameters.depthStabilization = depthStabilizer;
        //Check if the AR is needed and if possible to add
        CheckStereoMode();

        //Init the other options
        isZEDTracked = enableTracking;
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        zedPosition = initialPosition;
        zedOrientation = Quaternion.identity;

      
        //Create a camera and return an error message if the dependencies are not detected
        zedCamera = sl.ZEDCamera.GetInstance();
		LastErrorMessageInit = sl.ERROR_CODE.ERROR_CODE_LAST;

        zedSVOManager = GetComponent<ZEDSVOManager>();
		zedCamera.CreateCamera(wrapperVerbose);

        if (zedSVOManager != null)
        {
            //Create a camera
            if ((zedSVOManager.read || zedSVOManager.record) && zedSVOManager.videoFile.Length == 0)
            {
                zedSVOManager.record = false;
                zedSVOManager.read = false;
            }
            if (zedSVOManager.read)
            {
                zedSVOManager.record = false;
                initParameters.pathSVO = zedSVOManager.videoFile;
                initParameters.svoRealTimeMode = true;
				initParameters.depthStabilization = depthStabilizer;
            }
        }

		Debug.Log("ZED SDK : " + sl.ZEDCamera.GetSDKVersion() + "// ZED Unity Plugin : " + sl.ZEDCamera.PluginVersion);

 
		ZEDRenderingPlane textureLeftOverlay = GetLeftCameraTransform().GetComponent<ZEDRenderingPlane>();
		textureLeftOverlay.SetPostProcess (postProcessing);
        GetLeftCameraTransform().GetComponent<Camera>().renderingPath = (RenderingPath)(int)renderingPath;
        Shader.SetGlobalFloat("_ZEDFactorAffectReal", cameraBrightness);
        if (IsStereoRig)
        {
			ZEDRenderingPlane textureRightOverlay = GetRightCameraTransform().GetComponent<ZEDRenderingPlane>();
			textureRightOverlay.SetPostProcess (postProcessing);
            GetRightCameraTransform().GetComponent<Camera>().renderingPath = (RenderingPath)(int)renderingPath;
        }

		//Set the tracking referentiel
		SetTrackingRef();
		if (isStereoRig)
		{
			//Creates a CameraRig (the 2 last cameras)
			GameObject o = CreateZEDRigDisplayer();
			o.hideFlags = HideFlags.HideAndDontSave;
			o.transform.parent = transform;

			//Force some initParameters that are required for MR experience
			initParameters.enableRightSideMeasure = isStereoRig;
			initParameters.depthMinimumDistance = 0.1f;
			initParameters.depthMode = sl.DEPTH_MODE.PERFORMANCE;
			initParameters.depthStabilization = depthStabilizer;

			//Create the mirror, the texture from the firsts cameras is rendered to avoid a black border
			CreateMirror();
		}

        //Start the co routine to initialize the ZED and avoid to block the user
		LastErrorMessageInit = sl.ERROR_CODE.ERROR_CODE_LAST;
		openingLaunched = false;
	    StartCoroutine("InitZED");


    }


    #region INITIALIZATION
	void OpenZEDInBackground()
	{
		openingLaunched = true;
		LastErrorMessageInit = zedCamera.Init(ref initParameters);
		openingLaunched = false;
	}

 
    System.Collections.IEnumerator InitZED()
    {
	    zedInitOver = false;
		while (LastErrorMessageInit != sl.ERROR_CODE.SUCCESS)
        {
            //Initialize the camera
			if (!openingLaunched) {
				threadOpening = new Thread (new ThreadStart (OpenZEDInBackground));
				threadOpening.Start ();
			}
				 

			if (LastErrorMessageInit != sl.ERROR_CODE.SUCCESS)
            {
#if UNITY_EDITOR
                numberTriesOpening++;
                if (numberTriesOpening % 20 == 0 )
                {
                    Debug.LogWarning("[ZEDPlugin]: " + LastErrorMessageInit);
                }
                if (numberTriesOpening > 100)
                {
                    Debug.Log("[ZEDPlugin]: Stops initialization");
                    break;
                }
#endif
            }

            yield return new WaitForSeconds(0.3f);
        }


		//ZED has opened
        if (LastErrorMessageInit == sl.ERROR_CODE.SUCCESS)
        {
			threadOpening.Join ();
            //Initialize the threading mode, the positions with the AR and the SVO if needed
            //Launch the threading to enable the tracking
            ZEDReady();

            //Wait until the ZED of the init of the tracking
            while (enableTracking && !isTrackingEnable)
            {
                yield return new WaitForSeconds(0.5f);
            }

            //Calls all the observers, the ZED is ready :)
            if (OnZEDReady != null)
            {
                OnZEDReady();
            }

            float ratio = (float)Screen.width / (float)Screen.height;
            float target = 16.0f / 9.0f;
            if (Mathf.Abs(ratio - target) > 0.01)
            {
                ZEDLogMessage.Error2Str(ZEDLogMessage.ERROR.SCREEN_RESOLUTION);
            }
        }
        zedInitOver = true;


		//If not alreay launched, launch the grabbing thread
		if (!running) {

			running = true;
			requestNewFrame = true;

			threadGrab = new Thread (new ThreadStart (ThreadedZEDGrab));
			threadGrab.Start ();

		}
	 

    }


	/// <summary>
	/// Adjust camera(s) and render plane position regarding zedRigRoot transform
	/// The ZED Rig will then only be moved using zedRigRoot transform (each camera will keep its local position regarding zedRigRoot)
	/// </summary>
	void AdjustZEDRigTransform()
	{
		Vector3 rightCameraOffset = new Vector3 (zedCamera.Baseline, 0.0f, 0.0f);
		if (isStereoRig && UnityEngine.XR.XRDevice.isPresent) {

			// zedRigRoot transform (origin of the global camera) is placed on the HMD headset. Therefore we move the camera in front of it ( offsetHmdZEDPosition)
			// as when the camera is mount on the HMD. Values are provided by default. This can be done with a calibration as well
			// to know the exact position of the HMD regarding the camera
			Vector3 offsetHmdZEDPosition = new Vector3 (-zedCamera.Baseline / 2, 0.0f, 0.135f);
			Quaternion offsetHmdZEDRotation = Quaternion.identity;
			cameraLeft.localPosition = offsetHmdZEDPosition;
			cameraLeft.localRotation = offsetHmdZEDRotation;
			if (cameraRight) cameraRight.localPosition = offsetHmdZEDPosition + rightCameraOffset;
			if (cameraRight) cameraRight.localRotation = offsetHmdZEDRotation;

		} else if (isStereoRig && !UnityEngine.XR.XRDevice.isPresent) {
			// When no Hmd is available, simply put the origin at the left camera.
			cameraLeft.localPosition = Vector3.zero;
			cameraLeft.localRotation = Quaternion.identity;
			if (cameraRight) cameraRight.localPosition = rightCameraOffset;
			if (cameraRight) cameraRight.localRotation = Quaternion.identity;
		} else {
			cameraLeft.localPosition = Vector3.zero;
			cameraLeft.localRotation = Quaternion.identity;
		}
	}
    #endregion



	#region IMAGE_ACQUIZ
	private void ThreadedZEDGrab()
	{

		runtimeParameters = new sl.RuntimeParameters();
		runtimeParameters.sensingMode = sensingMode;
		runtimeParameters.enableDepth = true;
		// Don't change this ReferenceFrame. If we need normals in world frame, then we will do the convertion ourselves.
		runtimeParameters.measure3DReferenceFrame = sl.REFERENCE_FRAME.CAMERA;

		while (running)
		{
			if (zedCamera == null)
				return;

			AcquireImages ();
		}

	}

	private void AcquireImages()
	{

		if (requestNewFrame) 
		{
			
			/// call grab() to request a new frame
			sl.ERROR_CODE e = zedCamera.Grab (ref runtimeParameters);

			lock (grabLock) 
			{
				if (e == sl.ERROR_CODE.CAMERA_NOT_DETECTED) {
					Debug.Log ("Camera not detected or disconnected.");
					isDisconnected = true;
					Thread.Sleep (10);
					requestNewFrame = false;
				} else if (e == sl.ERROR_CODE.SUCCESS) {

					//Save the timestamp
					lastcameraTimeStamp = cameraTimeStamp;
					cameraTimeStamp = zedCamera.GetCameraTimeStamp ();  

					#if UNITY_EDITOR
					float frame_drop_count = zedCamera.GetFrameDroppedPercent();
					if (frame_drop_count>20)
						Debug.LogWarning("WARNING : More than 20% of frame drop detected");
					#endif

					//Get position of camera
					if (isTrackingEnable) {
						lastTrackingState = zedCamera.GetPosition (ref zedOrientation, ref zedPosition, ref rotationOffset, ref positionOffset);
					} else
						lastTrackingState = sl.TRACKING_FRAME_STATE.TRACKING_OFF;

					// Indicate that a new frame is available and pause the thread until a new request is called
					newFrameAvailable = true;
					requestNewFrame = false;
				} 
			}

		} 
		else 
		{
			//to avoid "overheat"
			Thread.Sleep (1);
		}
	}
	#endregion




    /// <summary>
    /// Init the SVO, and launch the thread to enable the tracking
    /// </summary>
    private void ZEDReady()
    {

        if (zedSVOManager != null)
        {
            if (zedSVOManager.record)
            {
                if (zedCamera.EnableRecording(zedSVOManager.videoFile) != sl.ERROR_CODE.SUCCESS)
                {
                    zedSVOManager.record = false;
                }
            }

            if (zedSVOManager.read)
            {
                zedSVOManager.NumberFrameMax = zedCamera.GetSVONumberOfFrames();
            }
        }



		AdjustZEDRigTransform ();

        if (isStereoRig)
        {
			ZEDMixedRealityPlugin.Pose pose = ar.InitTrackingAR();
            originPose = pose.translation;
            originRotation = pose.rotation;

        }
        else
        {
            originPose = transform.TransformPoint(initialPosition);
            originRotation = initialRotation;
        }

        if (enableTracking)
        {
            trackerThread = new Thread(EnableTrackingThreaded);
            trackerThread.Start();
        }


#if UNITY_EDITOR
        UnityEditor.EditorApplication.playmodeStateChanged = HandleOnPlayModeChanged;
#endif
    }

    /// <summary>
    /// Enables the thread to get the trackingr.up
    /// </summary>
    void EnableTrackingThreaded()
    {
		enablePoseSmoothing = enableSpatialMemory;
		lock (grabLock) {

			//Make sure we have "grabbed" on frame first
			sl.ERROR_CODE e = zedCamera.Grab (ref runtimeParameters);
			int timeOut_grab = 0;
			while (e != sl.ERROR_CODE.SUCCESS && timeOut_grab < 100) {
				e = zedCamera.Grab (ref runtimeParameters);
				Thread.Sleep (10);
				timeOut_grab++;
			}

			//Now enable the tracking with the proper parameters
			if (!(enableTracking = (zedCamera.EnableTracking (ref initialRotation, ref initialPosition, enableSpatialMemory, enablePoseSmoothing, pathSpatialMemory) == sl.ERROR_CODE.SUCCESS))) {
				throw new Exception (ZEDLogMessage.Error2Str (ZEDLogMessage.ERROR.TRACKING_NOT_INITIALIZED));
			} else {
				isTrackingEnable = true;
			}
		}
    }

#if UNITY_EDITOR
    void HandleOnPlayModeChanged()
    {

        if (zedCamera == null) return;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.playmodeStateChanged = HandleOnPlayModeChanged;
#endif
    }
#endif


	#region ENGINE_UPDATE

	/// <summary>
	/// If a new frame is available, this function retrieve the Images and update the texture at each engine tick
	/// Called in Update()
	/// </summary>
	public void UpdateImages()
	{
		if (zedCamera == null)
			return;

		if (videoOverlay) {

			if (newFrameAvailable) {

				lock (grabLock)
				{
					zedCamera.RetrieveTextures ();
					zedCamera.UpdateTextures();
					imageTimeStamp = zedCamera.GetImagesTimeStamp();
				}
								 
				requestNewFrame = true;
				newFrameAvailable = false;
			}

		}


		#region SVO Manager
		if (zedSVOManager != null)
		{
			if (zedSVOManager.record)
			{
				zedCamera.Record();
			}
			else if (zedSVOManager.read)
			{
				zedSVOManager.CurrentFrame = zedCamera.GetSVOPosition();
				if (zedSVOManager.loop && zedSVOManager.CurrentFrame >= zedCamera.GetSVONumberOfFrames() - 1)
				{
					zedCamera.SetSVOPosition(0);

					if (enableTracking)
					{

						if (!(enableTracking = (zedCamera.ResetTracking(initialRotation, initialPosition) == sl.ERROR_CODE.SUCCESS)))
						{
							throw new Exception("Error, tracking not available");
						}

						zedRigRoot.localPosition = initialPosition;
						zedRigRoot.localRotation = initialRotation;
					}
				}
			}
		}
		#endregion

	}


    /// <summary>
    /// Get the tracking position from the ZED and update the manager's position. If enable, update the AR Tracking
	/// Only called in LIVE mode
    /// Called in Update()
    /// </summary>
    private void UpdateTracking()
    {
		if (isZEDTracked)
        {

			Quaternion r;
			Vector3 v;
      
			if (UnityEngine.XR.XRDevice.isPresent && isStereoRig)
            {
				ar.ExtractLatencyPose(imageTimeStamp);
				ar.AdjustTrackingAR(zedPosition, zedOrientation, out r, out v);
				zedRigRoot.localRotation = r;
				zedRigRoot.localPosition = v;

            }
			else
			{
				zedRigRoot.localRotation = zedOrientation;
				zedRigRoot.localPosition = zedPosition;
            }
        }
        else
        {
			//If VR Device is available and we are in StereoMode, use the tracking from the HDM
			if (UnityEngine.XR.XRDevice.isPresent && isStereoRig) {
					ar.ExtractLatencyPose (imageTimeStamp);		
					zedRigRoot.localRotation = ar.LatencyPose ().rotation;
					zedRigRoot.localPosition = ar.LatencyPose ().translation;
			}
        }
     }

	/// <summary>
	/// Updates the collection of hmd pose (AR only)
	/// </summary>
	void updateHmdPose()
	{
		if (IsStereoRig && UnityEngine.XR.XRDevice.isPresent)
		{
			ar.CollectPose ();
		}
	}

    /// <summary>
    /// Update this instance. Called at each frame
    /// </summary>
	void Update()
    {

		UpdateImages ();
		updateHmdPose ();
		UpdateTracking ();

		//If the ZED is disconnected, to easily look at the message
		if (isDisconnected)
		{
			ZEDDisconnected ();
				
		}

    }

	public void LateUpdate()
	{
		if (IsStereoRig && UnityEngine.XR.XRDevice.isPresent) {
			ar.LateUpdateHdmRendering ();

			if (!zedCamera.IsHmdCompatible && zedCamera.IsCameraReady)
				Debug.LogWarning ("WARNING : Stereo Passtrough with a ZED is not recommended. You may consider using the ZED-M, designed for that purpose");
		}

	}
	#endregion

    private bool isDisconnected = false;
    /// <summary>
    /// Event called when camera is disconnected
    /// </summary>
    void ZEDDisconnected()
    {
		Destroy();

        if (zedCamera != null)
        {
            zedCamera.Destroy();
        }
        isDisconnected = true;

		if (UnityEngine.XR.XRDevice.isPresent) {
			zedRigRoot.position = UnityEngine.XR.InputTracking.GetLocalPosition (UnityEngine.XR.XRNode.Head);
			zedRigRoot.rotation = UnityEngine.XR.InputTracking.GetLocalRotation (UnityEngine.XR.XRNode.Head);
		}

    }


    #region AR_CAMERAS
    private ZEDMixedRealityPlugin ar;
    /// <summary>
	/// Create a GameObject to display the ZED in an headset (ZED-M Only)
    /// </summary>
    /// <returns></returns>
    private GameObject CreateZEDRigDisplayer()
    {
        GameObject zedRigDisplayer = new GameObject("ZEDRigDisplayer");
		ar =zedRigDisplayer.AddComponent<ZEDMixedRealityPlugin>();
  	 
		/*Screens : Left and right */
		GameObject leftScreen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        MeshRenderer meshLeftScreen = leftScreen.GetComponent<MeshRenderer>();
        meshLeftScreen.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        meshLeftScreen.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        meshLeftScreen.receiveShadows = false;
        meshLeftScreen.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        meshLeftScreen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshLeftScreen.sharedMaterial = Resources.Load("Materials/Unlit/Mat_ZED_Unlit") as Material;
        leftScreen.layer = layerLeftFinalScreen;
        GameObject.Destroy(leftScreen.GetComponent<MeshCollider>());

        GameObject rightScreen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        MeshRenderer meshRightScreen = rightScreen.GetComponent<MeshRenderer>();
        meshRightScreen.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        meshRightScreen.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        meshRightScreen.receiveShadows = false;
        meshRightScreen.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        meshRightScreen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        GameObject.Destroy(rightScreen.GetComponent<MeshCollider>());
        meshRightScreen.sharedMaterial = Resources.Load("Materials/Unlit/Mat_ZED_Unlit") as Material;
        rightScreen.layer = layerRightFinalScreen;

        /*Camera left and right*/
		GameObject camLeft = new GameObject("cameraLeft");
		camLeft.transform.SetParent(zedRigDisplayer.transform);
        Camera camL = camLeft.AddComponent<Camera>();
        camL.stereoTargetEye = StereoTargetEyeMask.Left;
        camL.renderingPath = RenderingPath.Forward;//Minimal overhead
        camL.clearFlags = CameraClearFlags.Color;
        camL.backgroundColor = Color.black;
        camL.cullingMask = 1 << layerLeftFinalScreen;
#if UNITY_5_6_OR_NEWER
        camL.allowHDR = false;
        camL.allowMSAA = false;
#endif

		GameObject camRight = new GameObject("cameraRight");
		camRight.transform.SetParent(zedRigDisplayer.transform);
        Camera camR = camRight.AddComponent<Camera>();
        camR.renderingPath = RenderingPath.Forward;//Minimal overhead
        camR.clearFlags = CameraClearFlags.Color;
        camR.backgroundColor = Color.black;
        camR.stereoTargetEye = StereoTargetEyeMask.Right;
        camR.cullingMask = 1 << layerRightFinalScreen;
#if UNITY_5_6_OR_NEWER
        camR.allowHDR = false;
        camR.allowMSAA = false;
#endif
        camRight.layer = layerRightFinalScreen;
        camLeft.layer = layerRightFinalScreen;

        //Hide camera in editor
#if UNITY_EDITOR
        LayerMask layerNumberBinary = (1 << layerRightFinalScreen); // This turns the layer number into the right binary number
        layerNumberBinary |= (1 << layerLeftFinalScreen);
        LayerMask flippedVisibleLayers = ~UnityEditor.Tools.visibleLayers;
        UnityEditor.Tools.visibleLayers = ~(flippedVisibleLayers | layerNumberBinary);
#endif
		leftScreen.transform.SetParent (zedRigDisplayer.transform);
		rightScreen.transform.SetParent (zedRigDisplayer.transform);

	
        ar.finalCameraLeft = camLeft;
        ar.finalCameraRight = camRight;
		ar.ZEDEyeLeft = cameraLeft.gameObject;
		ar.ZEDEyeRight = cameraRight.gameObject;
		ar.quadLeft = leftScreen.transform;
		ar.quadRight = rightScreen.transform;

		return zedRigDisplayer;
    }




    #endregion

    #region MIRROR
    private ZEDMirror mirror = null;
    private GameObject mirrorContainer = null;
    void CreateMirror()
    {
        if (mirrorContainer != null) return;
        mirrorContainer = new GameObject("Mirror");
        mirrorContainer.hideFlags = HideFlags.HideAndDontSave;

        GameObject camLeft = new GameObject("MirrorCamera");
        camLeft.hideFlags = HideFlags.HideAndDontSave;
        mirror = camLeft.AddComponent<ZEDMirror>();
        mirror.manager = this;

        camLeft.transform.parent = mirrorContainer.transform;
        Camera camL = camLeft.AddComponent<Camera>();
        camL.gameObject.layer = 8;
        camL.stereoTargetEye = StereoTargetEyeMask.None;
        camL.renderingPath = RenderingPath.Forward;//Minimal overhead
        camL.clearFlags = CameraClearFlags.Color;
        camL.backgroundColor = Color.black;
        camL.cullingMask = 0;

#if UNITY_5_6_OR_NEWER
        camL.allowHDR = false;
        camL.allowMSAA = false;
        camL.useOcclusionCulling = false;
#endif
    }
    #endregion


}



