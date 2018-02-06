using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

public class SorroundPhotoController : MonoBehaviour
{
    public enum Camera
    {
        Cam0, Cam1, Cam2, Cam3, Cam4, Cam5, Cam6, Cam7, Cam8, Cam9, Cam10, Cam11,
    }

    public static SorroundPhotoController Instance { get; private set; }

    [SerializeField] private List<CameraMapping> _cameraMappings;
    [SerializeField] private GameObject _sorroundPhotoLocationPrefab;

    private readonly Dictionary<Camera, MeshRenderer> _cameraPlanes = new Dictionary<Camera, MeshRenderer>();

    private string _cameraFolderPath;
    private UnityEngine.Camera _camera;
    private bool _active;
    private Dictionary<int, SorroundPhotoLocation> _photoLocations;
    private List<RectTransform> _timeSliderPoints;
    private SorroundPhotoLocation _currentLoadedPhotoLocation;

    void Awake()
    {
        Instance = this;
        _cameraFolderPath = Application.streamingAssetsPath + "/AbsoluteZero";
        _camera = GetComponentInChildren<UnityEngine.Camera>();
        foreach (CameraMapping mapping in _cameraMappings)
        {
            _cameraPlanes.Add(mapping.Camera, mapping.Plane);
        }
    }

    void Start()
    {
        MazeMapController.Instance.OnFinishedGeneratingCampus += InitialisePhotoLocations;
    }

    void Update()
    {
        if (!_active) return;
        _camera.transform.localEulerAngles += new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0);
    }

    private IEnumerator RenderPhoto(string path, MeshRenderer target)
    {
        Texture2D tex = new Texture2D(4, 4, TextureFormat.DXT1, false);
        using (WWW www = new WWW(@"file:///" +path)) 
        {
            yield return www;
            www.LoadImageIntoTexture(tex);
            target.material.mainTexture = tex;
        }
    }

    public void InitialisePhotoLocations(int campusId)
    {
        _photoLocations = new Dictionary<int, SorroundPhotoLocation>();
        string path = $"{_cameraFolderPath}/{campusId}/";
        if (!Directory.Exists(path)) return;

        string[] directories = Directory.GetDirectories(path);

        foreach (string directory in directories)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(directory);
            string[] split = dirInfo.Name.Split('_');

            int id = int.Parse(split[0]); 
            DateTime timestamp = DateTime.Parse(split[1]);

            SorroundPictureMeta metaData = JsonUtility.FromJson<SorroundPictureMeta>(File.ReadAllText(directory+"/meta.json"));

            if (!_photoLocations.ContainsKey(id))
            {
                GameObject photoLocationObject = Instantiate(_sorroundPhotoLocationPrefab,
                    metaData.GpsCoordinate.ToUTM().ToUnity(), Quaternion.Euler(metaData.Orientation));
                photoLocationObject.name = "SorroundPhoto_" + id;
                photoLocationObject.transform.SetParent(transform, true);

                SorroundPhotoLocation sorroundPhotoLocation = photoLocationObject.GetComponent<SorroundPhotoLocation>();
                sorroundPhotoLocation.PictureId = id;
                sorroundPhotoLocation.OnClick += LoadPhoto;
                _photoLocations.Add(id, sorroundPhotoLocation);
            }

            _photoLocations[id].Timestamps.Add(timestamp);
            _photoLocations[id].Timestamps = _photoLocations[id].Timestamps.OrderBy(time => time).ToList();
            
        }
    }

    public void LoadPhoto(SorroundPhotoLocation photoLocation)
    {
        //TODO: Load path from database?
        _active = true;
        _currentLoadedPhotoLocation = photoLocation;
        PlayerUIController.Instance.PhotoClicked();
        PlayerController.Instance.CurrentPlayerState = PlayerController.PlayerState.SorroundViewing;
        _camera.enabled = true;
        DateTime firstTimestamp = photoLocation.Timestamps[0];
        string sId = photoLocation.PictureId.ToString("D4", CultureInfo.InvariantCulture);
        string imagePath = $"{_cameraFolderPath}/{MazeMapController.Instance.CampusId}/{sId}_{firstTimestamp.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)}";
        foreach (KeyValuePair<Camera, MeshRenderer> camera in _cameraPlanes)
        {
            string path = $"{imagePath}/{camera.Key}/IMAG{sId}.JPG";
            StartCoroutine(RenderPhoto(path, camera.Value));
        }
        
        //Load history
        if (_photoLocations[photoLocation.PictureId].Timestamps.Count > 2)
        {
            PlayerUIController.Instance.ResetTimeSlider();
            _timeSliderPoints = new List<RectTransform>();
            PlayerUIController.Instance.SetSliderVisibility(true);
            DateTime lastTimestamp = _photoLocations[photoLocation.PictureId].Timestamps[_photoLocations[photoLocation.PictureId].Timestamps.Count-1];
            double secondsBetweenFirstAndLast = lastTimestamp.Subtract(firstTimestamp).TotalSeconds;

            for (int i = 0; i < _photoLocations[photoLocation.PictureId].Timestamps.Count; i++)
            {
                DateTime timePosition = _photoLocations[photoLocation.PictureId].Timestamps[i];
                double secondsFromFirst = timePosition.Subtract(firstTimestamp).TotalSeconds;
                float position = 0;
                if (i > 0) 
                    position = (float)(secondsFromFirst / secondsBetweenFirstAndLast);

                PlayerUIController.Instance.InstantiateTimeSliderPoint(position, timePosition);
            }
        }
        else
        {
            PlayerUIController.Instance.SetSliderVisibility(false);
        }
    }

    public void ChangeTimeOnLoadedPhoto(DateTime dateTime)
    {
        string sId = _currentLoadedPhotoLocation.PictureId.ToString("D4", CultureInfo.InvariantCulture);
        string imagePath = $"{_cameraFolderPath}/{MazeMapController.Instance.CampusId}/{sId}_{dateTime.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)}";
        foreach (KeyValuePair<Camera, MeshRenderer> camera in _cameraPlanes) 
        {
            string path = $"{imagePath}/{camera.Key}/IMAG{sId}.JPG";
            StartCoroutine(RenderPhoto(path, camera.Value));
        }
    }

    public void DisableView()
    {
        _camera.enabled = false;
        _active = false;
    }

    [Serializable]
    public struct CameraMapping
    {
        public Camera Camera;
        public MeshRenderer Plane;
    }

}
