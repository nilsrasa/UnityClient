using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _mouseMovementSpeed = 5;
    [SerializeField] private float _mouseScrollSpeed = 10;

    private Camera _camera;
    private int _currentActiveLevel = 0;

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
                _camera.transform.eulerAngles.y + _mouseMovementSpeed * Input.GetAxis("Mouse X"), 
                0);
	    }
	    if (Input.GetAxis("Mouse ScrollWheel") != 0)
	    {
	        transform.Translate(_camera.transform.forward * Input.GetAxis("Mouse ScrollWheel") * _mouseScrollSpeed);
	        int level = GetCurrentActiveLevel();
	        if (level != _currentActiveLevel)
	        {
	            _currentActiveLevel = level;
                MazeMapController.Instance.SetActiveLayer(level);
	        }
	    }
	}

    private int GetCurrentActiveLevel()
    {
        return Mathf.FloorToInt((transform.position.y - MazeMapController.Instance.FloorHeight *3) / MazeMapController.Instance.FloorHeight);
        
    }
}
