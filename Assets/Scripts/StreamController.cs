using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Controls the player experience and camera input
public class StreamController : MonoBehaviour
{
    public enum ControlType
    {
        Head,
        Mouse,
        Eyes,
        Eyes_Mouse,
        Joystick
    }

    public static StreamController Instance { get; private set; }

    [SerializeField] public ControlType _selectedControlType = ControlType.Head;

    [Header("Cameras and Projection")] [SerializeField] private ThetaWebcamStream _cameraStreamUSB;
    [SerializeField] private QuestionManager _queryManager ;
    [SerializeField] private MeshRenderer _icosphere;
    [SerializeField] private MeshRenderer _icosphereDissolve;
    [SerializeField] private MeshRenderer _projectionSphere;
    [SerializeField] private float _sphereDissolveSpeed = 1;
    [SerializeField] private GameObject _projectionCamera;
    [SerializeField] private Vector3 _drivingModeRotationalOffset;
    [SerializeField] private FollowObject _projectionCameraFrontFollowObject;

    [Header("Chair Variables")] [SerializeField] private float _chairMaxSpeed = 2;
    [SerializeField] private float _chairAcceleration = 1;
    [SerializeField] private float _chairDeaccelerationDistance = 1;
    [SerializeField] private Transform _chair;
    [SerializeField] private Transform _chairFOVE;
    [SerializeField] private Transform _loopBeginPosition;
    [SerializeField] private Transform _loopEndPosition;
    [SerializeField] private Transform _chairEndPosition;
    [SerializeField] private Transform _shaftLid;
    [SerializeField] private FollowObject _rotatingControlsFollow;
    [SerializeField] private FollowObject _seatInterfaceFollow;

    [Space(5)] [Header("Lights")] [SerializeField] private List<Light> _shaftLights;
    [SerializeField] private List<Light> _domePerimiterLights;
    [SerializeField] private Light _domeTopLight;

    public Transform ActiveChair { get; private set; }

    private enum ChairState
    {
        Stopped,
        Accelerating,
        Moving,
        Deaccelerating
    }

    private bool _isLooping;
    private bool _isConnected;
    private bool _useFOVE;
    private float _currentChairSpeed;
    private float _accelTimer;
    private ChairState _currentChairState = ChairState.Stopped;

    void Awake()
    {
        Instance = this;
        //still use the FOVE even with joystick
        if (_selectedControlType == ControlType.Eyes || _selectedControlType == ControlType.Head || _selectedControlType==ControlType.Joystick) _useFOVE = true;
        ActiveChair = (_useFOVE) ? _chairFOVE : _chair;
        _chair.gameObject.SetActive(!_useFOVE);
        _chairFOVE.gameObject.SetActive(_useFOVE);
        _rotatingControlsFollow.ObjectToFollow = ActiveChair;
        _seatInterfaceFollow.ObjectToFollow = ActiveChair;
        VRController.Instance.Initialize(_selectedControlType);
    }

    void Start()
    {
        //Viewport.Instance.SetFollowTarget(VRController.Instance.Head);
    }


    public void Update()
    {
        //manual connect to robot and ascend chair instead of gaze track the button
        if (!_isConnected)
        {
            if (Input.GetKeyDown(KeyCode.A)){

                ConnectToRobot();
            }
        }

        
       
    }

    /// <summary>
    /// Starts animation of ascending the chair into the cockpit to start.
    /// Loops the chair so that the tube feels endless until a connection has been established.
    /// </summary>
    private IEnumerator AscendChair()
    {
        while (_currentChairState != ChairState.Stopped)
        {
            if (Vector3.Distance(ActiveChair.position, _chairEndPosition.position) < _chairDeaccelerationDistance && _currentChairState != ChairState.Stopped)
                _currentChairState = ChairState.Deaccelerating;

            if (_currentChairState == ChairState.Accelerating)
            {
                _currentChairSpeed = Mathf.Lerp(_currentChairSpeed, _chairMaxSpeed, _accelTimer += _chairAcceleration * Time.deltaTime);
                if (_currentChairSpeed >= _chairMaxSpeed)
                {
                    _currentChairSpeed = _chairMaxSpeed;
                    _currentChairState = ChairState.Moving;
                }
            }
            else if (_currentChairState == ChairState.Deaccelerating)
            {
                _currentChairSpeed = _chairMaxSpeed - _chairMaxSpeed * (_chairDeaccelerationDistance - Vector3.Distance(ActiveChair.position, _chairEndPosition.position)) / _chairDeaccelerationDistance;
            }

            ActiveChair.Translate(Vector3.up * _currentChairSpeed * Time.deltaTime);

            if (Vector3.Distance(ActiveChair.position, _loopEndPosition.position) < 1f)
            {
                if (_isLooping)
                    LoopChair();
                else
                {
                    _shaftLid.gameObject.SetActive(false);
                }
            }
            if (Vector3.Distance(ActiveChair.position, _chairEndPosition.position) < 0.1f)
            {
                _currentChairState = ChairState.Stopped;
                foreach (Light light in _shaftLights)
                    light.enabled = false;
                StartCoroutine(StartUpSequence());
            }
            ManageShaftLights();
            yield return new WaitForEndOfFrame();
        }
    }

    /// <summary>
    /// Turn on and off lights around the chair
    /// </summary>
    private void ManageShaftLights()
    {
        List<Light> lightsToTurnOn = new List<Light>();
        List<Light> sortedLights = new List<Light>();
        foreach (Light light in _shaftLights)
            if (light.transform.position.y >= ActiveChair.position.y)
                sortedLights.Add(light);

        sortedLights = _shaftLights.OrderBy(light => Vector3.Distance(light.transform.position, ActiveChair.position)).ToList();
        lightsToTurnOn = sortedLights.Take(2).ToList();
        foreach (Light light in _shaftLights)
            light.enabled = false;
        foreach (Light light in lightsToTurnOn)
            light.enabled = true;
    }

    private void LoopChair()
    {
        ActiveChair.position = _loopBeginPosition.position;
    }

    /// <summary>
    /// Starts the startup animation for lights and screens
    /// </summary>
    private IEnumerator StartUpSequence()
    {
        yield return new WaitForSeconds(1f);
        _domeTopLight.enabled = true;

        foreach (Light light in _domePerimiterLights)
        {
            yield return new WaitForSeconds(0.5f);
            light.enabled = true;
        }

        yield return new WaitForSeconds(0.75f);
        Viewport.Instance.SetEnabled(true);
        GuiController.Instance.SetRobotControlVisibility(true);
        yield return new WaitForSeconds(0.75f);
    }

    /// <summary>
    /// Plays animation of lights and screens for parking robot
    /// </summary>
    private IEnumerator ParkedModeSequence()
    {
        foreach (Light light in _domePerimiterLights)
        {
            light.enabled = false;
        }
        _domeTopLight.enabled = false;
        yield return new WaitForSeconds(1f);
        _icosphereDissolve.enabled = true;
        _icosphereDissolve.material.SetFloat("_Cutoff", 0);
        _icosphere.enabled = false;
        _projectionSphere.enabled = true;

        float dissolve = 0;
        while (dissolve < 1)
        {
            dissolve += Time.deltaTime * _sphereDissolveSpeed;
            _icosphereDissolve.material.SetFloat("_Cutoff", dissolve);
            yield return new WaitForEndOfFrame();
        }
        RobotInterface.Instance.DoneEnableParkMode();
    }

    /// <summary>
    /// Plays animation of lights and screes for disabling parked mode
    /// </summary>
    private IEnumerator DriveModeSequence()
    {
        float dissolve = 1;
        while (dissolve > 0)
        {
            dissolve -= Time.deltaTime * _sphereDissolveSpeed;
            _icosphereDissolve.material.SetFloat("_Cutoff", dissolve);
            yield return new WaitForEndOfFrame();
        }
        _projectionSphere.enabled = false;
        _icosphere.enabled = true;
        _icosphereDissolve.enabled = false;
        yield return new WaitForSeconds(1f);
        _domeTopLight.enabled = true;
        foreach (Light light in _domePerimiterLights)
        {
            light.enabled = true;
        }

        RobotInterface.Instance.DoneEnableDrivingMode();
    }

    private void StartVideoPlayer()
    {
    }

    /// <summary>
    /// Connects to USB 360 camera and starts up animations
    /// </summary>
    public void ConnectToRobot()
    {
        _cameraStreamUSB.StartStream();
        _currentChairState = ChairState.Accelerating;
        StartCoroutine(AscendChair());
        RobotInterface.Instance.Connect();
        _queryManager.EnableManager();
     
        //TODO: When online connection is put in, uncomment this
        //_isLooping = true;
        _isConnected = true;
    }

    public void RotateChair(float deltaAngle)
    {
        ActiveChair.eulerAngles = new Vector3(0, ActiveChair.eulerAngles.y + deltaAngle, 0);
    }


    public void EnableParkedMode()
    {
        StartCoroutine(ParkedModeSequence());
        SetViewportVisibility(false);
    }

    public void DisableParkedMode()
    {
        StartCoroutine(DriveModeSequence());
    }

    public void SetViewportVisibility(bool isVisible)
    {
        Viewport.Instance.SetEnabled(isVisible);
    }

    public void EnableDrivingMode()
    {
        Viewport.Instance.LockScreenToCenter();
        _projectionCameraFrontFollowObject.SetRotationalOffset(_drivingModeRotationalOffset);
        GuiController.Instance.SetDrivingControlsVisibility(true);
    }

    public void DisableDrivingMode()
    {
        Viewport.Instance.UnlockScreen();
        _projectionCameraFrontFollowObject.SetRotationalOffset(Vector3.zero);
        GuiController.Instance.SetDrivingControlsVisibility(false);
    }
}