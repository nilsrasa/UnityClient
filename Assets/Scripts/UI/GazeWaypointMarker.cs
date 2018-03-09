using System.Diagnostics;
using UnityEngine;

public class GazeWaypointMarker : GazeObject
{
    [SerializeField] private Transform _ring;
    [SerializeField] private Transform _progressMask;

    [Header("Animation")]
    [SerializeField] private float _spinSpeed = 1;
    [SerializeField] private float _progressMaskStartScale = 0.5f;
    [SerializeField] private float _progressMaskEndScale = 1;

    protected override void Awake()
    {
        base.Awake();
    }

	protected override void Update ()
	{
        base.Update();

	    float dwellProgress = _progressMaskStartScale + (_progressMaskEndScale - _progressMaskStartScale) * (_dwellTimer / _dwellTime);
        _progressMask.localScale = new Vector3(dwellProgress, dwellProgress, dwellProgress);
	    float currentSpinSpeed = _spinSpeed * Time.deltaTime * dwellProgress;
	    _ring.Rotate(transform.forward, currentSpinSpeed);

    }

}
