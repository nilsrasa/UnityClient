using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ROSBridgeLib;
using ROSBridgeLib.fiducial_msgs;
using UnityEngine;

public class FiducialController : MonoBehaviour
{
    public static FiducialController Instance { get; private set; }

    private const string FIDUCIAL_RESOURCE_NAME = "FiducialMarker";

    [SerializeField] private float _publishInterval = 3;

    private ROSBridgeWebSocketConnection _rosBridge;
    private ROSGenericPublisher _rosFiducialMapPublisher;
    private Dictionary<int, FiducialObject> _fiducialObjects = new Dictionary<int, FiducialObject>();
    private Dictionary<int, Fiducial> _fiducials = new Dictionary<int, Fiducial>();
    private string _fiducialSavePath;
    private FiducialData _zeroLocation;
    private FiducialCollectionFile _fiducialCollectionFile;
    private bool _initialised;

    private int _tempFiducialId;
    private Fiducial _tempFiducial;
    private Transform _fiducialToUpdate;
    private List<int> _fiducialsToDelete;
    private Coroutine _publishLoop;

    void Awake()
    {
        Instance = this;
        _fiducialSavePath = Application.persistentDataPath + "Fiducials.json";
    }

    void Start()
    {
        _zeroLocation = ConfigManager.ConfigFile.ZeroFiducial;
        MazeMapController.Instance.OnFinishedGeneratingCampus += LoadFiducials;
    }

    private void Initialise()
    {
        _fiducials = new Dictionary<int, Fiducial>();
        _fiducialObjects = new Dictionary<int, FiducialObject>();
        Fiducial newFiducial = (Instantiate(Resources.Load(FIDUCIAL_RESOURCE_NAME), _zeroLocation.Position.ToUTM().ToUnity(),
            Quaternion.Euler(_zeroLocation.Rotation)) as GameObject).GetComponent<Fiducial>();

        newFiducial.gameObject.name = _zeroLocation.FiducialId.ToString();

        newFiducial.FiducialId = _zeroLocation.FiducialId;
        _fiducials.Add(_zeroLocation.FiducialId, newFiducial);

        _fiducialObjects.Add(_zeroLocation.FiducialId, new FiducialObject
        {
            Id = _zeroLocation.FiducialId,
            Position = newFiducial.transform.position.ToUTM().ToWGS84(),
            Rotation = newFiducial.transform.eulerAngles.ToGeoRotation()
        });
        float heightAboveFloor = MazeMapController.Instance.GetHeightAboveFloor(newFiducial.transform.position.y);
        newFiducial.InitaliseFloorMarker(heightAboveFloor);
        _initialised = true;
    }

    private Fiducial InstantiateFiducial(FiducialObject fiducial)
    {
        Fiducial newFiducial = (Instantiate(Resources.Load(FIDUCIAL_RESOURCE_NAME), _fiducials[_zeroLocation.FiducialId].transform) as GameObject).GetComponent<Fiducial>();
        newFiducial.transform.position = fiducial.Position.ToUTM().ToUnity();
        newFiducial.transform.eulerAngles = fiducial.Rotation.ToUnity();
        newFiducial.name = fiducial.Id.ToString();
        newFiducial.FiducialId = fiducial.Id;
        _fiducials.Add(fiducial.Id, newFiducial);

        float heightAboveFloor = MazeMapController.Instance.GetHeightAboveFloor(newFiducial.transform.position.y);
        newFiducial.InitaliseFloorMarker(heightAboveFloor);
        return newFiducial;
    }

    private void UpdateFiducial(FiducialMapEntryMsg entry)
    {
        Vector3 localpos = new Vector3((float) entry._x, (float) entry._z, (float) entry._y);
        _fiducials[entry._fiducial_id].transform.localPosition = _fiducials[_zeroLocation.FiducialId].transform.rotation * localpos;
        _fiducials[entry._fiducial_id].transform.localEulerAngles = new Vector3((float) entry._rx * Mathf.Rad2Deg, (float) entry._rz * Mathf.Rad2Deg, (float) entry._ry * Mathf.Rad2Deg);
    }

    /// <summary>
    /// Serializes FiducialMap into json file and saves files to AppData/LocalLow/DTU-R3 folder.
    /// </summary>
    public void SaveFiducials()
    {
        if (_fiducialObjects.Count == 0) return;
        if (_fiducialCollectionFile == null)
            _fiducialCollectionFile = new FiducialCollectionFile();

        List<FiducialObject> fidObjects = new List<FiducialObject>(_fiducialObjects.Values.ToList());
        FiducialCollection collection = new FiducialCollection
        {
            CampusId = MazeMapController.Instance.CampusId,
            SavedFiducials = fidObjects
        };
        bool saved = false;
        if (_fiducialCollectionFile.FiducialCollections != null)
        {
            for (int i = 0; i < _fiducialCollectionFile.FiducialCollections.Count; i++)
            {
                if (_fiducialCollectionFile.FiducialCollections[i].CampusId == collection.CampusId)
                {
                    _fiducialCollectionFile.FiducialCollections[i] = collection;
                    saved = true;
                }
            }
        }
        if (!saved)
        {
            _fiducialCollectionFile.FiducialCollections = new List<FiducialCollection> {collection};
        }

        File.WriteAllText(_fiducialSavePath, JsonUtility.ToJson(_fiducialCollectionFile));
    }

    /// <summary>
    /// Loads all fiducials with a given campus id
    /// </summary>
    private void LoadFiducials(int campusId)
    {
        Initialise();
        if (!File.Exists(_fiducialSavePath)) return;
        lock (_fiducials)
        {
            _fiducialCollectionFile = JsonUtility.FromJson<FiducialCollectionFile>(File.ReadAllText(_fiducialSavePath));
            foreach (FiducialCollection collection in _fiducialCollectionFile.FiducialCollections)
            {
                if (campusId == collection.CampusId)
                {
                    foreach (FiducialObject fiducialObject in collection.SavedFiducials)
                    {
                        if (fiducialObject.Id == _zeroLocation.FiducialId) continue;

                        InstantiateFiducial(fiducialObject);
                        _fiducialObjects.Add(fiducialObject.Id, fiducialObject);
                    }
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Creates entire FiducialMapEntryArrayMsg to be published.
    /// </summary>
    private FiducialMapEntryArrayMsg GetFiducialMapForPublish()
    {
        FiducialMapEntryMsg[] mapEntries = new FiducialMapEntryMsg[_fiducialObjects.Count];
        for (int i = 0; i < _fiducialObjects.Count; i++)
        {
            FiducialObject f = _fiducialObjects.ElementAt(i).Value;
            mapEntries[i] = CreateFiducialMapEntryMessage(f.Id, f.Position, f.Rotation);
        }
        return new FiducialMapEntryArrayMsg(mapEntries);
    }

    private IEnumerator PublishLoop(float interval)
    {
        while (_rosBridge != null)
        {
            _rosFiducialMapPublisher.PublishData(GetFiducialMapForPublish());
            yield return new WaitForSeconds(interval);
        }
    }

    public int PlaceOrUpdateNewFiducial(int id, Vector3 position, Vector3 rotation)
    {
        if (_fiducialObjects.ContainsKey(id))
            _tempFiducialId = GetNewFiducialId();
        _tempFiducialId = id;

        if (_tempFiducial == null)
        {
            _tempFiducial = (Instantiate(Resources.Load(FIDUCIAL_RESOURCE_NAME), _fiducials[_zeroLocation.FiducialId].transform) as GameObject).GetComponent<Fiducial>();
            _tempFiducial.transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
        }
        else
        {
            _tempFiducial.transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
        }
        _tempFiducial.InitaliseFloorMarker(MazeMapController.Instance.GetHeightAboveFloor(position.y));
        _tempFiducial.FiducialId = _tempFiducialId;
        return _tempFiducialId;
    }

    public void FinalizeNewFiducialPlacement()
    {
        if (_tempFiducial == null) return;

        _tempFiducial.name = _tempFiducialId.ToString();
        _fiducials.Add(_tempFiducialId, _tempFiducial);


        _fiducialObjects.Add(_tempFiducialId, new FiducialObject
        {
            Id = _tempFiducialId,
            Position = _tempFiducial.transform.position.ToUTM().ToWGS84(),
            Rotation = _tempFiducial.transform.eulerAngles.ToGeoRotation()
        });

        _tempFiducial = null;
    }

    public void CancelNewFiducialPlacement()
    {
        if (_tempFiducial == null) return;
        Destroy(_tempFiducial.gameObject);
        _tempFiducial = null;
    }

    public int GetNewFiducialId()
    {
        int highestId = int.MinValue;
        foreach (KeyValuePair<int, FiducialObject> fiducial in _fiducialObjects)
        {
            if (fiducial.Key > highestId)
                highestId = fiducial.Key;
        }
        return highestId + 1;
    }

    /// <summary>
    /// Enables colliders on all fiducials for click events.
    /// </summary>
    public void SetFiducialColliders(bool isActive)
    {
        foreach (KeyValuePair<int, Fiducial> fiducial in _fiducials)
        {
            fiducial.Value.SetCollider(isActive);
        }
    }

    public void DeleteFiducial(Transform fiducial)
    {
        if (_fiducialsToDelete == null) _fiducialsToDelete = new List<int>();
        int id = int.Parse(fiducial.gameObject.name);
        _fiducialsToDelete.Add(id);
        _fiducials[id].gameObject.SetActive(false);
    }

    public void FinalizeDelete()
    {
        if (_fiducialsToDelete == null) return;

        foreach (int id in _fiducialsToDelete)
        {
            Destroy(_fiducials[id].gameObject);
            _fiducials.Remove(id);
            _fiducialObjects.Remove(id);
        }
    }

    public void CancelDelete()
    {
        if (_fiducialsToDelete == null) return;

        foreach (int id in _fiducialsToDelete)
        {
            _fiducials[id].gameObject.SetActive(true);
        }
        _fiducialsToDelete = null;
    }

    public int StartUpdateFiducial(Transform fiducial)
    {
        _tempFiducial = Instantiate(fiducial.gameObject).GetComponent<Fiducial>();
        _tempFiducial.transform.SetPositionAndRotation(fiducial.position, fiducial.rotation);
        _tempFiducial.name = fiducial.name;
        _tempFiducialId = int.Parse(fiducial.gameObject.name);
        _fiducialToUpdate = fiducial;
        _fiducialToUpdate.gameObject.SetActive(false);
        return _tempFiducialId;
    }

    public void UpdateFiducial(int id, Vector3 newPosition, Vector3 newRotation)
    {
        _tempFiducial.transform.position = newPosition;
        _tempFiducial.transform.eulerAngles = newRotation;
        _tempFiducial.FiducialId = id;
    }

    public void FinalizeUpdate()
    {
        if (_fiducialToUpdate == null) return;

        _fiducialToUpdate.SetPositionAndRotation(_tempFiducial.transform.position, Quaternion.Euler(_tempFiducial.transform.eulerAngles));

        _fiducialObjects[_tempFiducialId].Position = _fiducialToUpdate.position.ToUTM().ToWGS84();
        _fiducialObjects[_tempFiducialId].Rotation = _fiducialToUpdate.eulerAngles.ToGeoRotation();

        Destroy(_tempFiducial.gameObject);
        _fiducialToUpdate.gameObject.SetActive(true);
        _tempFiducial = null;
        _fiducialToUpdate = null;
    }

    public void CancelUpdate()
    {
        if (_fiducialToUpdate == null) return;

        Destroy(_tempFiducial.gameObject);
        _tempFiducial = null;
        _fiducialToUpdate.gameObject.SetActive(true);
        _fiducialToUpdate = null;
    }

    /// <summary>
    /// Transforms WGS84 coordinate and GeoRotation into fiducialEntryMessage.
    /// </summary>
    public FiducialMapEntryMsg CreateFiducialMapEntryMessage(int fiducialId, GeoPointWGS84 wgsPoint, GeoRotation geoRotation)
    {
        return new FiducialMapEntryMsg(fiducial_id: fiducialId, x: wgsPoint.longitude, y: wgsPoint.latitude, z: wgsPoint.altitude,
            rx: geoRotation.east, ry: geoRotation.north, rz: geoRotation.heading);
    }

    /// <summary>
    /// Register rosbridge and start publishing FiducialMapEntryArrayMsg to bridge.
    /// </summary>
    public void Register(ROSBridgeWebSocketConnection rosBridge)
    {
        if (_rosBridge != null)
        {
            Unregister(_rosBridge);
        }
        _rosBridge = rosBridge;
        _rosFiducialMapPublisher = new ROSGenericPublisher(rosBridge, "/fiducial_map_gps", FiducialMapEntryArrayMsg.GetMessageType());
        _publishLoop = StartCoroutine(PublishLoop(_publishInterval));
    }

    /// <summary>
    /// Unregister rosbridge to stop receiving FiducialMapEntryArrayMsgs on bridge.
    /// </summary>
    public void Unregister(ROSBridgeWebSocketConnection rosBridge)
    {
        if (_rosBridge != rosBridge) return;
        StopCoroutine(_publishLoop);
        _rosFiducialMapPublisher.Stop();
        _rosBridge = null;
    }

    public void SetFiducialVisibility(bool isVisible)
    {
        foreach (KeyValuePair<int, Fiducial> fiducial in _fiducials)
        {
            fiducial.Value.gameObject.SetActive(isVisible);
        }
    }
}

[Serializable]
public class FiducialData
{
    public int FiducialId;
    public GeoPointWGS84 Position;
    public Vector3 Rotation;
}