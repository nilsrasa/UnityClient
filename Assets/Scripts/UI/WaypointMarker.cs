using UnityEngine;

public class WaypointMarker : MonoBehaviour
{
    [SerializeField] private Transform _top;
    [SerializeField] private Transform _bottom;
    [SerializeField] private Transform _handle;

    [Header("Animation")] [SerializeField] private float _spinSpeed = 1;
    [SerializeField] private float _bobbingSpeed = 1;
    [SerializeField] private float _pulseSpeed = 1;
    [SerializeField] private AnimationCurve _scaleCurve;
    [SerializeField] private AnimationCurve _bobbingCurve;

    //Animation
    private float _scaleCurveIndex;
    private float _bobbingCurveIndex;
    private Vector3 _startLocation;
    private float _startCustomeZoneScale;

    private float _customZoneRadius = 0.5f;

    public WaypointController.Waypoint Waypoint;
    public bool IsLocked { get; private set; }

    private CapsuleCollider _collider;

    void Awake()
    {
        _collider = GetComponent<CapsuleCollider>();
    }

    void Start()
    {
        _startLocation = _top.localPosition;
    }

    void Update()
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

    public void SetWaypoint(WaypointController.Waypoint waypoint)
    {
        Waypoint = waypoint;
        SetThresholdZone(waypoint.ThresholdZone);
    }

    public void SetThresholdZone(WaypointController.ThresholdZone zone)
    {
        Waypoint.ThresholdZone = zone;
        _bottom.localScale = new Vector3(zone.Threshold, zone.Threshold, 1);
        _handle.gameObject.SetActive(zone.ThresholdZoneType == WaypointController.ThresholdZoneType.Custom);
        _handle.position = transform.position + Vector3.right * zone.Threshold;

        _bottom.GetComponent<SpriteRenderer>().color = zone.ZoneColor;
    }

    public void UpdateCustomScale(float newScale, Vector3 directionOfHandle)
    {
        _customZoneRadius =  newScale;
        _bottom.localScale = new Vector3(_customZoneRadius, _customZoneRadius, 1);
        if (directionOfHandle != Vector3.zero)
            _handle.position = transform.position + directionOfHandle * _customZoneRadius;
    }

    public void EndUpdateCustomScale()
    {
        Waypoint.ThresholdZone.Threshold = _customZoneRadius;
        WaypointController.Instance.UpdateMarker(this);
    }

    /// <summary>
    /// Determines whether or not this waypoint can be destroyed by clicking on it
    /// </summary>
    /// <param name="isLocked">Locked waypoints cannot be destroyed</param>
    public void SetLock(bool isLocked)
    {
        IsLocked = isLocked;
    }
}