using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Assets.Scripts;
using UnityEngine;

public class MazeMapController : MonoBehaviour
{
    [Header("Building Generation")]
    public float FloorHeight = 5;
    public int CurrentActiveLevel { get; private set; }
    [SerializeField] private float _lineWidth = 0.1f;
    [SerializeField] private Material _lineMaterial;

    [Header("Data Collection")]
    [SerializeField] private bool _useMazemapMercator;
    [SerializeField] private List<int> _poiIds;

    [Space(5)]
    [SerializeField] private Transform _triggerFloor;

    public static MazeMapController Instance;
    //Key: BuildingId, Value: Building data
    public Dictionary<int, Building> Buildings;

    private const string REST_BUILDING_SEARCH = "https://api.mazemap.com/api/buildings/?campusid={0}&srid=4326";
    private const string REST_FLOOROUTLINES = "https://api.mazemap.com/api/flooroutlines/?campusid={0}&srid=4326";
    private const string REST_FLOOROUTLINE = "https://api.mazemap.com/api/flooroutlines/?floorid={0}&srid=4326";
    private const string REST_POI_SEARCH = "http://api.mazemap.com/api/pois/{0}/?srid=4326";
    private const string REST_POI_SEARCH_BY_FLOORID = "https://api.mazemap.com/api/pois/?floorid={0}&srid=4326";
    //Key: Floor Z, Value: Floor Transform
    private Dictionary<int, List<Floor>> _floorsByZ;
    //Key: Floor Z, Value: If layer is currently shown
    private Dictionary<int, bool> _zLayerActive;

    private bool _mercatorOriginSet;
    private int _buildingsLeftToDraw;

    private int BuildingsLeftToDraw
    {
        get { return _buildingsLeftToDraw; }
        set
        {
            _buildingsLeftToDraw = value;
            PlayerUIController.Instance.UpdateLoadingProgress(1 - ((float)_buildingsLeftToDraw / Buildings.Count));
        }
    }

    void Awake()
    {
        Instance = this;
        Buildings = new Dictionary<int, Building>();
        _floorsByZ = new Dictionary<int, List<Floor>>();
        _zLayerActive = new Dictionary<int, bool>();
        CurrentActiveLevel = 2;
    }

    void Update ()
    {
        //Debug Keys
        if (Input.GetKeyDown(KeyCode.F))
            foreach (int point in _poiIds)
                StartCoroutine(DrawPoiOutline(point));
        if (Input.GetKeyDown(KeyCode.L))
            DrawAllRooms();
    }

    /// <summary>
    /// Coroutine to get data from REST API
    /// </summary>
    /// <param name="url">REST Endpoint</param>
    /// <param name="whenDone">Which function to run with returned JSON data</param>
    private IEnumerator GetWWW(string url, Action<string> whenDone)
    {
        WWW www = new WWW(url);
        yield return www;
        whenDone?.Invoke(www.text);
    }

    /// <summary>
    /// Parses GeoJSON into building objects and draws them
    /// </summary>
    /// <param name="text">GeoJSON string</param>
    private void GetBuildings(string text)
    {
        List<Building> buildings = new List<Building>();
        JSONObject collection = new JSONObject(text);
        foreach (JSONObject building in collection["buildings"])
        {
            Building b = new Building
            {
                Name = building["buildingNames"][0]["name"].str,
                Id = (int) building["buildingNames"][0]["id"].f,
                BuildingId = (int) building["buildingNames"][0]["buildingId"].f,
                AccessLevel = (int) building["accessLevel"].f,
                CampusId = (int) building["campusId"].f
            };

            foreach (JSONObject floor in building["floors"])
            {
                Floor f = new Floor
                {
                    Id = (int) floor["id"].f,
                    Name = floor["name"].str,
                    AccessLevel = (int) floor["accessLevel"].f,
                    Z = (int) floor["z"].f,
                    FloorOutlineId = (int)floor["floorOutlineId"].f,
            };
                if (f.Z > 0)
                    f.Z--;
                b.Floors.Add(f.Id, f);
            }
            buildings.Add(b);
            Buildings.Add(b.Id, b);
        }

        StartCoroutine(DrawAllBuildings());
    }

    private IEnumerator DrawAllBuildings()
    {
        BuildingsLeftToDraw = Buildings.Count;
        foreach (KeyValuePair<int, Building> pair in Buildings)
        {
            StartCoroutine(DrawBuilding(pair.Value));
        }
        while (BuildingsLeftToDraw > 0)
        {
            yield return new WaitForEndOfFrame();
        }
        SetActiveLayer(CurrentActiveLevel);
    }

    /// <summary>
    /// Draws floor outlines of building
    /// </summary>
    private IEnumerator DrawBuilding(Building building)
    {
        GameObject buildingObject = new GameObject(building.Name);
        building.RenderedModel = buildingObject.transform;
        List<Transform> floorObjects = new List<Transform>();

        foreach (KeyValuePair<int, Floor> pair in building.Floors)
        {
            Floor floor = pair.Value;
            WWW www = new WWW(string.Format(REST_FLOOROUTLINE, floor.Id));
            yield return www;
            GameObject floorObject = DrawFloorOutlines(www.text);
            floorObject.SetActive(false);
            floorObject.name = floor.Name;
            floorObjects.Add(floorObject.transform);
            floorObject.transform.position = floorObject.transform.position + new Vector3(0, floor.Z * FloorHeight, 0);
            floor.RenderedModel = floorObject.transform;

            if (!_floorsByZ.ContainsKey(floor.Z))
            {
                _floorsByZ.Add(floor.Z, new List<Floor>());
                _zLayerActive.Add(floor.Z, false);
            }
            _floorsByZ[floor.Z].Add(floor);
        }   

        foreach (Transform t in floorObjects)
            t.SetParent(buildingObject.transform, true);

        RenderBuilding(buildingObject);
        CenterParentOnChildren(buildingObject.transform);
        StartCoroutine(DrawRoomsInBuilding(building));
        BuildingsLeftToDraw--;
    }

    /// <summary>
    /// Parses GeoJSON and creates floor outline points
    /// </summary>
    /// <returns>GameObject that contains floor</returns>
    private GameObject DrawFloorOutlines(string text)
    {
        JSONObject collection = new JSONObject(text);
        GameObject floor = new GameObject();
        List<Transform> points = new List<Transform>();

        foreach (JSONObject feature in collection["features"]) 
        {
            floor.name = feature["properties"]["floorIds"][0].f.ToString();
            JSONObject geometry = feature["geometry"];
            foreach (JSONObject pos in geometry["coordinates"]) 
            {
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
                        
                        foreach (Transform t in mpPoints)
                            t.SetParent(multiPolygon.transform, true);

                        points.Add(multiPolygon.transform);
                    }
                    else
                    {
                        GameObject pointObject = InstantiatePoint(coord, _useMazemapMercator);
                        pointObject.transform.SetParent(floor.transform, true);
                        points.Add(pointObject.transform);
                    }
                }
            }
        }

        foreach (Transform t in points)
            t.SetParent(floor.transform, true);

        return floor;
    }
    
    /// <summary>
    /// Instantiates point object from WGS84 or Mercator coordinates, by converting them to UCS
    /// </summary>
    /// <param name="isMercator">Is coordinate in Mercator, otherwise WGS84</param>
    /// <returns>Point</returns>
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

    /// <summary>
    /// Creates outline of building using LineRenderers
    /// </summary>
    /// <param name="building">Building GameObject</param>
    private void RenderBuilding(GameObject building)
    {
        for (int floorIndex = 0; floorIndex < building.transform.childCount; floorIndex++)
        {
            Transform floor = building.transform.GetChild(floorIndex);
            List<Vector3> points = new List<Vector3>();

            for (int floorPointIndex = 0; floorPointIndex < floor.childCount; floorPointIndex++)
            {
                Transform floorPoint = floor.GetChild(floorPointIndex);
                if (floorPoint.childCount > 0) 
                {
                    LineRenderer lr = floorPoint.gameObject.AddComponent<LineRenderer>();
                    lr.positionCount = floorPoint.childCount;
                    lr.material = _lineMaterial;
                    lr.startWidth = lr.endWidth = _lineWidth;

                    for (int j = 0; j < floorPoint.childCount; j++) 
                    {
                        lr.SetPosition(j, floorPoint.GetChild(j).position);
                    }
                }
                else
                {
                    points.Add(floorPoint.position);
                }
            }
            if (points.Count > 0) 
            {
                LineRenderer lr = floor.gameObject.AddComponent<LineRenderer>();
                lr.positionCount = points.Count;
                lr.material = _lineMaterial;
                lr.startWidth = lr.endWidth = _lineWidth;
                lr.SetPositions(points.ToArray());
            }
        }
    }

    /// <summary>
    /// Creates outline of POI using LineRenderers
    /// </summary>
    /// <param name="poi">GameObject containing POI</param>
    private void RenderPoiOutline(GameObject poi)
    {
        foreach (Transform pointContainer in poi.transform)
        {
            LineRenderer lineRenderer = pointContainer.gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = _lineMaterial;
            lineRenderer.startWidth = lineRenderer.endWidth = _lineWidth;
            lineRenderer.positionCount = pointContainer.childCount;
            int i = 0;
            foreach (Transform point in pointContainer)
            {
                lineRenderer.SetPosition(i, point.position);
                i++;
            }
        }
    }

    private void DrawAllRooms()
    {
        foreach (Building building in Buildings.Values)
        {
            StartCoroutine(DrawRoomsInBuilding(building));
        }
    }

    public IEnumerator DrawRoomsInBuilding(Building building)
    {
        foreach (KeyValuePair<int, Floor> pair in building.Floors) {
            WWW www = new WWW(string.Format(REST_POI_SEARCH_BY_FLOORID, pair.Value.Id));
            yield return www;
            JSONObject pois = new JSONObject(www.text)["pois"];
            if (pois == null)
                continue;

            foreach (JSONObject poi in pois) {
                DrawPoiOutline(poi);
            }
        }
    }

    private IEnumerator DrawPoiOutline(int poiId)
    {
        WWW www = new WWW(string.Format(REST_POI_SEARCH, poiId));
        yield return www;
        JSONObject poi = new JSONObject(www.text);
        DrawPoiOutline(poi);
    }

    /// <summary>
    /// Creates POI object from JSONObject and makes outline
    /// </summary>
    private void DrawPoiOutline(JSONObject poi)
    {
        int z = (int)poi["z"].f;
        if (z > 0) z--;
        string roomName = poi["infos"][0]["name"].str;
        string buildingName = poi["buildingName"].str;
        int buildingId = (int)poi["buildingId"].f;
        int floorId = (int) poi["floorId"].f;
        GameObject roomObject = new GameObject("Room " + roomName);
        Room room = new Room
        {
            Id = (int) poi["poiId"].f,
            Name = roomName,
            RenderedModel = roomObject.transform
        };

        foreach (JSONObject pos in poi["geometry"]["coordinates"]) 
        {
            GameObject roomContainer = new GameObject("points");
            foreach (JSONObject coord in pos)
            {
                GameObject pointObject = InstantiatePoint(coord, _useMazemapMercator);
                pointObject.transform.position = pointObject.transform.position + new Vector3(0, z * FloorHeight, 0);
                pointObject.transform.SetParent(roomContainer.transform, true);
            }
            roomContainer.transform.SetParent(roomObject.transform, true);
        }
        roomObject.transform.SetParent(GetBuildingByName(buildingName).Floors[floorId].RenderedModel, true);
        GetBuildingByName(buildingName).Floors[floorId].Rooms.Add(room.Id, room);
        if (z != CurrentActiveLevel)
            roomObject.SetActive(false);

        RenderPoiOutline(roomObject);
        CenterParentOnChildren(roomObject.transform);
    }

    private void SetLayerVisibility(int z, bool isVisible)
    {
        foreach (Floor floor in _floorsByZ[z])
            floor.RenderedModel.gameObject.SetActive(isVisible);
        _zLayerActive[z] = isVisible;
    }

    private void SetFloorRoomsVisibility(int z, bool isVisible)
    {
        foreach (Floor floor in _floorsByZ[z])    {
            foreach (KeyValuePair<int, Room> pair in floor.Rooms)
            {
                pair.Value.RenderedModel.gameObject.SetActive(isVisible);
            }
        }
    }

    /// <summary>
    /// Recursively center parents on children (bottom up)
    /// </summary>
    /// <param name="parent">Root transform</param>
    private void CenterParentOnChildren(Transform parent)
    {
        Vector3 avrg = Vector3.zero;
        int childcount = parent.childCount;
        List<Transform> children = new List<Transform>();
        for (int i = 0; i < childcount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.childCount > 0)
            {
                CenterParentOnChildren(child);
            }
            children.Add(child);
            avrg += child.position;
        }

        foreach (Transform c in children)
            c.SetParent(null, true);

        avrg /= childcount;
        parent.position = avrg;

        foreach (Transform c in children)
            c.SetParent(parent, true);
    }

    public void SetActiveLayer(int z)
    {
        if (!_floorsByZ.ContainsKey(z))
            return;
        SetFloorRoomsVisibility(CurrentActiveLevel, false);
        CurrentActiveLevel = z;
        SetFloorRoomsVisibility(CurrentActiveLevel, true);

        //Set all layers above active layer to not render
        for (int i = z + 1; _floorsByZ.ContainsKey(i); i++)
        {
            if (_zLayerActive[i])
                SetLayerVisibility(i, false);
        }
        
        //Set all layers below active layer to render
        for (int i = z; _floorsByZ.ContainsKey(i); i--)
        {
            if (!_zLayerActive[i])
                SetLayerVisibility(i, true);
        }
        _triggerFloor.position = new Vector3(_triggerFloor.position.x, CurrentActiveLevel * FloorHeight, _triggerFloor.position.z);

    }

    public void ChangeActiveLayer(bool up)
    {
        if (up)
            SetActiveLayer(CurrentActiveLevel + 1);
        else
            SetActiveLayer(CurrentActiveLevel - 1);
    }

    public void GenerateCampus(int id)
    {
        StartCoroutine(GetWWW(string.Format(REST_BUILDING_SEARCH, id), GetBuildings));
    }

    public Building GetBuildingByName(string buildingName) 
    {
        foreach (KeyValuePair<int, Building> pair in Buildings)
            if (pair.Value.Name == buildingName)
                return pair.Value;
        return null;
    }

}
