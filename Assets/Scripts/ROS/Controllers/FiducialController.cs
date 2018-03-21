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
    private readonly Dictionary<int, FiducialObject> _fiducialObjects = new Dictionary<int, FiducialObject>();

    [SerializeField] private float _publishInterval = 3;
    [SerializeField] private bool _publishInGps = true;

    private ROSBridgeWebSocketConnection _rosConnection;
    private Dictionary<int, Transform> _fiducials = new Dictionary<int, Transform>();
    private string _fiducialSavePath;
    private Fiducial _zeroLocation;
    private ROSGenericPublisher _rosFiducialMapGpsPublisher;
    private ROSGenericSubscriber<FiducialMapEntryArrayMsg> _rosFiducialMapGpsSubscriber;
    private FiducialCollectionFile _fiducialCollectionFile;
    private bool _hasDataToConsume;
    private FiducialMapEntryArrayMsg _dataToConsume;
    private bool _initialised;
    private Coroutine _fiducialPublishingRoutine;

    private int _tempFiducialId;
    private Transform _tempFiducial;
    private Transform _fiducialToUpdate;
    private List<int> _fiducialsToDelete;

    void Awake()
    {
        Instance = this;
        _fiducialSavePath = Application.persistentDataPath + "Fiducials.json";
    }

    void Start()
    {
        _zeroLocation = ConfigManager.ConfigFile.ZeroFiducial;
        StartCoroutine(StartROSComponents());
        MazeMapController.Instance.OnFinishedGeneratingCampus += LoadFiducials;
    }

    void Update()
    {
        if (_hasDataToConsume)
        {
            HandleFiducialData(_dataToConsume);
            _hasDataToConsume = false;
        }
    }

    void OnApplicationQuit()
    {
        SaveFiducials();
    }

    private void OnReceivedFiducialData(ROSBridgeMsg mapArray)
    {
        FiducialMapEntryArrayMsg fids = (FiducialMapEntryArrayMsg)mapArray;
        _dataToConsume = fids;
        _hasDataToConsume = true;
    }

    private void Initialise()
    {
        _fiducials = new Dictionary<int, Transform>();
        GameObject newFiducial = Instantiate(Resources.Load(FIDUCIAL_RESOURCE_NAME), _zeroLocation.Position.ToUTM().ToUnity(), 
            Quaternion.Euler(_zeroLocation.Rotation)) as GameObject;

        newFiducial.name = _zeroLocation.FiducialId.ToString();
        newFiducial.GetComponentInChildren<TextMesh>().text = _zeroLocation.FiducialId.ToString();
        _fiducials.Add(_zeroLocation.FiducialId, newFiducial.transform);

        FiducialObject.FiducialData orgData = new FiducialObject.FiducialData
        {
            X = 0, Y = 0, Z = 0,
            RX = 0, RY = 0, RZ = 0
        };

        _fiducialObjects.Add(_zeroLocation.FiducialId, new FiducialObject
        {
            Id = _zeroLocation.FiducialId,
            Position = newFiducial.transform.position.ToUTM().ToWGS84(),
            Rotation = newFiducial.transform.eulerAngles,
            FiducialSpaceData = orgData
        });

        _initialised = true;
    }

    private void HandleFiducialData(FiducialMapEntryArrayMsg data)
    {
        if (!MazeMapController.Instance.CampusLoaded)
            return;
        if (!_initialised)
            Initialise();
        lock (_fiducials) {
            foreach (FiducialMapEntryMsg entry in data._fiducials) {
                Transform fiducialTransform;
                if (_fiducials.TryGetValue(entry._fiducial_id, out fiducialTransform))
                {
                    if (entry._fiducial_id != _zeroLocation.FiducialId) {
                        Vector3 localpos = new Vector3((float)entry._x, (float)entry._z, (float)entry._y);
                        fiducialTransform.localPosition = localpos;
                        fiducialTransform.localEulerAngles = new Vector3((float)entry._rx * Mathf.Rad2Deg, (float)entry._rz * Mathf.Rad2Deg, (float)entry._ry * Mathf.Rad2Deg);
                    }
                }
                else
                {
                    GameObject newFiducial = InstantiateFiducial(entry);

                    FiducialObject.FiducialData orgData = new FiducialObject.FiducialData {
                        X = entry._x,
                        Y = entry._y,
                        Z = entry._z,
                        RX = entry._rx,
                        RY = entry._ry,
                        RZ = entry._rz
                    };

                    _fiducialObjects.Add(entry._fiducial_id, new FiducialObject {
                        Id = entry._fiducial_id,
                        Position = newFiducial.transform.position.ToUTM().ToWGS84(),
                        Rotation = newFiducial.transform.eulerAngles,
                        FiducialSpaceData = orgData
                    });
                }
            }

        }
    }

    private GameObject InstantiateFiducial(FiducialMapEntryMsg entry)
    {
        GameObject newFiducial = Instantiate(Resources.Load(FIDUCIAL_RESOURCE_NAME), _fiducials[_zeroLocation.FiducialId]) as GameObject;
        newFiducial.transform.localPosition = new Vector3((float)entry._x, 0, (float)entry._y);
        newFiducial.transform.localEulerAngles = new Vector3((float)entry._rx * Mathf.Rad2Deg, (float)entry._rz * Mathf.Rad2Deg, (float)entry._ry * Mathf.Rad2Deg);
        newFiducial.name = entry._fiducial_id.ToString();
        newFiducial.GetComponentInChildren<TextMesh>().text = entry._fiducial_id.ToString();
        _fiducials.Add(entry._fiducial_id, newFiducial.transform);

        return newFiducial;
    }

    private void UpdateFiducial(FiducialMapEntryMsg entry)
    {
        Vector3 localpos = new Vector3((float)entry._x, (float)entry._z, (float)entry._y);
        _fiducials[entry._fiducial_id].localPosition = _fiducials[_zeroLocation.FiducialId].rotation * localpos;
        _fiducials[entry._fiducial_id].localEulerAngles = new Vector3((float)entry._rx * Mathf.Rad2Deg, (float)entry._rz * Mathf.Rad2Deg, (float)entry._ry * Mathf.Rad2Deg);
    }

    private void SaveFiducials()
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

    private IEnumerator StartROSComponents()
    {
        //TODO: Detect when necessary ROS connection is up
        while (_rosConnection == null) yield return new WaitForSecondsRealtime(0.3f);
        _rosFiducialMapGpsPublisher = new ROSGenericPublisher(_rosConnection, "fiducial_map_GPS", FiducialMapEntryArrayMsg.GetMessageType());
        _fiducialPublishingRoutine = StartCoroutine(PublishFiducialsLoop(_publishInGps, _publishInterval));

        _rosFiducialMapGpsSubscriber = new ROSGenericSubscriber<FiducialMapEntryArrayMsg>(_rosConnection, "/fiducial_map_gps", FiducialMapEntryArrayMsg.GetMessageType(), (msg) => new FiducialMapEntryArrayMsg(msg));
        _rosFiducialMapGpsSubscriber.OnDataReceived += OnReceivedFiducialData;
    }

    private IEnumerator PublishFiducialsLoop(bool inGps, float intervalInSeconds) {
        while (true) {
            PublishFiducials(inGps);
            yield return new WaitForSeconds(intervalInSeconds);
        }
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
            foreach (FiducialCollection collection in _fiducialCollectionFile.FiducialCollections) {
                if (campusId == collection.CampusId)
                {
                    foreach (FiducialObject fiducialObject in collection.SavedFiducials)
                    {
                        if (fiducialObject.Id == _zeroLocation.FiducialId) continue;
                        //TODO: Use once markers are place correctly

                        FiducialMapEntryMsg msg = new FiducialMapEntryMsg(fiducialObject.Id, fiducialObject.FiducialSpaceData.X, fiducialObject.FiducialSpaceData.Y, fiducialObject.FiducialSpaceData.Z,
                            fiducialObject.FiducialSpaceData.RX, fiducialObject.FiducialSpaceData.RY, fiducialObject.FiducialSpaceData.RZ);
                        InstantiateFiducial(msg);
                        _fiducialObjects.Add(fiducialObject.Id, fiducialObject);
                    }
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Publishes known fiducials to corresponding topic
    /// </summary>
    /// <param name="inGps">true: Publishes fiducials in WGS84 coordinates.
    /// false: Publishes fiducials in fiducial space.</param>
    private void PublishFiducials(bool inGps)
    {
        FiducialMapEntryMsg[] mapEntries = new FiducialMapEntryMsg[_fiducialObjects.Count];
        for (int i = 0; i < _fiducialObjects.Count; i++)
        {
            FiducialObject f = _fiducialObjects.ElementAt(i).Value;
            if (inGps)
            {
                mapEntries[i] = new FiducialMapEntryMsg(f.Id, f.Position.latitude, f.Position.longitude, f.Position.altitude, f.FiducialSpaceData.RX, f.FiducialSpaceData.RY, f.FiducialSpaceData.RZ);
            }
            else
            {
                mapEntries[i] = new FiducialMapEntryMsg(f.Id, f.FiducialSpaceData.X, f.FiducialSpaceData.Y, f.FiducialSpaceData.Z, f.FiducialSpaceData.RX, f.FiducialSpaceData.RY, f.FiducialSpaceData.RZ);
            }
        }
        FiducialMapEntryArrayMsg mapEntryArray = new FiducialMapEntryArrayMsg(mapEntries);

        _rosFiducialMapGpsPublisher.PublishData(mapEntryArray);
    }

    public void Initialise(ROSBridgeWebSocketConnection rosConnection)
    {
        _rosConnection = rosConnection;
        Initialise();
    }

    public int PlaceOrUpdateNewFiducial(int id, Vector3 position, Vector3 rotation)
    {
        if (_fiducialObjects.ContainsKey(id))
            _tempFiducialId = GetNewFiducialId();
        _tempFiducialId = id;

        if (_tempFiducial == null)
        {
            _tempFiducial = (Instantiate(Resources.Load(FIDUCIAL_RESOURCE_NAME), _fiducials[_zeroLocation.FiducialId]) as GameObject).transform;
            _tempFiducial.SetPositionAndRotation(position, Quaternion.Euler(rotation));
        }
        else
        {
            _tempFiducial.SetPositionAndRotation(position, Quaternion.Euler(rotation));

        }
        _tempFiducial.GetComponentInChildren<TextMesh>().text = _tempFiducialId.ToString();
        return _tempFiducialId;
    }

    public void FinalizeNewFiducialPlacement()
    {
        _tempFiducial.name = _tempFiducialId.ToString();
        _fiducials.Add(_tempFiducialId, _tempFiducial);

        FiducialObject.FiducialData orgData = new FiducialObject.FiducialData
        {
            X = _tempFiducial.localPosition.x,
            Y = _tempFiducial.localPosition.z,
            Z = _tempFiducial.localPosition.y,
            RX = _tempFiducial.localEulerAngles.x,
            RY = _tempFiducial.localEulerAngles.z,
            RZ = _tempFiducial.localEulerAngles.y,
        };

        _fiducialObjects.Add(_tempFiducialId, new FiducialObject
        {
            Id = _tempFiducialId,
            Position = _tempFiducial.position.ToUTM().ToWGS84(),
            Rotation = _tempFiducial.eulerAngles,
            FiducialSpaceData = orgData
        });

        _tempFiducial = null;
    }

    public void CancelNewFiducialPlacement()
    {
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

    public void SetFiducialColliders(bool isActive)
    {
        foreach (KeyValuePair<int, Transform> fiducial in _fiducials)
        {
            fiducial.Value.GetComponent<SphereCollider>().enabled = isActive;
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
        foreach (int id in _fiducialsToDelete)
        {
            Destroy(_fiducials[id].gameObject);
            _fiducials.Remove(id);
            _fiducialObjects.Remove(id);
        }
    }

    public void CancelDelete()
    {
        foreach (int id in _fiducialsToDelete)
        {
            _fiducials[id].gameObject.SetActive(true);
        }
        _fiducialsToDelete = null;
    }

    public int StartUpdateFiducial(Transform fiducial)
    {
        _tempFiducial = Instantiate(fiducial.gameObject).transform;
        _tempFiducial.SetPositionAndRotation(fiducial.position, fiducial.rotation);
        _tempFiducial.name = fiducial.name;
        _tempFiducialId = int.Parse(fiducial.gameObject.name);
        _fiducialToUpdate = fiducial;
        _fiducialToUpdate.gameObject.SetActive(false);
        return _tempFiducialId;
    }

    public void UpdateFiducial(int id, Vector3 newPosition, Vector3 newRotation)
    {
        _tempFiducial.position = newPosition;
        _tempFiducial.eulerAngles = newRotation;
    }

    public void FinalizeUpdate()
    {
        _fiducialToUpdate.SetPositionAndRotation(_tempFiducial.position, Quaternion.Euler(_tempFiducial.eulerAngles));
        
        _fiducialObjects[_tempFiducialId].FiducialSpaceData = new FiducialObject.FiducialData
        {
            X = _fiducialToUpdate.localPosition.x,
            Y = _fiducialToUpdate.localPosition.z,
            Z = _fiducialToUpdate.localPosition.y,
            RX = _fiducialToUpdate.localEulerAngles.x,
            RY = _fiducialToUpdate.localEulerAngles.z,
            RZ = _fiducialToUpdate.localEulerAngles.y,
        };

        _fiducialObjects[_tempFiducialId].Position = _fiducialToUpdate.position.ToUTM().ToWGS84();
        _fiducialObjects[_tempFiducialId].Rotation = _fiducialToUpdate.eulerAngles;

        Destroy(_tempFiducial.gameObject);
        _tempFiducial = null;
    }

    public void CancelUpdate()
    {
        Destroy(_tempFiducial.gameObject);
        _tempFiducial = null;
        _fiducialToUpdate.gameObject.SetActive(true);
    }

}

[Serializable]
public class Fiducial {
    public int FiducialId;
    public GeoPointWGS84 Position;
    public Vector3 Rotation;
}