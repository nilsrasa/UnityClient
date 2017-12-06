using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assets.Scripts;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using UnityEngine;

public class MazeMapController : MonoBehaviour
{
    [Header("Building Generation")]
    [SerializeField] private float _floorHeight = 3;
    [SerializeField] private Material _lineMaterial;

    [Header("Data Collection")]
    [SerializeField] private bool _useMazemapMercator;
    [SerializeField] private List<int> _poiIds;

    public static MazeMapController Instance;

    private const string REST_FLOOROUTLINES = "https://api.mazemap.com/api/flooroutlines/?campusid={0}&srid=4326";
    private const string REST_FLOOROUTLINE = "https://api.mazemap.com/api/flooroutlines/?floorid={0}&srid=4326";
    private const string REST_FLOOROUTLINE_MERCATOR = "https://api.mazemap.com/api/flooroutlines/?floorid={0}&srid=900913";
    private const string REST_POI_SEARCH = "http://api.mazemap.com/api/pois/{0}/?srid=4326";
    private const string REST_POI_SEARCH_MERCATOR = "http://api.mazemap.com/api/pois/{0}/?srid=900913";
    //Key: FloorId, Value: Floor Geometry
    private Dictionary<int, Transform> _generatedFloors;
    //Key: BuildingId, Value: Building data
    private Dictionary<int, Building> _buildings;

    private bool _mercatorOriginSet = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _buildings = new Dictionary<int, Building>();
        _generatedFloors = new Dictionary<int, Transform>();
        
    }

    // Update is called once per frame
    void Update ()
    {
        if (Input.GetKeyDown(KeyCode.G))
            StartCoroutine(GetWWW("https://api.mazemap.com/api/buildings/?campusid=89&srid=4326", GetBuildings));
        if (Input.GetKeyDown(KeyCode.B))
            StartCoroutine(DrawBuilding(GetBuildingByName("371")));
        if (Input.GetKeyDown(KeyCode.N))
            DrawAllBuildings();
        if (Input.GetKeyDown(KeyCode.F))
            foreach (int point in _poiIds)
                StartCoroutine(DrawFloorIndoorOutline(point));
    }

    private Building GetBuildingByName(string name)
    {
        foreach (KeyValuePair<int, Building> pair in _buildings)
            if (pair.Value.Name == name)
                return pair.Value;
        return null;
    }

    private IEnumerator GetWWW(string url, Action<string> whenDone)
    {
        WWW www = new WWW(url);
        yield return www;
        if (whenDone != null)
            whenDone(www.text);
    }

    private void GetBuildings(string text)
    {
        List<Building> buildings = new List<Building>();
        JSONObject collection = new JSONObject(text);
        foreach (JSONObject building in collection["buildings"])
        {
            Building b = new Building();
            b.Name = building["buildingNames"][0]["name"].str;
            b.Id = (int)building["buildingNames"][0]["id"].f;
            b.BuildingId = (int)building["buildingNames"][0]["buildingId"].f;
            b.AccessLevel = (int) building["accessLevel"].f;
            b.CampusId = (int) building["campusId"].f;
            foreach (JSONObject floor in building["floors"])
            {
                Floor f = new Floor();
                f.Id = (int)floor["id"].f;
                f.Name = floor["name"].str;
                f.AccessLevel = (int) floor["accessLevel"].f;
                f.Z = (int) floor["z"].f;
                if (f.Z > 0) f.Z--;
                f.FloorOutlineId = (int) floor["floorOutlineId"].f;
                b.Floors.Add(f);
            }
            buildings.Add(b);
        }

        foreach (Building building in buildings)
        {
            _buildings.Add(building.Id, building);
        }
        
        Debug.Log("----Done getting buildings----");
    }

    private void DrawAllBuildings()
    {
        foreach (KeyValuePair<int, Building> pair in _buildings)
        {
            StartCoroutine(DrawBuilding(pair.Value));
        }
    }

    private IEnumerator DrawBuilding(Building building)
    {
        GameObject buildingObject = new GameObject(building.Name);
        List<Transform> floorObjects = new List<Transform>();
        foreach (Floor floor in building.Floors)
        {
            WWW www = new WWW(string.Format(_useMazemapMercator ? REST_FLOOROUTLINE_MERCATOR : REST_FLOOROUTLINE, floor.Id));
            yield return www;
            GameObject floorObject = DrawFloorOutlines(www.text);
            floorObject.name = floor.Name;
            floorObjects.Add(floorObject.transform);
            floorObject.transform.position = floorObject.transform.position + new Vector3(0, floor.Z * _floorHeight, 0);
            _generatedFloors.Add(floor.Id, floorObject.transform);
        }
        Vector3 avrg = Vector3.zero;
        foreach (Transform t in floorObjects)
            avrg += t.position;
       // buildingObject.transform.position = avrg / floorObjects.Count;
        foreach (Transform t in floorObjects)
            t.parent = buildingObject.transform;

        RenderBuilding(buildingObject);
    }

    private GameObject DrawFloorOutlines(string text)
    {
        JSONObject collection = new JSONObject(text);
        GameObject floor = new GameObject();
        List<Transform> points = new List<Transform>();
        foreach (JSONObject feature in collection["features"]) {
            floor.name = feature["properties"]["floorIds"][0].f.ToString();
            JSONObject geometry = feature["geometry"];
            foreach (JSONObject pos in geometry["coordinates"]) {
                foreach (JSONObject coord in pos)
                {
                    if (geometry["type"].str == "MultiPolygon")
                    {
                        GameObject multiPolygon = new GameObject("mp");
                        
                        List<Transform> mpPoints = new List<Transform>();
                        foreach (JSONObject multiCoord in coord)
                        {
                            GameObject pointObject = InstantiatePoint(multiCoord, _useMazemapMercator);
                            mpPoints.Add(pointObject.transform);
                        }
                        
                        Vector3 avrgPos = Vector3.zero;
                        foreach (Transform t in mpPoints)
                            avrgPos += t.position;
                       // multiPolygon.transform.position = avrgPos / mpPoints.Count;
                        foreach (Transform t in mpPoints)
                            t.parent = multiPolygon.transform;
                        points.Add(multiPolygon.transform);
                    }
                    else
                    {
                        GameObject pointObject = InstantiatePoint(coord, _useMazemapMercator);
                        pointObject.transform.parent = floor.transform;
                        points.Add(pointObject.transform);
                    }
                }
            }
            
        }

        Vector3 avrg = Vector3.zero;
        foreach (Transform t in points)
            avrg += t.position;
        //floor.transform.position = avrg / points.Count;
        foreach (Transform t in points)
            t.parent = floor.transform;
        return floor;
    }
    
    private GameObject InstantiatePoint(JSONObject coordinate, bool isMercator)
    {
        GeoPointMercator geoPointMercator;
        if (isMercator)
        {
            geoPointMercator = new GeoPointMercator {
                longitude = Double.Parse(coordinate[0].ToString(), NumberStyles.Float),
                latitude = Double.Parse(coordinate[1].ToString(), NumberStyles.Float),
            };
        }
        else
        {
            var geoPointWGS84 = new GeoPointWGS84 {
                longitude = Double.Parse(coordinate[0].ToString(), NumberStyles.Float),
                latitude = Double.Parse(coordinate[1].ToString(), NumberStyles.Float),
            };
            geoPointMercator = geoPointWGS84.ToMercator();
        }

        GameObject pointObject = new GameObject("point");
        if (!_mercatorOriginSet)
        {
            _mercatorOriginSet = true;
            GeoUtils.MercatorOrigin = geoPointMercator;
            pointObject.name = "Zero";
            Debug.Log("Zero - Mercator: " + geoPointMercator);
            Debug.Log("Zero - WGS84: " + geoPointMercator.ToWGS84());
        }

        pointObject.transform.position = geoPointMercator.ToUnity();
        return pointObject;
    }

    private void RenderBuilding(GameObject building)
    {
        for (int floorIndex = 0; floorIndex < building.transform.childCount; floorIndex++)
        {
            Transform floor = building.transform.GetChild(floorIndex);
            List<Vector3> points = new List<Vector3>();

            for (int floorPointIndex = 0; floorPointIndex < floor.childCount; floorPointIndex++)
            {
                Transform floorPoint = floor.GetChild(floorPointIndex);
                if (floorPoint.childCount > 0) {
                    LineRenderer lr = floorPoint.gameObject.AddComponent<LineRenderer>();
                    lr.positionCount = floorPoint.childCount;
                    lr.material = _lineMaterial;
                    lr.startWidth = lr.endWidth = 0.4f;
                    for (int j = 0; j < floorPoint.childCount; j++) {
                        lr.SetPosition(j, floorPoint.GetChild(j).position);
                    }
                }
                else
                {
                    points.Add(floorPoint.position);
                }
            }
            if (points.Count > 0) {
                LineRenderer lr = floor.gameObject.AddComponent<LineRenderer>();
                lr.positionCount = points.Count;
                lr.material = _lineMaterial;
                lr.startWidth = lr.endWidth = 0.4f;
                lr.SetPositions(points.ToArray());
            }
        }
    }

    private void RenderFloorIndoorOutline(GameObject floor)
    {
        LineRenderer lineRenderer = floor.AddComponent<LineRenderer>();
        lineRenderer.material = _lineMaterial;
        lineRenderer.startWidth = lineRenderer.endWidth = 0.3f;
        lineRenderer.positionCount = floor.transform.childCount;
        int i = 0;
        foreach (Transform t in floor.transform)
        {
            lineRenderer.SetPosition(i, t.position);
            i++;
        }

    }

    private IEnumerator DrawFloorIndoorOutline(int poi)
    {
        WWW www = new WWW(string.Format(_useMazemapMercator ? REST_POI_SEARCH_MERCATOR : REST_POI_SEARCH, poi));
        yield return www;
        JSONObject feature = new JSONObject(www.text);
        foreach (JSONObject pos in feature["geometry"]["coordinates"]) {
            GameObject container = new GameObject("floor");
            //container.transform.position = parent.position;
            //container.transform.parent = parent;
            foreach (JSONObject coord in pos)
            {
                GameObject pointObject = InstantiatePoint(coord, _useMazemapMercator);
                pointObject.transform.parent = container.transform;
            }

            RenderFloorIndoorOutline(container);
        }
    }
}
