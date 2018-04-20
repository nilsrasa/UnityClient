using UnityEngine;

public class WaypointMarker : MonoBehaviour
{
    [SerializeField] private Transform _top;
    [SerializeField] private Transform _bottom;

    [Header("Animation")]
    [SerializeField] private float _spinSpeed = 1;
    [SerializeField] private float _bobbingSpeed = 1;
    [SerializeField] private float _pulseSpeed = 1;
    [SerializeField] private AnimationCurve _scaleCurve;
    [SerializeField] private AnimationCurve _bobbingCurve;

    //Animation
    private float _scaleCurveIndex;
    private float _bobbingCurveIndex;
    private Vector3 _startLocation;

    private CapsuleCollider _collider;

    void Awake()
    {
        _collider = GetComponent<CapsuleCollider>();
    }

    void Start()
    {
        _startLocation = _top.localPosition;
    }

	void Update ()
	{
	    _scaleCurveIndex += Time.deltaTime * _pulseSpeed;
	    _bobbingCurveIndex += Time.deltaTime * _bobbingSpeed;
	    if (_scaleCurveIndex > 1) _scaleCurveIndex -= 1;
	    if (_bobbingCurveIndex > 1) _bobbingCurveIndex -= 1;

	    _top.Rotate(transform.forward, _spinSpeed * Time.deltaTime);
        _top.localScale = new Vector3(_scaleCurve.Evaluate(_scaleCurveIndex), _scaleCurve.Evaluate(_scaleCurveIndex), _scaleCurve.Evaluate(_scaleCurveIndex));
        _top.localPosition = _startLocation + new Vector3(0, _bobbingCurve.Evaluate(_bobbingCurveIndex), 0);
	}

    public void SetColour(Color32 color)
    {
        _bottom.GetComponent<SpriteRenderer>().color = color;
        _top.GetComponent<SpriteRenderer>().color = color;
    }

    /// <summary>
    /// Determines whether or not this waypoint can be destroyed by clicking on it
    /// </summary>
    /// <param name="isLocked">Locked waypoints cannot be destroyed</param>
    public void SetLock(bool isLocked)
    {
        _collider.enabled = !isLocked;
    }

}
