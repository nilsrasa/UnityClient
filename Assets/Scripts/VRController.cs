using System.Collections.Generic;
using Fove.Managed;
using UnityEngine;

//Script attached to the player controlling VR HMD and its interfaces
public class VRController : MonoBehaviour
{
    public static VRController Instance { get; private set; }

    [SerializeField] private Transform _optimalHeadPosition;

    [Header("Cursor")]
    [SerializeField] private Transform _cursorCanvas;
    [SerializeField] private float _cursorDistance = 0.4f;

    [Header("Mouse Controls")]
    [SerializeField] private float _mouseRotationSpeed = 2;
    
    public Transform Head;

    private FoveInterface _foveInterface;
    private GazeObject _hoveredGazeObject;
    private StreamController.ControlType _selectedControlType;
    private bool _initialized;

    void Awake()
    {
        Instance = this;
        _foveInterface = Head.GetComponent<FoveInterface>();
    }

    void Start()
    {
        //CenterHead();
        _cursorCanvas.position = Head.position + transform.forward * _cursorDistance;
    }

    //Gets point where user is looking every frame and interacts with any intersecting gazeobjects if possible
	void Update ()
	{
	    if (!_initialized) return;
		if (Input.GetKeyDown(KeyCode.C))
            CenterHead();
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
	    Ray ray = new Ray();
	    switch (_selectedControlType)
	    {
	        case StreamController.ControlType.Head:
	            ray = new Ray(Head.position, Head.forward * 1000);
                break;
	        case StreamController.ControlType.Eyes_Mouse:
            case StreamController.ControlType.Mouse:
	            if (Input.GetMouseButtonDown(1)) {
	            }
	            if (Input.GetMouseButton(1)) {
	                Head.Rotate(Vector3.up, Input.GetAxis("Mouse X") * _mouseRotationSpeed, Space.Self);
	                Head.Rotate(Vector3.right, -Input.GetAxis("Mouse Y") * _mouseRotationSpeed, Space.Self);
	                Head.localRotation = Quaternion.Euler(Head.localEulerAngles.x, Head.localEulerAngles.y, 0);
	            }

	            if (Input.GetMouseButton(0) || _selectedControlType == StreamController.ControlType.Eyes_Mouse)
	            {
	                ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                }
                else {
	                ResetHoveredObject();
	                return;
	            }
                break;
	        case StreamController.ControlType.Eyes:
                List<Vector3> eyeDirections = new List<Vector3>();
	            FoveInterfaceBase.EyeRays rays = _foveInterface.GetGazeRays();
	            EFVR_Eye eyeClosed = FoveInterface.CheckEyesClosed();
                if (eyeClosed != EFVR_Eye.Both && eyeClosed != EFVR_Eye.Left)
                    eyeDirections.Add(rays.left.direction);
                if (eyeClosed != EFVR_Eye.Both && eyeClosed != EFVR_Eye.Right)
                    eyeDirections.Add(rays.right.direction);
	            Vector3 direction = Vector3.zero;

	            foreach (Vector3 eyeDirection in eyeDirections) {
	                direction += eyeDirection;
	            }
	            direction = direction / eyeDirections.Count;

                ray = new Ray(Head.transform.position, direction * 1000);
                break;
	    }

        //Positioning of the cursor
	    _cursorCanvas.position = Head.position + ray.direction * _cursorDistance;

        Debug.DrawRay(ray.origin, ray.direction);
	    RaycastHit hit;
	    if (Physics.Raycast(ray, out hit))
	    {
            GazeObject gazeObject = hit.collider.GetComponent<GazeObject>();
	        if (gazeObject == null)
	        {
                ResetHoveredObject();
	            return;
	        }
	        RobotControlTrackPad robotControl = gazeObject.GetComponent<RobotControlTrackPad>();
	        if (robotControl != null)
	        {
	            Vector2 controlResult = robotControl.GetControlResult(hit.point);
                if (robotControl.IsActivated)
                    RobotInterface.Instance.SendCommand(controlResult);
            }
	        else
	        {
                RobotInterface.Instance.SendCommand(Vector2.zero);
	        }
	        if (gazeObject == _hoveredGazeObject) return;
            if (_hoveredGazeObject != null) _hoveredGazeObject.OnUnhover();
            gazeObject.OnHover();
	        _hoveredGazeObject = gazeObject;
	    }
	    else
	        ResetHoveredObject();
	    
    }

    private void ResetHoveredObject()
    {
        if (_hoveredGazeObject != null)
            _hoveredGazeObject.OnUnhover();
        _hoveredGazeObject = null;
        RobotInterface.Instance.SendCommand(Vector2.zero);
    }

    /// <summary>
    /// Centers the player's head position so that they are looking forward
    /// </summary>
    private void CenterHead() {
        Quaternion qrot = Quaternion.Inverse(Head.rotation) * _optimalHeadPosition.rotation;
        Head.parent.rotation = Head.parent.rotation * qrot;
        Vector3 movementToCenter = _optimalHeadPosition.position - Head.position;
        Vector3 hcPos = Head.parent.position;
        Head.parent.position = hcPos + movementToCenter;
    }

    public void Initialize(StreamController.ControlType controlType)
    {
        _selectedControlType = controlType;
        _initialized = true;
    }

    public void RotateSeat(float deltaAngle)
    {
        transform.localEulerAngles += new Vector3(0, deltaAngle, 0);
    }

    public void CenterSeat()
    {
        transform.localEulerAngles = Vector3.zero;
    }

}
