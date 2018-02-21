using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Messages;
using Messages.fiducial_msgs;
using Ros_CSharp;
using UnityEngine;

public class FiducialController : MonoBehaviour
{
    public static FiducialController Instance { get; private set; }

    private const string FIDUCIAL_RESOURCE_NAME = "FiducialMarker";
    private readonly Dictionary<int, FiducialObject> _fiducialObjects = new Dictionary<int, FiducialObject>();

    [SerializeField] private float _publishInterval = 3;
    [SerializeField] private bool _publishInGps = true;

    private Dictionary<int, Transform> _fiducials = new Dictionary<int, Transform>();
    private string _fiducialSavePath;
    private Fiducial _zeroLocation;
    private ROSFiducialMap _rosFiducialMapSubscriber;
    private ROSFiducialMap _rosFiducialMapPublisher;
    private ROSFiducialMapGps _rosFiducialMapGpsPublisher;
    private FiducialCollectionFile _fiducialCollectionFile;
    private bool _hasDataToConsume;
    private FiducialMapEntryArray _dataToConsume;
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

    private void OnReceivedFiducialData(ROSAgent sender, IRosMessage mapArray)
    {
        FiducialMapEntryArray fids = (FiducialMapEntryArray)mapArray;
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

    private void HandleFiducialData(FiducialMapEntryArray data)
    {
        if (!MazeMapController.Instance.CampusLoaded)
            return;
        if (!_initialised)
            Initialise();
        lock (_fiducials) {
            foreach (FiducialMapEntry entry in data.fiducials) {
                Transform fiducialTransform;
                if (_fiducials.TryGetValue(entry.fiducial_id, out fiducialTransform))
                {
                    if (entry.fiducial_id != _zeroLocation.FiducialId) {
                        Vector3 localpos = new Vector3((float)entry.x, (float)entry.z, (float)entry.y);
                        fiducialTransform.localPosition = localpos;
                        fiducialTransform.localEulerAngles = new Vector3((float)entry.rx * Mathf.Rad2Deg, (float)entry.rz * Mathf.Rad2Deg, (float)entry.ry * Mathf.Rad2Deg);
                    }
                }
                else
                {
                    GameObject newFiducial = InstantiateFiducial(entry);

                    FiducialObject.FiducialData orgData = new FiducialObject.FiducialData {
                        X = entry.x,
                        Y = entry.y,
                        Z = entry.z,
                        RX = entry.rx,
                        RY = entry.ry,
                        RZ = entry.rz
                    };

                    _fiducialObjects.Add(entry.fiducial_id, new FiducialObject {
                        Id = entry.fiducial_id,
                        Position = newFiducial.transform.position.ToUTM().ToWGS84(),
                        Rotation = newFiducial.transform.eulerAngles,
                        FiducialSpaceData = orgData
                    });
                }
            }

        }
    }

    private GameObject InstantiateFiducial(FiducialMapEntry entry)
    {
        GameObject newFiducial = Instantiate(Resources.Load(FIDUCIAL_RESOURCE_NAME), _fiducials[_zeroLocation.FiducialId]) as GameObject;
        newFiducial.transform.localPosition = new Vector3((float)entry.x, 0, (float)entry.y);
        newFiducial.transform.localEulerAngles = new Vector3((float)entry.rx * Mathf.Rad2Deg, (float)entry.rz * Mathf.Rad2Deg, (float)entry.ry * Mathf.Rad2Deg);
        newFiducial.name = entry.fiducial_id.ToString();
        newFiducial.GetComponentInChildren<TextMesh>().text = entry.fiducial_id.ToString();
        _fiducials.Add(entry.fiducial_id, newFiducial.transform);

        return newFiducial;
    }

    private void UpdateFiducial(FiducialMapEntry entry)
    {
        Vector3 localpos = new Vector3((float)entry.x, (float)entry.z, (float)entry.y);
        _fiducials[entry.fiducial_id].localPosition = _fiducials[_zeroLocation.FiducialId].rotation * localpos;
        _fiducials[entry.fiducial_id].localEulerAngles = new Vector3((float)entry.rx * Mathf.Rad2Deg, (float)entry.rz * Mathf.Rad2Deg, (float)entry.ry * Mathf.Rad2Deg);
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
        while (!ROS.isStarted()) yield return new WaitForEndOfFrame();
        _rosFiducialMapSubscriber = new ROSFiducialMap();
        _rosFiducialMapSubscriber.StartAgent(ROSAgent.AgentJob.Subscriber);
        _rosFiducialMapSubscriber.DataWasReceived += OnReceivedFiducialData;
        _rosFiducialMapPublisher = new ROSFiducialMap();
        _rosFiducialMapPublisher.StartAgent(ROSAgent.AgentJob.Publisher);
        _rosFiducialMapGpsPublisher = new ROSFiducialMapGps();
        _rosFiducialMapGpsPublisher.StartAgent(ROSAgent.AgentJob.Publisher);
        _fiducialPublishingRoutine = StartCoroutine(PublishFiducialsLoop(_publishInGps, _publishInterval));
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
                        InstantiateFiducial(new FiducialMapEntry
                        {
                            fiducial_id = fiducialObject.Id,
                            x = fiducialObject.FiducialSpaceData.X, y = fiducialObject.FiducialSpaceData.Y, z = fiducialObject.FiducialSpaceData.Z,
                            rx = fiducialObject.FiducialSpaceData.RX, ry = fiducialObject.FiducialSpaceData.RY, rz = fiducialObject.FiducialSpaceData.RZ,
                            
                        });
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
        FiducialMapEntryArray mapEntryArray = new FiducialMapEntryArray();
        FiducialMapEntry[] mapEntries = new FiducialMapEntry[_fiducialObjects.Count];
        for (int i = 0; i < _fiducialObjects.Count; i++)
        {
            FiducialObject f = _fiducialObjects.ElementAt(i).Value;
            if (inGps)
            {
                mapEntries[i] = new FiducialMapEntry
                {
                    fiducial_id = f.Id,
                    x = f.Position.latitude, y = f.Position.longitude, z = f.Position.altitude,
                };
            }
            else
            {
                mapEntries[i] = new FiducialMapEntry
                {
                    fiducial_id = f.Id,
                    x = f.FiducialSpaceData.X, y = f.FiducialSpaceData.Y, z = f.FiducialSpaceData.Z,
                };
            }
            mapEntries[i].rx = f.FiducialSpaceData.RX;
            mapEntries[i].ry = f.FiducialSpaceData.RY;
            mapEntries[i].rz = f.FiducialSpaceData.RZ;
        }
        mapEntryArray.fiducials = mapEntries;
        if (inGps)
            _rosFiducialMapGpsPublisher.PublishData(mapEntryArray);
        else
            _rosFiducialMapPublisher.PublishData(mapEntryArray);
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