using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DemoTool : MonoBehaviour
{

    [SerializeField] private int _campusId;
    [SerializeField] private string _robotName;
    [SerializeField] private GeoPointWGS84 _robotStartPoint;
    [SerializeField] private GeoPointWGS84 _robotOrientationPoint;
    [SerializeField] private List<ConfigFile.WaypointRoute> _availableWaypoints;
    [SerializeField] private GameObject _waypointPrefab;
    [SerializeField] private GeoPointWGS84 _cameraPosition;
    [SerializeField] private Vector3 _cameraRotation;

    void Start()
    {
        MazeMapController.Instance.OnFinishedGeneratingCampus += OnFinishedGeneratingCampus;
        MazeMapController.Instance.GenerateCampus(_campusId);
        PlayerController.Instance.enabled = false;
    }

    private void OnFinishedGeneratingCampus(int campusId)
    {   
        PlayerUIController.Instance.SetUIState(PlayerUIController.UIState.Hidden);
        Transform robot = RobotMasterController.Instance.LoadRobot(_robotName).transform;
        //StartCoroutine(OverrideRobotPosition(_robotStartPoint, _robotOrientationPoint));
        //RobotMasterController.SelectedRobot.OverridePositionAndOrientation(_robotStartPoint.ToUTM().ToUnity(),
            //Quaternion.LookRotation(_robotOrientationPoint.ToUTM().ToUnity() - _robotStartPoint.ToUTM().ToUnity(), Vector3.up));
        PlayerController.Instance.FocusCameraOn2D(_cameraPosition.ToUTM().ToUnity(), _cameraRotation);

        foreach (ConfigFile.WaypointRoute route in _availableWaypoints)
        {
            GeoPointWGS84 goal = route.Points[route.Points.Count - 1];
            GameObject marker = Instantiate(_waypointPrefab, goal.ToUTM().ToUnity(), Quaternion.identity);
            marker.GetComponent<GazeWaypointMarker>().Route = route;
        }
    }

    private IEnumerator OverrideRobotPosition(GeoPointWGS84 startPoint, GeoPointWGS84 orientationPoint)
    {
        yield return new WaitForSeconds(2);
        RobotMasterController.SelectedRobot.MoveToPoint(startPoint);
        yield return new WaitForSeconds(2);
        RobotMasterController.SelectedRobot.MoveToPoint(orientationPoint);
        yield return new WaitForSeconds(0.05f);
        RobotMasterController.SelectedRobot.StopRobot();
    }

}
