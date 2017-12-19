using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts;
using UnityEngine;

public class WaypointController : MonoBehaviour {

	public enum WaypointMode { Single, Route }

    [SerializeField] private GameObject _waypointMarkerPrefab;

    [Header("Rendering")]
    [SerializeField] private Color32 _singleWaypointColor;
    [SerializeField] private Color32 _waypointRouteColor;
    [SerializeField] private float _lineYOffset = 0.4f;

    private WaypointMode _currentWaypointMode = WaypointMode.Single;
    private List<WaypointMarker> _waypointMarkers;
    private LineRenderer _lineRenderer;

    //Testing
    [Header("Testing Parameters")]
    [SerializeField] private string _saveRouteName = "route";
    [SerializeField] private bool _saveRoute;
    [SerializeField] private bool _loadRoute;
    private string _routePath;

    void Awake()
    {
        _routePath = Application.persistentDataPath + "/routes";
        if (!Directory.Exists(_routePath))
            Directory.CreateDirectory(_routePath);
        _waypointMarkers = new List<WaypointMarker>();
        _lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (_saveRoute)
        {
            _saveRoute = false;

            string route = "";
            foreach (WaypointMarker marker in _waypointMarkers)
            {
                GeoPointWGS84 point = marker.transform.position.ToMercator().ToWGS84();
                route += string.Format("{0}/{1}/{2}&", point.longitude, marker.transform.position.y, point.latitude);
            }
            StreamWriter writer = new StreamWriter(_routePath+"/"+_saveRouteName+".txt", false);
            writer.WriteLine(route);
            writer.Close();

        }
        if (_loadRoute)
        {
            _loadRoute = false;
            StreamReader reader;
            try {
                reader = new StreamReader(_routePath + "/" + _saveRouteName + ".txt");
            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e);
                return;
            }

            if (_currentWaypointMode != WaypointMode.Route) ToggleWaypointMode();
            string[] points = reader.ReadToEnd().Split('&');
            foreach (string point in points) {

                string[] axis = point.Split('/');
                if (axis.Length <= 1)
                    continue;
                GeoPointWGS84 coord = new GeoPointWGS84
                {
                    longitude = double.Parse(axis[0]),
                    altitude = double.Parse(axis[1]),
                    latitude = double.Parse(axis[2]),
                };
                Vector3 v = coord.ToMercator().ToUnity();
                v += new Vector3(0, (float)coord.altitude, 0);
                CreateWaypoint(v);
            }
            
            reader.Close();
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
}
