using System.Diagnostics;
using System.Linq;
using UnityEngine;

public class GazeWaypointMarker : GazeObject
{
    [SerializeField] private Transform _ring;
    [SerializeField] private Transform _progressMask;

    [Header("Animation")]
    [SerializeField] private float _spinSpeed = 1;
    [SerializeField] private float _progressMaskStartScale = 0.5f;
    [SerializeField] private float _progressMaskEndScale = 1;

    public ConfigFile.WaypointRoute Route;

    protected override void Awake()
    {
        base.Awake();
    }

	protected override void Update ()
	{
        base.Update();

	    if (IsActivated)
	    {
	        _progressMask.localScale = new Vector3(_progressMaskEndScale, _progressMaskEndScale, _progressMaskEndScale);
	        _ring.Rotate(transform.forward, _spinSpeed * Time.deltaTime);
        }
        else
	    {
	        float dwellProgress = (_progressMaskStartScale + (_progressMaskEndScale - _progressMaskStartScale)) * _dwellTimer / _dwellTime;
	        _progressMask.localScale = new Vector3(dwellProgress, dwellProgress, dwellProgress);
	        float currentSpinSpeed = _spinSpeed * Time.deltaTime * dwellProgress;
	        _ring.Rotate(transform.forward, currentSpinSpeed);
        }
    }

    protected override void Activate()
    {
        base.Activate();
        WaypointController.Instance.ClearAllWaypoints();
        WaypointController.Instance.CreateRoute(Route.Points.Select(point => point.ToUTM().ToUnity()).ToList());
        RobotMasterController.SelectedRobot.MovePath(Route.Points);
    }
}
