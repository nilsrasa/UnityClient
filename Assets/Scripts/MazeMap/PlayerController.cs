using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _mouseMovementSpeed = 1;
    [SerializeField] private float _mouseScrollSpeed = 1;

    private Camera _camera;

    void Awake()
    {
        _camera = GetComponentInChildren<Camera>();
    }

	void Update () {
        //Move camera
	    if (Input.GetMouseButton(1))
	    {
            transform.Translate(_mouseMovementSpeed * -Input.GetAxis("Mouse X"), 0, _mouseMovementSpeed * -Input.GetAxis("Mouse Y"));
	    }
        if (Input.GetMouseButton(0))
        {
            transform.rotation = Quaternion.Euler(
                transform.eulerAngles.x + _mouseMovementSpeed * -Input.GetAxis("Mouse Y"), 
                transform.eulerAngles.y + _mouseMovementSpeed * Input.GetAxis("Mouse X"), 0);
	       // transform.Rotate(_mouseMovementSpeed * -Input.GetAxis("Mouse Y"), _mouseMovementSpeed * Input.GetAxis("Mouse X"), 0, Space.Self);
	    }
	    if (Input.GetAxis("Mouse ScrollWheel") != 0)
	    {
	        _camera.transform.Translate(_camera.transform.forward * Input.GetAxis("Mouse ScrollWheel") * _mouseScrollSpeed);
	    }
	}
}
