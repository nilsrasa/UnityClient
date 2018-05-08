using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    public enum PlayerState
    {
        Normal,
        SorroundViewing
    }

    public static PlayerController Instance { get; set; }

    public delegate void MouseWasClicked(Ray mouseRay);

    public event MouseWasClicked OnMouseClick;

    [SerializeField] private float _mouseMovementSpeed = 5;
    [SerializeField] private float _mouseScrollSpeed = 10;
    [SerializeField] private float _mouseClickSpeed = 0.1f;
    [SerializeField] private float _cameraFocusDistance = 10;
    [SerializeField] private Transform _triggerFloor;

    private PlayerState _currentPlayerState;

    public PlayerState CurrentPlayerState
    {
        get { return _currentPlayerState; }
        set
        {
            _currentPlayerState = value;
            switch (value)
            {
                case PlayerState.Normal:
                    _camera.enabled = true;
                    break;
                case PlayerState.SorroundViewing:
                    _camera.enabled = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("value", value, null);
            }
        }
    }

    private Camera _camera;
    private Coroutine _mouseLeftClickCheck;
    private bool _isLeftMouseClick;
    private Coroutine _mouseRightClickCheck;
    private bool _isRightMouseClick;
    private MouseObject _hoveredMouseObject;
    private bool _isDraggingObject;
    private Vector2 _lastMousePos;
    private DragObject _dragObject;
    private float _dragOffset;

    void Awake()
    {
        _camera = GetComponentInChildren<Camera>();
        CurrentPlayerState = PlayerState.Normal;
        Instance = this;
    }

    void Update()
    {
        //Input events
        if (CurrentPlayerState == PlayerState.SorroundViewing) return;

        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Single.PositiveInfinity, LayerMask.GetMask("SorroundPhotoLocations")))
        {
            MouseObject hovered = hit.transform.GetComponent<MouseObject>();
            if (hovered != null)
            {
                if (_hoveredMouseObject != null)
                {
                    if (_hoveredMouseObject == hovered)
                        _hoveredMouseObject.Stayed();
                    else
                    {
                        UnHoverMouseObject();

                        _hoveredMouseObject = hovered;
                        _hoveredMouseObject.Hovered();
                    }
                }
                else
                {
                    _hoveredMouseObject = hovered;
                    _hoveredMouseObject.Hovered();
                }
            }
            else
            {
                UnHoverMouseObject();
            }
        }
        else
        {
            UnHoverMouseObject();
        }

        //Check if left/right mouse button was clicked or held
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit raycastHit;
            int layer = 1 << 15;
            if (Physics.Raycast(ray, out raycastHit, 1000, layer))
            {
                if (!_isDraggingObject)
                    StartDrag(raycastHit.transform.GetComponent<DragObject>());
            }
            else
            {
                _mouseLeftClickCheck = StartCoroutine(CheckForLeftClick(_mouseClickSpeed));
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            _mouseRightClickCheck = StartCoroutine(CheckForRightClick(_mouseClickSpeed));
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (_isDraggingObject)
            {
                StopDrag();
            }
            else
            {
                if (_isLeftMouseClick)
                    LeftMouseClicked();
                _isLeftMouseClick = false;
                if (_mouseLeftClickCheck != null)
                    StopCoroutine(_mouseLeftClickCheck);
            }
        }

        if (Input.GetMouseButtonUp(1))
        {
            if (_isRightMouseClick)
                RightMouseClicked();
            _isRightMouseClick = false;
            if (_mouseRightClickCheck != null)
                StopCoroutine(_mouseRightClickCheck);
        }

        if (Input.GetMouseButton(0) && !_isLeftMouseClick)
        {
            if (_isDraggingObject)
            {
                _dragObject.OnDrag(Vector3.Distance(Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, _dragOffset)), _dragObject.transform.position));
            }
            else
            {
                _camera.transform.rotation = Quaternion.Euler(
                    _camera.transform.eulerAngles.x + _mouseMovementSpeed * -Input.GetAxis("Mouse Y"),
                    _camera.transform.eulerAngles.y + 0,
                    _camera.transform.eulerAngles.z + 0);
                transform.eulerAngles = new Vector3(transform.eulerAngles.x,
                    transform.eulerAngles.y + _mouseMovementSpeed * Input.GetAxis("Mouse X"),
                    transform.eulerAngles.z);
            }
        }

        if (Input.GetMouseButton(1) && !_isRightMouseClick)
        {
            transform.Translate(_mouseMovementSpeed * -Input.GetAxis("Mouse X"),
                0,
                _mouseMovementSpeed * -Input.GetAxis("Mouse Y"));
        }

        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            transform.Translate(_camera.transform.forward * Input.GetAxis("Mouse ScrollWheel") * _mouseScrollSpeed, Space.World);
        }

        _triggerFloor.position = new Vector3(transform.position.x, _triggerFloor.position.y, transform.position.z);

    }

    private void UnHoverMouseObject()
    {
        if (_hoveredMouseObject != null)
            _hoveredMouseObject.Exited();
        _hoveredMouseObject = null;
    }

    private IEnumerator CheckForLeftClick(float time)
    {
        float timer = 0;
        _isLeftMouseClick = true;
        while (timer < time)
        {
            yield return new WaitForEndOfFrame();
            timer += Time.deltaTime;
        }
        _isLeftMouseClick = false;
    }

    private IEnumerator CheckForRightClick(float time)
    {
        float timer = 0;
        _isRightMouseClick = true;
        while (timer < time)
        {
            yield return new WaitForEndOfFrame();
            timer += Time.deltaTime;
        }
        _isRightMouseClick = false;
    }

    private void LeftMouseClicked()
    {
        //Checks if UI was clicked
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (_hoveredMouseObject && OnMouseClick == null)
        {
            _hoveredMouseObject.Clicked();
            return;
        }
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

        //If something subscribed to mouse click, redirect
        if (OnMouseClick != null)
        {
            OnMouseClick(ray);
            return;
        }

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000))
        {
            WaypointMarker marker = hit.collider.GetComponent<WaypointMarker>();
            if (marker != null)
            {
                if (RobotMasterController.SelectedRobot != null)
                {
                    if (RobotMasterController.SelectedRobot.CurrenLocomotionType == ROSController.RobotLocomotionType.DIRECT || RobotMasterController.SelectedRobot.CurrentRobotLocomotionState == ROSController.RobotLocomotionState.STOPPED)
                    {
                        WaypointController.Instance.DeleteMarker(marker);
                    }
                }
                else
                {
                    WaypointController.Instance.DeleteMarker(marker);
                }

                return;
            }
            if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Floor")) return;

            if (RobotMasterController.SelectedRobot != null)
                if (RobotMasterController.SelectedRobot.CurrenLocomotionType == ROSController.RobotLocomotionType.DIRECT || RobotMasterController.SelectedRobot.CurrentRobotLocomotionState == ROSController.RobotLocomotionState.STOPPED)
                    WaypointController.Instance.CreateWaypoint(hit.point);
        }
    }

    private void RightMouseClicked()
    {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000))
        {
            WaypointMarker marker = hit.collider.GetComponent<WaypointMarker>();
            if (marker != null)
            {
                if (RobotMasterController.SelectedRobot != null)
                {
                    if (RobotMasterController.SelectedRobot.CurrenLocomotionType == ROSController.RobotLocomotionType.DIRECT || RobotMasterController.SelectedRobot.CurrentRobotLocomotionState == ROSController.RobotLocomotionState.STOPPED)
                    {
                        WaypointController.Instance.ClickedWaypointMarker(marker);
                    }
                }
                else
                {
                    WaypointController.Instance.ClickedWaypointMarker(marker);
                }
            }
        }
    }

    private void StartDrag(DragObject target)
    {
        _isDraggingObject = true;
        _dragObject = target;
        target.StartDrag();
        _dragOffset = Camera.main.WorldToScreenPoint(_dragObject.transform.position).z;
    }

    private void StopDrag()
    {
        _dragObject.StopDrag();
        _isDraggingObject = false;
        _dragObject = null;
    }

    public void FocusCameraOn(Transform target)
    {
        Vector3 cameraPoint = transform.position + _camera.transform.forward * _cameraFocusDistance;
        Vector3 offset = target.position - cameraPoint;
        transform.position += offset;

        //Vector3 pos = target.position - _camera.transform.forward * target.position.y;
        //transform.position = new Vector3(pos.x, transform.position.y, pos.z);
    }
}