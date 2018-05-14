using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WaypointController : MonoBehaviour
{
    public static WaypointController Instance { get; private set; }

    [SerializeField] private GameObject _waypointMarkerPrefab;
    [SerializeField] private ThresholdZoneType _defaultZoneType = ThresholdZoneType.Precise;
    [SerializeField] private ThresholdZone[] _thresholdZones;

    public enum ThresholdZoneType
    {
        Precise = 0,
        Medium = 1, 
        Fast = 2,
        Custom = 3
    }

    private List<WaypointMarker> _waypointMarkers;
    private LineRenderer _lineRendererRoute;
    private LineRenderer _lineRendererRobot;
    private readonly Dictionary<string, List<Waypoint>> _savedRoutes = new Dictionary<string, List<Waypoint>>();
    private readonly Dictionary<ThresholdZoneType, ThresholdZone> _thresholdZoneDict = new Dictionary<ThresholdZoneType, ThresholdZone>();

    void Awake()
    {
        Instance = this;
        _waypointMarkers = new List<WaypointMarker>();
        _lineRendererRoute = GetComponent<LineRenderer>();
        _lineRendererRobot = transform.GetChild(0).GetComponent<LineRenderer>();

        foreach (ThresholdZone zone in _thresholdZones)
        {
            _thresholdZoneDict.Add(zone.ThresholdZoneType, zone);
        }
    }

    void Start()
    {
        LoadInPathsFromConfig();
    }

    void Update()
    {
        if (_waypointMarkers.Count <= 0)
        {
            _lineRendererRobot.positionCount = 0;
        }
        else
        {
            _lineRendererRobot.positionCount = 2;
            _lineRendererRobot.SetPositions(new [] {RobotMasterController.SelectedRobot.transform.position, _waypointMarkers[0].transform.position});
        }
    }

    private void LoadInPathsFromConfig()
    {
        List<ConfigFile.WaypointRoute> jsonRoutes = ConfigManager.ConfigFile.Routes;
        foreach (ConfigFile.WaypointRoute route in jsonRoutes)
        {
            _savedRoutes.Add(route.Name, route.Waypoints);
        }
    }

    public void CreateRoute(List<Waypoint> route)
    {
        ClearAllWaypoints();

        foreach (Waypoint waypoint in route)
        {
            WaypointMarker marker = CreateWaypoint(waypoint.Point.ToUTM().ToUnity());
            marker.SetWaypoint(waypoint);
        }
    }

    public void CreateRoute(List<GeoPointWGS84> route)
    {
        if (route.Count <= 0) return;
        List<Vector3> routeTransformed = route.Select(point => point.ToUTM().ToUnity()).ToList();
        CreateRoute(routeTransformed);
    }

    public void CreateRoute(List<Vector3> route)
    {
        ClearAllWaypoints();

        foreach (Vector3 point in route)
        {
            CreateWaypoint(point);
        }
    }

    public WaypointMarker CreateWaypoint(Vector3 waypointPosition)
    {
        foreach (WaypointMarker marker in _waypointMarkers)
            marker.SetLock(true);
        _lineRendererRoute.positionCount++;
        _lineRendererRoute.SetPosition(_lineRendererRoute.positionCount - 1, waypointPosition);

        WaypointMarker waypoint = Instantiate(_waypointMarkerPrefab, waypointPosition, Quaternion.identity).GetComponent<WaypointMarker>();
        ThresholdZone zone = _thresholdZoneDict[_defaultZoneType];
        waypoint.SetWaypoint(new Waypoint{Point = waypointPosition.ToUTM().ToWGS84(), ThresholdZone = zone});
        _waypointMarkers.Add(waypoint);
        PlayerUIController.Instance.UpdateUI(RobotMasterController.SelectedRobot);
        return waypoint;
    }

    public void DeleteMarker(WaypointMarker toDelete)
    {
        if (toDelete.IsLocked) return;
        if (_lineRendererRoute.positionCount > 0)
            _lineRendererRoute.positionCount--;
        Destroy(_waypointMarkers[_waypointMarkers.Count - 1].gameObject);
        _waypointMarkers.RemoveAt(_waypointMarkers.Count - 1);
        if (_waypointMarkers.Count == 0)
            PlayerUIController.Instance.UpdateUI(RobotMasterController.SelectedRobot);
        else if (_waypointMarkers.Count > 0)
            _waypointMarkers[_waypointMarkers.Count-1].SetLock(false);
    }

    public List<Waypoint> GetPath()
    {
        return _waypointMarkers.Select(marker => marker.Waypoint).ToList();
    }

    public void ClearAllWaypoints()
    {
        if (_waypointMarkers.Count == 0) return;
        foreach (WaypointMarker marker in _waypointMarkers)
            Destroy(marker.gameObject);
        _waypointMarkers = new List<WaypointMarker>();
        _lineRendererRoute.positionCount = 0;
        if (_waypointMarkers.Count == 0) PlayerUIController.Instance.UpdateUI(RobotMasterController.SelectedRobot);
    }

    public void LoadRoute(string routeName)
    {
        if (!MazeMapController.Instance.CampusLoaded) return;
        if (_savedRoutes.ContainsKey(routeName))
        {
            CreateRoute(_savedRoutes[routeName]);
        }
    }

    public void SaveRoute(string routeName)
    {
        if (_waypointMarkers.Count == 0) return;
        if (_savedRoutes.ContainsKey(routeName))
            _savedRoutes[routeName] = GetPath();
        else
        {
            _savedRoutes.Add(routeName, GetPath());
        }
        ConfigManager.SaveRoute(routeName, GetPath());
    }

    public void ClickedWaypointMarker(WaypointMarker marker)
    {
        int currentType = (int)marker.Waypoint.ThresholdZone.ThresholdZoneType;
        currentType++;
        if (currentType > 3)
            currentType = 0;
        ThresholdZone newZone = _thresholdZoneDict[(ThresholdZoneType) currentType];
        marker.SetThresholdZone(newZone);
        UpdateMarker(marker);
    }

    public void UpdateMarker(WaypointMarker marker)
    {
        _waypointMarkers[_waypointMarkers.IndexOf(marker)].Waypoint = marker.Waypoint;
    }

    [Serializable]
    public struct ThresholdZone
    {
        public ThresholdZoneType ThresholdZoneType;
        public float Threshold;
        public Color ZoneColor;
    }

    [Serializable]
    public struct Waypoint
    {
        public GeoPointWGS84 Point;
        public ThresholdZone ThresholdZone;
    }
}