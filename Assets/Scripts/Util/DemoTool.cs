using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoTool : MonoBehaviour
{

    [SerializeField] private int _campusId;
    [SerializeField] private string _robotName;
    [SerializeField] private GeoPointWGS84 _robotStartPoint;
    [SerializeField] private GeoPointWGS84 _robotOrientationPoint;
    [SerializeField] private List<GeoPointWGS84> _availableWaypoints;
    [SerializeField] private GameObject _waypointPrefab;
    
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
        PlayerController.Instance.FocusCameraOn2D(robot);
        StartCoroutine(OverrideRobotPosition(_robotStartPoint, _robotOrientationPoint));
        foreach (GeoPointWGS84 point in _availableWaypoints)
        {
            Instantiate(_waypointPrefab, point.ToUTM().ToUnity(), Quaternion.identity);
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
