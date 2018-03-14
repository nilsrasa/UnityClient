using UnityEngine;

public class MazemapScreen : GazeObject
{

    [SerializeField] private Camera _mazemapCamera;

    private GazeObject _hoveredGazeObject;

    public void OnHover(RaycastHit hit)
    {
        base.OnHover();
        Ray ray = _mazemapCamera.ViewportPointToRay(hit.textureCoord);
        Debug.DrawRay(ray.origin, ray.direction * 1000, Color.red);
        RaycastHit mazeHit;
        if (Physics.Raycast(ray, out mazeHit))
        {
            GazeObject gazeObject = mazeHit.collider.GetComponent<GazeObject>();
            if (gazeObject == null)
            {
                ResetHoveredObject();
                return;
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
        if (_hoveredGazeObject != null)
        {
            _hoveredGazeObject = null;
            if (RobotMasterController.SelectedRobot != null)
            {
                RobotMasterController.SelectedRobot.StopRobot();
            }
        }
    }
}
