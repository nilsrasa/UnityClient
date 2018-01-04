using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; set; }

    [SerializeField] private float _mouseMovementSpeed = 5;
    [SerializeField] private float _mouseScrollSpeed = 10;
    [SerializeField] private float _mouseClickSpeed = 0.2f;
    [SerializeField] private Transform _triggerFloor;

    public WaypointController WaypointController { get; private set; }

    private Camera _camera;
    private Vector3 _currentSelectedWaypoint;
    private Coroutine _mouseClickCheck;
    private bool _isMouseClick;

    void Awake()
    {
        _camera = GetComponentInChildren<Camera>();
        WaypointController = GetComponent<WaypointController>();
        Instance = this;
    }

	void Update () {
        //Input events

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
            StopCoroutine(_mouseClickCheck);
	    }

	    if (Input.GetMouseButton(0) && !_isMouseClick) {
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
	        transform.Translate(_camera.transform.forward * Input.GetAxis("Mouse ScrollWheel") * _mouseScrollSpeed);
	        int level = GetCurrentActiveLevel();
	        if (level != MazeMapController.Instance.CurrentActiveLevel)
	        {
                MazeMapController.Instance.SetActiveLayer(level);
	        }
	    }

        _triggerFloor.position = new Vector3(transform.position.x, _triggerFloor.position.y, transform.position.z);

    }

    private int GetCurrentActiveLevel()
    {
        return Mathf.FloorToInt((transform.position.y - MazeMapController.Instance.FloorHeight *3) / MazeMapController.Instance.FloorHeight);
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

        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000))
        {
            WaypointMarker marker = hit.collider.GetComponent<WaypointMarker>();
            if (marker != null)
            {
                WaypointController.DeleteMarker(marker);
                return;
            }
            if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Floor")) return;

            WaypointController.CreateWaypoint(hit.point);
        }
    }

    private void ToggleWaypointModeButtonClicked()
    {
        WaypointController.ToggleWaypointMode();
    }

    private void StartPathButtonClicked()
    {
        List<Vector3> pathInUnity = WaypointController.GetPath();
        List<GeoPointWGS84> pathInWgs84 = pathInUnity.Select(point => point.ToMercator().ToWGS84()).ToList();
        ArlobotROSController.Instance.MovePath(pathInWgs84);
    }

    public void FocusCameraOn(Transform target)
    {
        Vector3 pos = target.position - _camera.transform.forward * target.position.y;
        transform.position = new Vector3(pos.x, transform.position.y, pos.z);
    }

}
