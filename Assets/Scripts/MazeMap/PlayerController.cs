using System;
using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _mouseMovementSpeed = 5;
    [SerializeField] private float _mouseScrollSpeed = 10;
    [SerializeField] private float _mouseClickSpeed = 0.2f;
    [SerializeField] private Transform _floor;
     
    private Camera _camera;
    private int _currentActiveLevel = 0;
    private Vector3 _currentSelectedWaypoint;
    private Coroutine _mouseClickCheck;
    private bool _isMouseClick;

    void Awake()
    {
        _camera = GetComponentInChildren<Camera>();
    }

	void Update () {
        //Move camera
	    if (Input.GetMouseButton(1))
	    {
            transform.Translate(_mouseMovementSpeed * -Input.GetAxis("Mouse X"), 
                0, 
                _mouseMovementSpeed * -Input.GetAxis("Mouse Y"));
	    }
        if (Input.GetMouseButton(0))
        {
            _camera.transform.rotation = Quaternion.Euler(
                _camera.transform.eulerAngles.x + _mouseMovementSpeed * -Input.GetAxis("Mouse Y"),
                _camera.transform.eulerAngles.y + 0,
                _camera.transform.eulerAngles.z + 0);
            transform.eulerAngles = new Vector3(transform.eulerAngles.x,
                transform.eulerAngles.y + _mouseMovementSpeed * Input.GetAxis("Mouse X"), 
                transform.eulerAngles.z);
        }
	    if (Input.GetAxis("Mouse ScrollWheel") != 0)
	    {
	        transform.Translate(_camera.transform.forward * Input.GetAxis("Mouse ScrollWheel") * _mouseScrollSpeed);
	        int level = GetCurrentActiveLevel();
	        if (level != _currentActiveLevel)
	        {
	            _currentActiveLevel = level;
                MazeMapController.Instance.SetActiveLayer(level);
                _floor.position = new Vector3(_floor.position.x, level * MazeMapController.Instance.FloorHeight, _floor.position.z);
	        }
	    }

        _floor.position = new Vector3(transform.position.x, _floor.position.y, transform.position.z);
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
}
