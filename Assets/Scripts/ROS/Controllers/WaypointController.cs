using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WaypointController : MonoBehaviour {

	public enum WaypointMode { Single, Route }
    public static WaypointController Instance { get; private set; }

    [SerializeField] private GameObject _waypointMarkerPrefab;

    [Header("Rendering")]
    [SerializeField] private Color32 _singleWaypointColor;
    [SerializeField] private Color32 _waypointRouteColor;
    [SerializeField] private float _lineYOffset = 0.4f;

    private WaypointMode _currentWaypointMode = WaypointMode.Single;
    private List<WaypointMarker> _waypointMarkers;
    private LineRenderer _lineRenderer;
    private readonly Dictionary<string, List<GeoPointWGS84>> _savedRoutes = new Dictionary<string, List<GeoPointWGS84>>();

    void Awake()
    {
        Instance = this;
        _waypointMarkers = new List<WaypointMarker>();
        _lineRenderer = GetComponent<LineRenderer>();
    }

    void Start()
    {
        LoadInPathsFromConfig();
    }

    private void LoadInPathsFromConfig()
    {
        List<ConfigFile.WaypointRoute> jsonRoutes = ConfigManager.ConfigFile.Routes;
        foreach (ConfigFile.WaypointRoute route in jsonRoutes)
        {
            _savedRoutes.Add(route.Name, route.Points);
        }
    }

    public void CreateWaypoint(Vector3 waypointPosition)
    {
        if (_currentWaypointMode == WaypointMode.Single)
        {
            ClearAllWaypoints();
        }
        else
        {
            foreach (WaypointMarker marker in _waypointMarkers)
                marker.SetLock(true);
            _lineRenderer.positionCount++;
            _lineRenderer.SetPosition(_lineRenderer.positionCount-1, waypointPosition + new Vector3(0, _lineYOffset, 0));
        }

        WaypointMarker waypoint = Instantiate(_waypointMarkerPrefab, waypointPosition, Quaternion.identity).GetComponent<WaypointMarker>();
        waypoint.SetColour(_currentWaypointMode == WaypointMode.Single ? _singleWaypointColor : _waypointRouteColor);
        _waypointMarkers.Add(waypoint);
    }

    public void ToggleWaypointMode()
    {
        _currentWaypointMode = _currentWaypointMode == WaypointMode.Single ? WaypointMode.Route : WaypointMode.Single;
        if (_currentWaypointMode == WaypointMode.Route)
        {
            if (_waypointMarkers.Count > 0)
            {
                _waypointMarkers[0].SetColour(_waypointRouteColor);
                _lineRenderer.positionCount = 1;
                _lineRenderer.SetPosition(0, _waypointMarkers[0].transform.position);
            }
        }
    }

    public void DeleteMarker(WaypointMarker toDelete)
    {
        if (_lineRenderer.positionCount > 0)
            _lineRenderer.positionCount--;
        Destroy(_waypointMarkers[_waypointMarkers.Count - 1].gameObject);
        _waypointMarkers.RemoveAt(_waypointMarkers.Count-1);
    }

    public List<Vector3> GetPath()
    {
        return _waypointMarkers.Select(marker => marker.transform.position).ToList();
    }

    public void ClearAllWaypoints() {
        foreach (WaypointMarker marker in _waypointMarkers)
            Destroy(marker.gameObject);
        _waypointMarkers = new List<WaypointMarker>();
        _lineRenderer.positionCount = 0;
    }

    public void LoadRoute(string routeName)
    {
        if (!MazeMapController.Instance.CampusLoaded) return;
        if (_savedRoutes.ContainsKey(routeName))
        {
            ClearAllWaypoints();
            List<GeoPointWGS84> routePoints = _savedRoutes[routeName];
            foreach (GeoPointWGS84 point in routePoints)
            {
                CreateWaypoint(point.ToUTM().ToUnity());
            }
        }
    }

    public void SaveRoute(string routeName)
    {
        if (_waypointMarkers.Count == 0) return;
        List<GeoPointWGS84> route = GetPath().Select(point => point.ToUTM().ToWGS84()).ToList();
        if (_savedRoutes.ContainsKey(routeName))
            _savedRoutes[routeName] = route;
        else
        {
            _savedRoutes.Add(routeName, route);
        }
        ConfigManager.SaveRoute(routeName, route);
    }
}
