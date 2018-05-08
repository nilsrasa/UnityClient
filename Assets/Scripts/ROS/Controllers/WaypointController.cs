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
    private readonly Dictionary<string, List<GeoPointWGS84>> _savedRoutes = new Dictionary<string, List<GeoPointWGS84>>();
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
            _savedRoutes.Add(route.Name, route.Points);
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

        route.ForEach(CreateWaypoint);
    }

    public void CreateWaypoint(Vector3 waypointPosition)
    {
        foreach (WaypointMarker marker in _waypointMarkers)
            marker.SetLock(true);
        _lineRendererRoute.positionCount++;
        _lineRendererRoute.SetPosition(_lineRendererRoute.positionCount - 1, waypointPosition);

        WaypointMarker waypoint = Instantiate(_waypointMarkerPrefab, waypointPosition, Quaternion.identity).GetComponent<WaypointMarker>();
        ThresholdZone zone = _thresholdZoneDict[_defaultZoneType];
        waypoint.SetThresholdZone(zone.ThresholdZoneType, zone.Threshold, zone.ZoneColor);
        _waypointMarkers.Add(waypoint);
        PlayerUIController.Instance.UpdateUI(RobotMasterController.SelectedRobot);
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

    public List<Vector3> GetPath()
    {
        return _waypointMarkers.Select(marker => marker.transform.position).ToList();
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

    public void ClickedWaypointMarker(WaypointMarker marker)
    {
        int currentType = (int)marker.ThresholdZoneType;
        currentType++;
        if (currentType > 3)
            currentType = 0;
        ThresholdZone newZone = _thresholdZoneDict[(ThresholdZoneType) currentType];
        marker.SetThresholdZone(newZone.ThresholdZoneType, newZone.Threshold, newZone.ZoneColor);
    }

    [Serializable]
    private struct ThresholdZone
    {
        public ThresholdZoneType ThresholdZoneType;
        public float Threshold;
        public Color ZoneColor;
    }
}