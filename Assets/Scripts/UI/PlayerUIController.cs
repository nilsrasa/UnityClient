using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIController : MonoBehaviour
{
    public static PlayerUIController Instance { get; private set; }

    private enum UIState { Navigation, Options, PlacingFiducial, UpdatingFiducial, DeletingFiducial, SorroundPhoto, Loading }
    private enum RobotDrivingUIState { NoRobotSelected, RobotStopped, RobotDriving, RobotPaused }
    private enum WaypointMode { Point, Route }

    //Right Panel
    [Header("Right Panel")]
    [SerializeField] private GameObject _rightPanel;
    [SerializeField] private Button _generateCampus;
    [SerializeField] private Button _goToBuilding;
    [SerializeField] private Dropdown _selectRobot;
    [SerializeField] private Button _toggleWaypointMode;
    [SerializeField] private Text _toggleWaypointModeText;
    [SerializeField] private Color _routeColorNormal;
    [SerializeField] private Color _routeColorHovered;
    [SerializeField] private Color _routeColorDown;
    [SerializeField] private Button _clearAllWaypoints;
    [SerializeField] private Button _returnToBase;
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
    [SerializeField] private Button _options;

    //Loading Panel
    [Header("Loading Panel")]
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private Image _loadingFill;

    //Floor indicators
    [Header("Floor Level Panel")]
    [SerializeField] private GameObject _layerPanel;
    [SerializeField] private Text _layerNumberText;
    [SerializeField] private Button _layerUp;
    [SerializeField] private Button _layerDown;
        
    //Sorround Photo UI
    [Header("Sorround Photo Panel")]
    [SerializeField] private Button _backFromSorroundPhoto;
    [SerializeField] private GameObject _sorroundPhotoPanel;
    [SerializeField] private Slider _timeSlider;
    [SerializeField] private GameObject _timeSliderPositionPrefab;
    [SerializeField] private Text _timeSliderDateText;

    [Header("Options Panel")]
    [SerializeField] private GameObject _optionsPanel;
    [SerializeField] private Button _addFiducial;
    [SerializeField] private Button _updateFiducial;
    [SerializeField] private Button _deleteFiducial;
    [SerializeField] private Button _exitApplication;
    [SerializeField] private Button _closeOptions;

    [Header("Info Panel")]
    [SerializeField] private GameObject _infoPanel;
    [SerializeField] private Text _infoText;

    [Header("Add Fiducial Panel")]
    [SerializeField] private GameObject _addFiducialPanel;
    [SerializeField] private InputField _addFidId;
    [SerializeField] private InputField _addFidPosX;
    [SerializeField] private InputField _addFidPosY;
    [SerializeField] private InputField _addFidPosZ;
    [SerializeField] private InputField _addFidRotX;
    [SerializeField] private InputField _addFidRotY;
    [SerializeField] private InputField _addFidRotZ;
    [SerializeField] private Button _addFidAccept;
    [SerializeField] private Button _addFidCancel;
    
    [Header("Done Panel")]
    [SerializeField] private GameObject _donePanel;
    [SerializeField] private Button _doneAccept;
    [SerializeField] private Button _doneCancel;

    [Header("Robot Panel")]
    [SerializeField] private GameObject _robotPanel;
    [SerializeField] private Button _robotListRefreshList;
    [SerializeField] private Button _robotListClose;
    [SerializeField] private Text _robotRefreshText;
    [SerializeField] private RectTransform _listContentsParent;
    [SerializeField] private GameObject _robotListPrefab;
    [SerializeField] private RefreshButton _refreshButton;

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

    private UIState CurrentUIState
    {
        get { return _currentUIState; }
        set
        {
            if (_currentUIState == value) return;
            _currentUIState = value;
            HideAllPanels();
            switch (_currentUIState)
            {
                case UIState.Navigation:
                    ActivatePanels(_rightPanel, _layerPanel);
                    SorroundPhotoController.Instance.SetCameraPositionVisibility(true);
                    break;
                case UIState.Options:
                    ActivatePanels(_rightPanel, _optionsPanel, _layerPanel);
                    break;
                case UIState.PlacingFiducial:
                    ActivatePanels(_addFiducialPanel, _layerPanel);
                    SetInfoText("You can place a Fiducial on the ceiling by left-clicking on the map.");
                    break;
                case UIState.UpdatingFiducial:
                    ActivatePanels(_addFiducialPanel, _layerPanel, _donePanel);
                    SetInfoText("Click a Fiducial to update position and rotation.\nFinish by clicking Accept/Cancel.");
                    break;
                case UIState.DeletingFiducial:
                    ActivatePanels(_layerPanel, _donePanel);
                    SetInfoText("Click a Fiducial to delete it.\nFinish by clicking Accept/Cancel.");
                    break;
                case UIState.SorroundPhoto:
                    ActivatePanels(_sorroundPhotoPanel);
                    break;
                case UIState.Loading:
                    ActivatePanels(_loadingPanel);
                    break;
            }
        }
    }

    private RobotDrivingUIState CurrentRobotDrivingUIState
    {
        get { return _currentRobotDrivingUiState; }
        set
        {
            _currentRobotDrivingUiState = value;
            _driveRobot.interactable = false;
            _pauseRobot.interactable = false;
            _returnToBase.interactable = false;
            switch (value)
            {
                case RobotDrivingUIState.NoRobotSelected:
                    SetDriveMode(false);
                    break;
                case RobotDrivingUIState.RobotStopped:
                    SetDriveMode(false);
                    IsPaused = false;
                    _driveRobot.interactable = true;
                    _returnToBase.interactable = true;
                    _driveRobot.colors = _driveRobotColorBlock;
                    _driveRobotText.text = "Drive";
                    _isDriving = false;
                    break;
                case RobotDrivingUIState.RobotDriving:
                    SetDriveMode(true);
                    IsPaused = false;
                    _driveRobot.interactable = true;
                    _pauseRobot.interactable = true;
                    _returnToBase.interactable = true;
                    _driveRobot.colors = _driveRobotStopColorBlock;
                    _driveRobotText.text ="Stop";
                    _isDriving = true;
                    break;
                case RobotDrivingUIState.RobotPaused:
                    IsPaused = true;
                    _driveRobot.interactable = true;
                    _pauseRobot.interactable = true;
                    _returnToBase.interactable = true;
                    _driveRobot.colors = _driveRobotStopColorBlock;
                    _driveRobotText.text = "Stop";
                    _isDriving = true;
                    break;
            }
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

    private WaypointMode _currentWaypointMode = WaypointMode.Point;
    private UIState _currentUIState = UIState.Loading;
    private RobotDrivingUIState _currentRobotDrivingUiState = RobotDrivingUIState.NoRobotSelected;
    private ColorBlock _toggleWaypointPointColorBlock;
    private ColorBlock _toggleWaypointRouteColorBlock;
    private ColorBlock _driveRobotColorBlock;
    private ColorBlock _driveRobotStopColorBlock;
    private ColorBlock _pauseRobotColorBlock;
    private ColorBlock _resumeRobotColorBlock;
    private WaypointController _waypointController;
    private RectTransform _sliderTransform;
    private bool _isPaused;
    private bool _isDriving;
    private Dictionary<float, TimeSliderPosition> _timeSliderPositions;
    private float _highlightedTimeSliderPosition;
    private Transform _fiducialToUpdate;
    private Dictionary<RobotMasterController.Robot, RobotListItem> _robotListItems = new Dictionary<RobotMasterController.Robot, RobotListItem>();
    private bool _refreshingRobotList;
    private int _robotsLeftToRefresh;

    void Awake()
    {
        Instance = this;
        _generateCampus.onClick.AddListener(GenerateCampusButtonOnClick);
        _goToBuilding.onClick.AddListener(GoToBuildingOnClick);
        _selectRobot.onValueChanged.AddListener(OnSelectedRobotValueChanged);
        _toggleWaypointMode.onClick.AddListener(ToggleWaypointModeOnClick);
        _clearAllWaypoints.onClick.AddListener(ClearAllWaypointsOnClick);
        _driveRobot.onClick.AddListener(DriveRobotOnClick);
        _loadRoute.onClick.AddListener(LoadRouteClick);
        _saveRoute.onClick.AddListener(SaveRouteClick);
        _pauseRobot.onClick.AddListener(PauseRobotClick);
        _backFromSorroundPhoto.onClick.AddListener(BackFromSorroundPhotoClick);
        _timeSlider.onValueChanged.AddListener(OnTimeSliderValueChanged);
        _returnToBase.onClick.AddListener(ReturnRobotToBase);
        _options.onClick.AddListener(OptionsClick);
        _addFiducial.onClick.AddListener(AddFiducialClick);
        _exitApplication.onClick.AddListener(ExitApplicationClick);
        _closeOptions.onClick.AddListener(OptionsCloseClick);
        _addFidAccept.onClick.AddListener(AddFiducialAcceptClick);
        _addFidCancel.onClick.AddListener(AddFiducialCancelClick);
        _addFidPosX.onEndEdit.AddListener(OnAddFiducialValuesChanged);
        _addFidPosY.onEndEdit.AddListener(OnAddFiducialValuesChanged);
        _addFidPosZ.onEndEdit.AddListener(OnAddFiducialValuesChanged);
        _addFidRotX.onEndEdit.AddListener(OnAddFiducialValuesChanged);
        _addFidRotY.onEndEdit.AddListener(OnAddFiducialValuesChanged);
        _addFidRotZ.onEndEdit.AddListener(OnAddFiducialValuesChanged);
        _addFidId.onEndEdit.AddListener(OnAddFiducialValuesChanged);
        _updateFiducial.onClick.AddListener(UpdateFiducialClick);
        _deleteFiducial.onClick.AddListener(DeleteFiducialClick);
        _doneAccept.onClick.AddListener(DoneAcceptClick);
        _doneCancel.onClick.AddListener(DoneCancelClick);
        _robotListClose.onClick.AddListener(RobotListCloseClick);
        _robotListRefreshList.onClick.AddListener(RobotListRefreshClick);


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

        CurrentUIState = UIState.Navigation;
        CurrentRobotDrivingUIState = RobotDrivingUIState.NoRobotSelected;
        MazeMapController.Instance.OnFinishedGeneratingCampus += OnFinishedGeneratingCampus;
    }

    void Update()
    {
        if (_refreshingRobotList)
        {
            UpdateRobotList();
            if (_robotsLeftToRefresh <= 0)
            {
                _robotRefreshText.text = "Refresh";
                _refreshingRobotList = false;
                _refreshButton.ShouldRotate = false;
            }
            else
            {
                _robotRefreshText.text = "Refreshing: " + _robotsLeftToRefresh + "/" + RobotMasterController.Robots.Count + "...";
            }
        }
    }

    private void HideAllPanels()
    {
        _addFiducialPanel.SetActive(false);
        _optionsPanel.SetActive(false);
        _sorroundPhotoPanel.SetActive(false);
        _rightPanel.SetActive(false);
        _loadingPanel.SetActive(false);
        _layerPanel.SetActive(false);
        _donePanel.SetActive(false);
        HideInfoText();
    }

    private void ActivatePanels(params GameObject[] panels)
    {
        foreach (GameObject panel in panels)
        {
            panel.SetActive(true);
        }
    }

    private void OnFinishedGeneratingCampus(int campusId)
    {
        _selectRobot.options = new List<Dropdown.OptionData>();
        _selectRobot.options.Add(new Dropdown.OptionData("No Robot Selected"));
        foreach (string robotName in RobotMasterController.Instance.GetRobotNames(campusId))
        {
            _selectRobot.options.Add(new Dropdown.OptionData(robotName));
        }
        _selectRobot.interactable = true;
    }

    #region ButtonClickEvents
    private void GenerateCampusButtonOnClick()
    {
        MazeMapController.Instance.ClearAll();
        int id = -1;
        _loadingFill.fillAmount = 0;
        CurrentUIState = UIState.Loading;
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

        //TODO: Needs to be improved
        if (building != null)
        {
            PlayerController.Instance.FocusCameraOn(building.Floors.First().Value.RenderedModel);
        }
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

    private void ReturnRobotToBase()
    {
        //TODO: Hardcoded
        MazeMapController.Instance.GetPath(RobotMasterController.SelectedRobot.transform.position.ToUTM().ToWGS84(), new GeoPointWGS84() { latitude = 55.78268988306574, longitude = 12.514101387003798 });
    }

    private void DriveRobotOnClick()
    {
        if (_isDriving)
        {
            RobotMasterController.SelectedRobot.StopRobot();
            CurrentRobotDrivingUIState = RobotDrivingUIState.RobotStopped;
        }
        else
        {
            List<GeoPointWGS84> path = _waypointController.GetPath().Select(point => point.ToUTM().ToWGS84()).ToList();
            RobotMasterController.SelectedRobot.MovePath(path);
            CurrentRobotDrivingUIState = RobotDrivingUIState.RobotDriving;
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
        CurrentUIState = UIState.Navigation;
        SorroundPhotoController.Instance.DisableView();
        PlayerController.Instance.CurrentPlayerState = PlayerController.PlayerState.Normal;
    }

    private void OptionsClick()
    {
        CurrentUIState = UIState.Options;
    }

    private void OptionsCloseClick()
    {
        CurrentUIState = UIState.Navigation;
    }

    private void AddFiducialClick()
    {
        CurrentUIState = UIState.PlacingFiducial;
        SorroundPhotoController.Instance.SetCameraPositionVisibility(false);
        RegisterMouseClick();
        _addFidId.text = FiducialController.Instance.GetNewFiducialId().ToString();
        _addFidPosX.text = "0.0";
        _addFidPosY.text = "0.0";
        _addFidPosZ.text = "0.0";
        _addFidRotX.text = "0.0";
        _addFidRotY.text = "0.0";
        _addFidRotZ.text = "0.0";
    }

    private void ExitApplicationClick()
    {
        Application.Quit();
    }

    private void AddFiducialAcceptClick()
    {
        UnregisterMouseClick();
        if (CurrentUIState == UIState.PlacingFiducial)
            FiducialController.Instance.FinalizeNewFiducialPlacement();
        else if (CurrentUIState == UIState.UpdatingFiducial)
            FiducialController.Instance.FinalizeUpdate();
        CurrentUIState = UIState.Navigation;
    }

    private void AddFiducialCancelClick()
    {
        UnregisterMouseClick();
        if (CurrentUIState == UIState.PlacingFiducial)
            FiducialController.Instance.CancelNewFiducialPlacement();
        else if (CurrentUIState == UIState.UpdatingFiducial)
            FiducialController.Instance.CancelUpdate();
        CurrentUIState = UIState.Navigation;
    }

    private void UpdateFiducialClick()
    {
        FiducialController.Instance.SetFiducialColliders(true);
        SorroundPhotoController.Instance.SetCameraPositionVisibility(false);
        CurrentUIState = UIState.UpdatingFiducial;
        RegisterMouseClick();
    }

    private void DeleteFiducialClick()
    {
        FiducialController.Instance.SetFiducialColliders(true);
        SorroundPhotoController.Instance.SetCameraPositionVisibility(false);
        CurrentUIState = UIState.DeletingFiducial;
        RegisterMouseClick();
    }

    private void DoneAcceptClick()
    {
        switch (CurrentUIState)
        {
            case UIState.UpdatingFiducial:
                FiducialController.Instance.CancelUpdate();
                UnregisterMouseClick();
                CurrentUIState = UIState.Navigation;
                FiducialController.Instance.SetFiducialColliders(false);
                break;
            case UIState.DeletingFiducial:
                FiducialController.Instance.FinalizeDelete();
                UnregisterMouseClick();
                FiducialController.Instance.SetFiducialColliders(false);
                CurrentUIState = UIState.Navigation;
                break;
        }
    }

    private void DoneCancelClick()
    {
        switch (CurrentUIState)
        {
            case UIState.UpdatingFiducial:
                FiducialController.Instance.CancelUpdate();
                UnregisterMouseClick();
                CurrentUIState = UIState.Navigation;
                FiducialController.Instance.SetFiducialColliders(false);
                break;
            case UIState.DeletingFiducial:
                FiducialController.Instance.CancelDelete();
                UnregisterMouseClick();
                FiducialController.Instance.SetFiducialColliders(false);
                CurrentUIState = UIState.Navigation;
                break;
        }
    }

    private void PauseRobotClick()
    {
        if (_isPaused)
            RobotMasterController.SelectedRobot.PausePath();
        else
            RobotMasterController.SelectedRobot.ResumePath();

        IsPaused = !IsPaused;
    }

    private void RobotListRefreshClick()
    {
        _refreshingRobotList = true;
        _robotsLeftToRefresh = RobotMasterController.Robots.Count;
        _refreshButton.ShouldRotate = true;
        RobotMasterController.Instance.RefreshRobotConnections();
    }

    private void RobotListCloseClick()
    {
        
    }

    #endregion

    #region OnValueChangeEvents
    private void OnAddFiducialValuesChanged(string value)
    {
        Vector3 position = new Vector3(float.Parse(_addFidPosX.text, CultureInfo.InvariantCulture),
            float.Parse(_addFidPosY.text, CultureInfo.InvariantCulture), float.Parse(_addFidPosZ.text, CultureInfo.InvariantCulture));
        Vector3 rotation = new Vector3(float.Parse(_addFidRotX.text, CultureInfo.InvariantCulture),
            float.Parse(_addFidRotY.text, CultureInfo.InvariantCulture), float.Parse(_addFidRotZ.text, CultureInfo.InvariantCulture));
        int id = int.Parse(_addFidId.text);
        if (CurrentUIState == UIState.PlacingFiducial)
            FiducialController.Instance.PlaceOrUpdateNewFiducial(int.Parse(_addFidId.text), position, rotation);
        else if (CurrentUIState == UIState.UpdatingFiducial)
            FiducialController.Instance.UpdateFiducial(id, position, rotation);
    }

    private void OnSelectedRobotValueChanged(int newIndex)
    {
        //TODO: Update once new system in place
        //UpdateUI(RobotMasterController.Instance.LoadRobot(_selectRobot.options[newIndex].text));
    }

    private void OnTimeSliderValueChanged(float newValue)
    {
        float closestPosition = 0;
        float distanceToClosest = float.MaxValue;
        foreach (KeyValuePair<float, TimeSliderPosition> pair in _timeSliderPositions)
        {
            float distance = Mathf.Abs(_timeSlider.value - pair.Key);
            if (distance < distanceToClosest)
            {
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

    public void OnTimeSliderValueChanged()
    {
        _timeSlider.value = _highlightedTimeSliderPosition;
        DateTime selectedDateTime = _timeSliderPositions[_timeSlider.value].Timestamp;
        _timeSliderDateText.text = selectedDateTime.ToString("dd-MM-yyyy");
        SorroundPhotoController.Instance.ChangeTimeOnLoadedPhoto(selectedDateTime);
    }
    #endregion

    #region MouseClickEvents
    private void RegisterMouseClick()
    {
        PlayerController.Instance.OnMouseClick += OnMouseClick;
    }

    private void UnregisterMouseClick()
    {
        PlayerController.Instance.OnMouseClick -= OnMouseClick;
    }

    private void OnMouseClick(Ray ray)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, Single.PositiveInfinity);
        foreach (RaycastHit hit in hits)
        {
            switch (CurrentUIState)
            {
                case UIState.Navigation:
                    break;
                case UIState.Options:
                    break;
                case UIState.PlacingFiducial:
                    if (hit.collider.gameObject.layer != LayerMask.NameToLayer("Floor")) continue;
                    OnAddFiducialMouseClick(hit.point);
                    return;
                case UIState.UpdatingFiducial:
                    if (!hit.collider.CompareTag("Fiducial")) continue;
                    OnUpdateFiducialMouseClick(hit.transform);
                    return;
                case UIState.DeletingFiducial:
                    if (!hit.collider.CompareTag("Fiducial")) continue;
                    OnDeleteFiducialMouseClick(hit.transform);
                    return;
                case UIState.SorroundPhoto:
                    break;
                case UIState.Loading:
                    break;
            }
        }
    }

    private void OnUpdateFiducialMouseClick(Transform fiducialToUpdate)
    {
        _addFiducialPanel.SetActive(true);
        _addFidPosX.text = fiducialToUpdate.position.x.ToString(CultureInfo.InvariantCulture);
        _addFidPosY.text = fiducialToUpdate.position.y.ToString(CultureInfo.InvariantCulture);
        _addFidPosZ.text = fiducialToUpdate.position.z.ToString(CultureInfo.InvariantCulture);
        _addFidRotX.text = fiducialToUpdate.eulerAngles.x.ToString(CultureInfo.InvariantCulture);
        _addFidRotY.text = fiducialToUpdate.eulerAngles.y.ToString(CultureInfo.InvariantCulture);
        _addFidRotZ.text = fiducialToUpdate.eulerAngles.z.ToString(CultureInfo.InvariantCulture);
        _addFidId.text = FiducialController.Instance.StartUpdateFiducial(fiducialToUpdate).ToString();
        _donePanel.SetActive(false);
    }

    private void OnDeleteFiducialMouseClick(Transform fiducialToDelete)
    {
        FiducialController.Instance.DeleteFiducial(fiducialToDelete);
    }

    private void OnAddFiducialMouseClick(Vector3 clickPos)
    {
        clickPos = new Vector3(clickPos.x, ConfigManager.ConfigFile.ZeroFiducial.Position.ToUTM().ToUnity().y, clickPos.z);
        _addFidPosX.text = clickPos.x.ToString(CultureInfo.InvariantCulture);
        _addFidPosY.text = clickPos.y.ToString(CultureInfo.InvariantCulture);
        _addFidPosZ.text = clickPos.z.ToString(CultureInfo.InvariantCulture);
        _addFidRotX.text = "0.0";
        _addFidRotY.text = "0.0";
        _addFidRotZ.text = "0.0";

        Vector3 position = new Vector3(float.Parse(_addFidPosX.text, CultureInfo.InvariantCulture),
            float.Parse(_addFidPosY.text, CultureInfo.InvariantCulture), float.Parse(_addFidPosZ.text, CultureInfo.InvariantCulture));
        Vector3 rotation = new Vector3(float.Parse(_addFidRotX.text, CultureInfo.InvariantCulture),
            float.Parse(_addFidRotY.text, CultureInfo.InvariantCulture), float.Parse(_addFidRotZ.text, CultureInfo.InvariantCulture));
        FiducialController.Instance.PlaceOrUpdateNewFiducial(int.Parse(_addFidId.text), position, rotation);
    }

    public void PhotoClicked()
    {
        CurrentUIState = UIState.SorroundPhoto;
    }
    #endregion

    private void ClearTimeSliderHighlights()
    {
        foreach (KeyValuePair<float, TimeSliderPosition> pair in _timeSliderPositions)
        {
            pair.Value.SetHighlight(false);
        }
    }

    private void SetInfoText(string text)
    {
        _infoPanel.SetActive(true);
        _infoText.text = text;
        LayoutRebuilder.ForceRebuildLayoutImmediate(_infoPanel.GetComponent<RectTransform>());
    }

    private void HideInfoText()
    {
        _infoPanel.SetActive(false);
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

    /// <summary>
    /// Updates loading bar
    /// </summary>
    /// <param name="percentDone">Value 0..1</param>
    public void UpdateLoadingProgress(float percentDone)
    {
        _loadingFill.fillAmount = percentDone;
        if (percentDone >= 1)
            CurrentUIState = UIState.Navigation;
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

    public void UpdateUI(ROSController robot)
    {
        if (robot.CurrenLocomotionType == ROSController.RobotLocomotionType.DIRECT)
        {
            CurrentRobotDrivingUIState = RobotDrivingUIState.RobotStopped;
        }
        else
        {
            if (robot.CurrentRobotLocomotionState == ROSController.RobotLocomotionState.MOVING)
                CurrentRobotDrivingUIState = RobotDrivingUIState.RobotDriving;
            else if (robot.CurrentRobotLocomotionState == ROSController.RobotLocomotionState.STOPPED)
                CurrentRobotDrivingUIState = RobotDrivingUIState.RobotStopped;
        }
    }

    public void LoadRobots(List<RobotMasterController.Robot> robots)
    {
        foreach (KeyValuePair<RobotMasterController.Robot, RobotListItem> pair in _robotListItems)
        {
            Destroy(pair.Value.gameObject);
        }
        _robotListItems = new Dictionary<RobotMasterController.Robot, RobotListItem>();

        foreach (RobotMasterController.Robot robot in robots)
        {
            RobotListItem listItem = Instantiate(_robotListPrefab, _listContentsParent).GetComponent<RobotListItem>();
            listItem.Initialise(robot.Name, robot.Uri, robot.Port);
            _robotListItems.Add(robot, listItem);
        }
    }

    public void UpdateRobotList()
    {
        foreach (KeyValuePair<string, RobotMasterController.Robot> pair in RobotMasterController.Robots)
        {
            _robotListItems[pair.Value].IsActive = pair.Value.IsActive;
        }
    }

    public void RobotRefreshed()
    {
        _robotsLeftToRefresh--;
    }
}
