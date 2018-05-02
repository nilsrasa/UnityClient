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
    private Vector3 _currentSelectedWaypoint;
    private Coroutine _mouseClickCheck;
    private bool _isMouseClick;
    private MouseObject _hoveredMouseObject;

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

        //Check if left mouse button was clicked or held
        if (Input.GetMouseButtonDown(0))
        {
            _mouseClickCheck = StartCoroutine(CheckForClick(_mouseClickSpeed));
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (_isMouseClick)
                MouseClicked();
            _isMouseClick = false;
            if (_mouseClickCheck != null)
                StopCoroutine(_mouseClickCheck);
        }

        if (Input.GetMouseButton(0) && !_isMouseClick)
        {
            _camera.transform.rotation = Quaternion.Euler(
                _camera.transform.eulerAngles.x + _mouseMovementSpeed * -Input.GetAxis("Mouse Y"),
                _camera.transform.eulerAngles.y + 0,
                _camera.transform.eulerAngles.z + 0);
            transform.eulerAngles = new Vector3(transform.eulerAngles.x,
                transform.eulerAngles.y + _mouseMovementSpeed * Input.GetAxis("Mouse X"),
                transform.eulerAngles.z);
        }

        if (Input.GetMouseButton(1))
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

    private IEnumerator CheckForClick(float time)
    {
        float timer = 0;
        _isMouseClick = true;
        while (timer < time)
        {
            yield return new WaitForEndOfFrame();
            timer += Time.deltaTime;
        }
        _isMouseClick = false;
    }

    private void MouseClicked()
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
                WaypointController.Instance.DeleteMarker(marker);
                return;
            }
            if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Floor")) return;

            if (RobotMasterController.SelectedRobot != null)
                WaypointController.Instance.CreateWaypoint(hit.point);
        }
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