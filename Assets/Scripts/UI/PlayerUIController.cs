using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIController : MonoBehaviour
{
    public static PlayerUIController Instance { get; private set; }

    private enum WaypointMode { Point, Route }
    //Right Panel
    [SerializeField] private GameObject _rightPanel;
    [SerializeField] private Button _generateCampus;
    [SerializeField] private Button _goToBuilding;
    [SerializeField] private Button _nextRobot;
    [SerializeField] private Button _toggleWaypointMode;
    [SerializeField] private Text _toggleWaypointModeText;
    [SerializeField] private Color _routeColorNormal;
    [SerializeField] private Color _routeColorHovered;
    [SerializeField] private Color _routeColorDown;
    [SerializeField] private Button _clearAllWaypoints;
    [SerializeField] private Button _driveRobot;
    [SerializeField] private Button _loadRoute;
    [SerializeField] private Button _saveRoute;
    //[SerializeField] private Color _driveRobotStopColorNormal;
    //[SerializeField] private Color _driveRobotStopColorHovered;
    //[SerializeField] private Color _driveRobotStopColorDown;
    [SerializeField] private Text _driveRobotText;
    [SerializeField] private Button _pauseRobot;
    [SerializeField] private Text _pauseRobotText;
    [SerializeField] private Color _resumeColorNormal;
    [SerializeField] private Color _resumeColorHovered;
    [SerializeField] private Color _resumeColorDown;
    [SerializeField] private InputField _campusId;
    [SerializeField] private InputField _buildingName;
    [SerializeField] private InputField _routeName;

    //Loading Panel
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private Image _loadingFill;

    //Floor indicators
    [SerializeField] private GameObject _layerPanel;
    [SerializeField] private Text _layerNumberText;
    [SerializeField] private Button _layerUp;
    [SerializeField] private Button _layerDown;

    //Sorround Photo UI
    [SerializeField] private Button _backFromSorroundPhoto;
    [SerializeField] private GameObject _sorroundPhotoPanel;
    [SerializeField] private Slider _timeSlider;
    [SerializeField] private GameObject _timeSliderPositionPrefab;
    [SerializeField] private Text _timeSliderDateText;

    private WaypointMode CurrentWaypointMode
    {
        get { return _currentWaypointMode; }
        set
        {
            _currentWaypointMode = value;
            _toggleWaypointModeText.text = _currentWaypointMode.ToString() + " Mode";
            _toggleWaypointMode.colors = _currentWaypointMode == WaypointMode.Point ? _toggleWaypointPointColorBlock : _toggleWaypointRouteColorBlock;
        }
    }

    private bool IsPaused
    {
        get { return _isPaused; }
        set
        {
            _isPaused = value;
            _pauseRobot.colors = _isPaused ? _resumeRobotColorBlock : _pauseRobotColorBlock;
            _pauseRobotText.text = _isPaused ? "Resume Route" : "Pause Route";
        }
    }

    private WaypointMode _currentWaypointMode;
    private ColorBlock _toggleWaypointPointColorBlock;
    private ColorBlock _toggleWaypointRouteColorBlock;
    private ColorBlock _driveRobotColorBlock;
    private ColorBlock _driveRobotStopColorBlock;
    private ColorBlock _pauseRobotColorBlock;
    private ColorBlock _resumeRobotColorBlock;
    private WaypointController _waypointController;
    private ROSController _selectedRobot;
    private RectTransform _sliderTransform;
    private bool _isDriving;
    private bool _isPaused;
    private Dictionary<float, TimeSliderPosition> _timeSliderPositions;
    private float _highlightedTimeSliderPosition;

    void Awake()
    {
        Instance = this;
        _generateCampus.onClick.AddListener(GenerateCampusButtonOnClick);
        _goToBuilding.onClick.AddListener(GoToBuildingOnClick);
        _nextRobot.onClick.AddListener(NextRobotOnClick);
        _toggleWaypointMode.onClick.AddListener(ToggleWaypointModeOnClick);
        _clearAllWaypoints.onClick.AddListener(ClearAllWaypointsOnClick);
        _driveRobot.onClick.AddListener(DriveRobotOnClick);
        _loadRoute.onClick.AddListener(LoadRouteClick);
        _saveRoute.onClick.AddListener(SaveRouteClick);
        _pauseRobot.onClick.AddListener(PauseRobotOnClick);
        _backFromSorroundPhoto.onClick.AddListener(BackFromSorroundPhotoClick);
        _timeSlider.onValueChanged.AddListener(OnTimeSliderValueChanged);

        _toggleWaypointPointColorBlock = _toggleWaypointMode.colors;

        _toggleWaypointRouteColorBlock = _toggleWaypointMode.colors;
        _toggleWaypointRouteColorBlock.normalColor = _routeColorNormal;
        _toggleWaypointRouteColorBlock.highlightedColor = _routeColorHovered;
        _toggleWaypointRouteColorBlock.pressedColor = _routeColorDown;

        _driveRobotColorBlock = _driveRobot.colors;

        _driveRobotStopColorBlock = _driveRobot.colors;
        //_driveRobotStopColorBlock.normalColor = _driveRobotStopColorNormal;
       // _driveRobotStopColorBlock.highlightedColor = _driveRobotStopColorHovered;
       // _driveRobotStopColorBlock.pressedColor = _driveRobotStopColorDown;

        _pauseRobotColorBlock = _pauseRobot.colors;

        _resumeRobotColorBlock = _pauseRobotColorBlock;
        _resumeRobotColorBlock.normalColor = _resumeColorNormal;
        _resumeRobotColorBlock.highlightedColor = _resumeColorHovered;
        _resumeRobotColorBlock.pressedColor = _resumeColorDown;

        _layerUp.onClick.AddListener(() => { LayerChangeClick(true); });
        _layerDown.onClick.AddListener(() => { LayerChangeClick(false); });

        _sliderTransform = _timeSlider.GetComponent<RectTransform>();
    }

    void Start()
    {
        _waypointController = PlayerController.Instance.WaypointController;
        _layerNumberText.text = MazeMapController.Instance.CurrentActiveLevel.ToString();
    }

    private void GenerateCampusButtonOnClick()
    {
        MazeMapController.Instance.ClearAll();
        int id = -1;
        _loadingFill.fillAmount = 0;
        _loadingPanel.SetActive(true);
        if (int.TryParse(_campusId.text, out id))
        {
            MazeMapController.Instance.GenerateCampus(id);
        }
        else
        {
            Debug.LogError("Cannot parse CampusId to int");
        }
    }

    private void GoToBuildingOnClick()
    {
        string buildingName = _buildingName.text;
        Building building = MazeMapController.Instance.GetBuildingByName(buildingName);

        //Needs to be improved
        if (building != null)
        {
            PlayerController.Instance.FocusCameraOn(building.Floors.First().Value.RenderedModel);
        }
    }

    private void NextRobotOnClick()
    {
        _selectedRobot = RobotMasterController.Instance.GetNextRobot();
        if (_selectedRobot == null) return;

        PlayerController.Instance.FocusCameraOn(_selectedRobot.transform);
        _driveRobot.interactable = true;
    }

    private void ToggleWaypointModeOnClick()
    {
        CurrentWaypointMode = CurrentWaypointMode == WaypointMode.Point ? WaypointMode.Route : WaypointMode.Point;
        _waypointController.ToggleWaypointMode();
    }

    private void ClearAllWaypointsOnClick()
    {
        _waypointController.ClearAllWaypoints();
    }

    private void DriveRobotOnClick()
    {
        if (_selectedRobot == null) return;
        if (_isDriving)
        {
            _selectedRobot.StopRobot();
            SetDriveMode(false);
        }
        else
        {
            List<GeoPointWGS84> path = _waypointController.GetPath().Select(point => point.ToUTM().ToWGS84()).ToList();
            _selectedRobot.MovePath(path);
            SetPauseMode(true);
            SetDriveMode(true);
        }
    }

    private void PauseRobotOnClick()
    {
        if (_selectedRobot == null) return;
        if (_isDriving)
        {
            if (IsPaused)
                _selectedRobot.ResumePath();
            else
                _selectedRobot.PausePath();
            IsPaused = !IsPaused;
        }
    }

    private void LayerChangeClick(bool up)
    {
        MazeMapController.Instance.ChangeActiveLayer(up);
        _layerNumberText.text = MazeMapController.Instance.CurrentActiveLevel.ToString();
    }

    private void LoadRouteClick()
    {
        WaypointController.Instance.LoadRoute(_routeName.text);
    }

    private void SaveRouteClick()
    {
        WaypointController.Instance.SaveRoute(_routeName.text);
    }

    private void BackFromSorroundPhotoClick()
    {
        _sorroundPhotoPanel.gameObject.SetActive(false);
        _rightPanel.gameObject.SetActive(true);
        _layerPanel.SetActive(true);
        SorroundPhotoController.Instance.DisableView();
        PlayerController.Instance.CurrentPlayerState = PlayerController.PlayerState.Normal;
    }

    private void ClearTimeSliderHighlights()
    {
        foreach (KeyValuePair<float, TimeSliderPosition> pair in _timeSliderPositions)
        {
            pair.Value.SetHighlight(false);
        }
    }

    private void OnTimeSliderValueChanged(float newValue)
    {
        float closestPosition = 0;
        float distanceToClosest = float.MaxValue;
        foreach (KeyValuePair<float, TimeSliderPosition> pair in _timeSliderPositions) {
            float distance = Mathf.Abs(_timeSlider.value - pair.Key);
            if (distance < distanceToClosest) {
                closestPosition = pair.Key;
                distanceToClosest = distance;
            }
        }

        if (closestPosition != _highlightedTimeSliderPosition)
        {
            ClearTimeSliderHighlights();
            _timeSliderDateText.text = _timeSliderPositions[closestPosition].Timestamp.ToString("dd-MM-yyyy");
        }
        _highlightedTimeSliderPosition = closestPosition;
        _timeSliderPositions[closestPosition].SetHighlight(true);
    }

    public void OnTimeSlider()
    {
        _timeSlider.value = _highlightedTimeSliderPosition;
        DateTime selectedDateTime = _timeSliderPositions[_timeSlider.value].Timestamp;
        _timeSliderDateText.text = selectedDateTime.ToString("dd-MM-yyyy");
        SorroundPhotoController.Instance.ChangeTimeOnLoadedPhoto(selectedDateTime);
    }

    public void SetDriveMode(bool isDriving)
    {
        _driveRobot.colors = isDriving ? _driveRobotStopColorBlock : _driveRobotColorBlock;
        _driveRobotText.text = isDriving ? "Stop" : "Drive";
        _isDriving = isDriving;
        if (!isDriving)
            IsPaused = false;
        _pauseRobot.interactable = _isDriving;
    }

    public void SetPauseMode(bool isPaused)
    {
        if (_selectedRobot == null) return;
        _pauseRobot.interactable = _isDriving;
        if (_isPaused)
            _selectedRobot.PausePath();
        else
            _selectedRobot.ResumePath();
        
        IsPaused = isPaused;
    }

    public void PhotoClicked()
    {
        _sorroundPhotoPanel.SetActive(true);
        _rightPanel.SetActive(false);
        _layerPanel.SetActive(false);
    }

    /// <summary>
    /// Updates loading bar
    /// </summary>
    /// <param name="percentDone">Value 0..1</param>
    public void UpdateLoadingProgress(float percentDone)
    {
        _loadingFill.fillAmount = percentDone;
        if (percentDone >= 1)
            _loadingPanel.SetActive(false);
    }

    public void SetSliderVisibility(bool showSlider)
    {
        _timeSlider.gameObject.SetActive(showSlider);
        _timeSliderDateText.gameObject.SetActive(showSlider);
    }

    public void InstantiateTimeSliderPoint(float position, DateTime date)
    {
        GameObject timeSliderPoint = Instantiate(_timeSliderPositionPrefab, Vector3.zero, Quaternion.identity, _timeSlider.transform);
        timeSliderPoint.transform.SetSiblingIndex(2);
        float positionX = position * _sliderTransform.rect.width - _sliderTransform.rect.width / 2;
        timeSliderPoint.transform.localPosition += new Vector3(positionX, 0, 0);
        TimeSliderPosition sliderPosition = timeSliderPoint.GetComponent<TimeSliderPosition>();
        sliderPosition.Timestamp = date;
        _timeSliderPositions.Add(position, sliderPosition);
        _timeSliderDateText.text = _timeSliderPositions[0].Timestamp.ToString("dd-MM-yyyy");
    }

    public void ResetTimeSlider()
    {
        _timeSliderPositions = new Dictionary<float, TimeSliderPosition>();
    }

}
