using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUIController : MonoBehaviour
{
    public static PlayerUIController Instance { get; private set; }

    public bool ScrollLocked { get; private set; }

    private enum UIState
    {
        Navigation,
        Options,
        PlacingFiducial,
        UpdatingFiducial,
        DeletingFiducial,
        SorroundPhoto,
        Loading,
        RobotList,
        SetRobotPosition,
        SetRobotOrientation,
        RobotDebug
    }

    private enum RobotDrivingUIState
    {
        NoRobotSelected,
        RobotStopped,
        RobotDriving,
        RobotPaused
    }

    private enum WaypointMode
    {
        Point,
        Route
    }

    //Right Panel
    [Header("Right Panel")] [SerializeField] private GameObject _rightPanel;

    [SerializeField] private Button _generateCampus;
    [SerializeField] private Button _goToBuilding;
    [SerializeField] private Dropdown _selectRobot;
    [SerializeField] private Color _routeColorNormal;
    [SerializeField] private Color _routeColorHovered;
    [SerializeField] private Color _routeColorDown;
    [SerializeField] private Button _robotList;
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
    [SerializeField] private Button _saveFiducials;
    [SerializeField] private Button _overrideRobotPosition;
    [SerializeField] private Button _resetRobot;
    [SerializeField] private Button _robotDebug;
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
    [SerializeField] private InputField _addFidLon;
    [SerializeField] private InputField _addFidAlt;
    [SerializeField] private InputField _addFidLat;
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

    [Header("Robot Debug Panel")]
    [SerializeField] private GameObject _robotDebugPanel;
    [SerializeField] private Button _robotDebugClose;
    [SerializeField] private RectTransform _robotDebugContentsParent;
    [SerializeField] private GameObject _robotDebugMessagePrefab;
    [SerializeField] private Dropdown _robotDebugSelectRobot;

    [Header("Legend Panel")]
    [SerializeField] private GameObject _legendPanel;
    [SerializeField] private Button _legendShow;
    [SerializeField] private Toggle _legendSorroundPhotoToggle;
    [SerializeField] private Toggle _legendFiducialToggle;

    private UIState CurrentUIState
    {
        get { return _currentUIState; }
        set
        {
            _currentUIState = value;
            ScrollLocked = false;
            HideAllPanels();
            if (_robotLogListenLoop != null)
                StopCoroutine(_robotLogListenLoop);
            switch (_currentUIState)
            {
                case UIState.Navigation:
                    ActivatePanels(_rightPanel, _layerPanel);
                    if (_legendSorroundPhotoToggle.isOn)
                        SorroundPhotoController.Instance.SetCameraPositionVisibility(true);
                    if (MazeMapController.Instance.CampusLoaded)
                    {
                        SetInfoText("Left click to place waypoints. Left click on last waypoint to remove it.\nRight click on waypoint to change precision.");
                        ActivatePanels(_legendPanel);
                    }
                    break;
                case UIState.Options:
                    ActivatePanels(_rightPanel, _optionsPanel, _layerPanel);
                    break;
                case UIState.PlacingFiducial:
                    ActivatePanels(_addFiducialPanel, _layerPanel);
                    SetInfoText("You can place a Fiducial on the ceiling by left-clicking on the map.");
                    if (_legendFiducialToggle.isOn)
                        FiducialController.Instance.SetFiducialVisibility(true);
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
                    ScrollLocked = true;
                    break;
                case UIState.Loading:
                    ActivatePanels(_loadingPanel);
                    ScrollLocked = true;
                    break;
                case UIState.RobotList:
                    ActivatePanels(_rightPanel, _robotPanel);
                    RobotListRefreshClick();
                    ScrollLocked = true;
                    break;
                case UIState.SetRobotPosition:
                    ActivatePanels(_donePanel);
                    SetInfoText("Click to set the position of the robot");
                    break;
                case UIState.SetRobotOrientation:
                    ActivatePanels(_donePanel);
                    SetInfoText("Click to set robot orientation (Robot will face this point)");
                    break;
                case UIState.RobotDebug:
                    ActivatePanels(_rightPanel, _robotDebugPanel);
                    if (_selectRobot.options.Count > 1)
                    {
                        _robotDebugSelectRobot.options = _selectRobot.options.GetRange(1, _selectRobot.options.Count - 1);
                        _robotDebugSelectRobot.value = _selectRobot.value+1;

                        //string selectedRobot = _robotDebugSelectRobot.options[_selectRobot.value + 1].text;
                        //InitialiseRobotDebugPanel(RobotMasterController.Instance.GetRosControllerFromName(selectedRobot).GetRobotLogs());

                        _robotLogListenLoop = StartCoroutine(RobotDebugListen());
                    }
                    ScrollLocked = true;
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
            _loadRoute.interactable = false;
            _saveRoute.interactable = false;
            _clearAllWaypoints.interactable = false;
            switch (value)
            {
                case RobotDrivingUIState.NoRobotSelected:
                    SetDriveMode(false);
                    _isDriving = false;
                    _clearAllWaypoints.interactable = true;
                    break;
                case RobotDrivingUIState.RobotStopped:
                    SetDriveMode(false);
                    IsPaused = false;
                    _driveRobot.interactable = WaypointController.Instance.GetPath().Count > 0;
                    _returnToBase.interactable = true;
                    _driveRobot.colors = _driveRobotColorBlock;
                    _driveRobotText.text = "Drive";
                    _isDriving = false;
                    _loadRoute.interactable = true;
                    _saveRoute.interactable = true;
                    _clearAllWaypoints.interactable = true;
                    break;
                case RobotDrivingUIState.RobotDriving:
                    SetDriveMode(true);
                    IsPaused = false;
                    _driveRobot.interactable = true;
                    _pauseRobot.interactable = true;
                    _returnToBase.interactable = true;
                    _driveRobot.colors = _driveRobotStopColorBlock;
                    _driveRobotText.text = "Stop";
                    _isDriving = true;
                    break;
                case RobotDrivingUIState.RobotPaused:
                    IsPaused = true;
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

    private Canvas _canvas;
    private UIState _currentUIState = UIState.Loading;
    private RobotDrivingUIState _currentRobotDrivingUiState = RobotDrivingUIState.NoRobotSelected;
    private ColorBlock _driveRobotColorBlock;
    private ColorBlock _driveRobotStopColorBlock;
    private ColorBlock _pauseRobotColorBlock;
    private ColorBlock _resumeRobotColorBlock;
    private RectTransform _sliderTransform;
    private bool _isPaused;
    private bool _isDriving;
    private Dictionary<float, TimeSliderPosition> _timeSliderPositions;
    private float _highlightedTimeSliderPosition;
    private Transform _fiducialToUpdate;
    private Dictionary<RobotMasterController.Robot, RobotListItem> _robotListItems = new Dictionary<RobotMasterController.Robot, RobotListItem>();
    private bool _refreshingRobotList;
    private int _robotsLeftToRefresh;
    private bool _shouldUpdateRobotList;
    private Vector3 _robotOverridePosition;
    private Quaternion _robotOverrideOrientation;
    private bool _isLegendVisible;
    private bool _isLegendAnimating;
    private Coroutine _robotLogListenLoop;
    private List<RobotDebugListItem> _robotDebugListItems;

    void Awake()
    {
        Instance = this;
        _canvas = GetComponent<Canvas>();
        _generateCampus.onClick.AddListener(GenerateCampusButtonOnClick);
        _goToBuilding.onClick.AddListener(GoToBuildingOnClick);
        _selectRobot.onValueChanged.AddListener(OnSelectedRobotValueChanged);
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
        _addFidPosX.onEndEdit.AddListener(OnAddFiducialPositionValuesChanged);
        _addFidPosY.onEndEdit.AddListener(OnAddFiducialPositionValuesChanged);
        _addFidPosZ.onEndEdit.AddListener(OnAddFiducialPositionValuesChanged);
        _addFidRotX.onEndEdit.AddListener(OnAddFiducialRotationValuesChanged);
        _addFidRotY.onEndEdit.AddListener(OnAddFiducialRotationValuesChanged);
        _addFidRotZ.onEndEdit.AddListener(OnAddFiducialRotationValuesChanged);
        _addFidLon.onEndEdit.AddListener(OnAddFiducialGPSValuesChanged);
        _addFidLat.onEndEdit.AddListener(OnAddFiducialGPSValuesChanged);
        _addFidAlt.onEndEdit.AddListener(OnAddFiducialGPSValuesChanged);
        _addFidId.onEndEdit.AddListener(OnAddFiducialPositionValuesChanged);
        _updateFiducial.onClick.AddListener(UpdateFiducialClick);
        _deleteFiducial.onClick.AddListener(DeleteFiducialClick);
        _doneAccept.onClick.AddListener(DoneAcceptClick);
        _doneCancel.onClick.AddListener(DoneCancelClick);
        _robotListClose.onClick.AddListener(RobotListCloseClick);
        _robotListRefreshList.onClick.AddListener(RobotListRefreshClick);
        _robotList.onClick.AddListener(RobotListClick);
        _overrideRobotPosition.onClick.AddListener(OverrideRobotPositionClick);
        _saveFiducials.onClick.AddListener(SaveFiducialsToFileClick);
        _legendFiducialToggle.onValueChanged.AddListener(LegendToggleValueChanged);
        _legendSorroundPhotoToggle.onValueChanged.AddListener(LegendToggleValueChanged);
        _legendShow.onClick.AddListener(LegendShowClick);
        _resetRobot.onClick.AddListener(ResetRobotClick);
        _robotDebugClose.onClick.AddListener(RobotDebugCloseClick);
        _robotDebug.onClick.AddListener(RobotDebugClick);
        _robotDebugSelectRobot.onValueChanged.AddListener(OnRobotDebugListValueChanged);

        _driveRobotColorBlock = _driveRobot.colors;

        _driveRobotStopColorBlock = _driveRobot.colors;

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
        _layerNumberText.text = MazeMapController.Instance.CurrentActiveLevel.ToString();

        CurrentUIState = UIState.Navigation;
        CurrentRobotDrivingUIState = RobotDrivingUIState.NoRobotSelected;
        MazeMapController.Instance.OnFinishedGeneratingCampus += OnFinishedGeneratingCampus;
        _robotDebugListItems = new List<RobotDebugListItem>();
    }

    void Update()
    {
        if (_refreshingRobotList)
        {
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
        if (_shouldUpdateRobotList)
        {
            UpdateRobotList();
            _shouldUpdateRobotList = false;
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
        _robotPanel.SetActive(false);
        _legendPanel.SetActive(false);
        _robotDebugPanel.SetActive(false);
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
        _selectRobot.options.Add(new Dropdown.OptionData("None"));
        _selectRobot.RefreshShownValue();
        _selectRobot.interactable = true;
        _robotList.interactable = true;
        _goToBuilding.interactable = true;
        _buildingName.interactable = true;
        _layerUp.interactable = true;
        _layerDown.interactable = true;
        _addFiducial.interactable = true;
        _updateFiducial.interactable = true;
        _deleteFiducial.interactable = true;
        _saveFiducials.interactable = true;
        if (CurrentUIState == UIState.Navigation)
            CurrentUIState = UIState.Navigation;
    }

    private void Cleanup()
    {
        MazeMapController.Instance.ClearAll();
        WaypointController.Instance.ClearAllWaypoints();
        RobotMasterController.Instance.DisconnectAllRobots();
        _selectRobot.value = 0;
        _selectRobot.RefreshShownValue();
        OnSelectedRobotValueChanged(0);
    }

    private IEnumerator GenerateCampus()
    {
        Cleanup();
        int id = -1;
        _loadingFill.fillAmount = 0;
        CurrentUIState = UIState.Loading;

        yield return new WaitForSeconds(2);
        if (int.TryParse(_campusId.text, out id))
        {
            MazeMapController.Instance.GenerateCampus(id);
        }
        else
        {
            Debug.LogError("Cannot parse CampusId to int");
        }
    }

    #region ButtonClickEvents

    private void GenerateCampusButtonOnClick()
    {
        StartCoroutine(GenerateCampus());
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

    private void ClearAllWaypointsOnClick()
    {
        WaypointController.Instance.ClearAllWaypoints();
    }

    private void ReturnRobotToBase()
    {
        //TODO: Hardcoded
        MazeMapController.Instance.GetPath(RobotMasterController.SelectedRobot.transform.position.ToUTM().ToWGS84(), new GeoPointWGS84() {latitude = 55.78268988306574, longitude = 12.514101387003798});
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
            RobotMasterController.SelectedRobot.MovePath(WaypointController.Instance.GetPath());
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
        _addFidLon.text = "0.0";
        _addFidAlt.text = "0.0";
        _addFidLat.text = "0.0";
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
                FiducialController.Instance.FinalizeUpdate();
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
            case UIState.SetRobotPosition:
            case UIState.SetRobotOrientation:
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
            case UIState.SetRobotPosition:
            case UIState.SetRobotOrientation:
                CurrentUIState = UIState.Navigation;
                UnregisterMouseClick();
                break;
        }
    }

    private void PauseRobotClick()
    {
        if (_isPaused)
            RobotMasterController.SelectedRobot.ResumePath();
        else
            RobotMasterController.SelectedRobot.PausePath();

        IsPaused = !IsPaused;
    }

    private void RobotListClick()
    {
        CurrentUIState = UIState.RobotList;
    }

    private void RobotListRefreshClick()
    {
        _refreshingRobotList = true;
        _robotsLeftToRefresh = RobotMasterController.Robots.Count;
        _refreshButton.ShouldRotate = true;
        foreach (KeyValuePair<RobotMasterController.Robot, RobotListItem> pair in _robotListItems)
        {
            if (!pair.Value.Connected)
                pair.Value.ResetListItem();
        }
        RobotMasterController.Instance.RefreshRobotConnections();
    }

    private void RobotListCloseClick()
    {
        CurrentUIState = UIState.Navigation;
    }

    private void RobotListConnectClick(bool shouldConnect, string name)
    {
        if (shouldConnect)
        {
            RobotMasterController.Instance.ConnectToRobot(name);
        }
        else
            RobotMasterController.Instance.DisconnectRobot(name);
    }

    private void RobotDebugClick()
    {
        CurrentUIState = UIState.RobotDebug;
    }

    private void RobotDebugCloseClick()
    {
        CurrentUIState = UIState.Navigation;
    }

    private void OverrideRobotPositionClick()
    {
        CurrentUIState = UIState.SetRobotPosition;
        RegisterMouseClick();
    }

    private void SaveFiducialsToFileClick()
    {
        FiducialController.Instance.SaveFiducials();
    }

    private void LegendShowClick()
    {
        if (_isLegendAnimating) return;
        if (_isLegendVisible)
        {
            iTween.MoveBy(_legendPanel, iTween.Hash("y", -75 * _canvas.scaleFactor, "time", 0.5f, "oncomplete", "OnLegendAnimationDone", "oncompletetarget", gameObject));
        }
        else
        {
            iTween.MoveBy(_legendPanel, iTween.Hash("y", 75 * _canvas.scaleFactor, "time", 0.5f, "oncomplete", "OnLegendAnimationDone", "oncompletetarget", gameObject));
        }
        _isLegendVisible = !_isLegendVisible;
        _isLegendAnimating = true;
    }

    private void ResetRobotClick()
    {
        RobotMasterController.SelectedRobot.ResetRobot();
    }

    #endregion

    #region OnValueChangeEvents

    private void OnAddFiducialPositionValuesChanged(string value)
    {
        Vector3 position = new Vector3(float.Parse(_addFidPosX.text, CultureInfo.InvariantCulture),
            float.Parse(_addFidPosY.text, CultureInfo.InvariantCulture), float.Parse(_addFidPosZ.text, CultureInfo.InvariantCulture));
        Vector3 rotation = new Vector3(float.Parse(_addFidRotX.text, CultureInfo.InvariantCulture),
            float.Parse(_addFidRotY.text, CultureInfo.InvariantCulture), float.Parse(_addFidRotZ.text, CultureInfo.InvariantCulture));
        int id = int.Parse(_addFidId.text);
        GeoPointWGS84 gps = position.ToUTM().ToWGS84();
        _addFidLon.text = gps.longitude.ToString(CultureInfo.InvariantCulture);
        _addFidAlt.text = gps.altitude.ToString(CultureInfo.InvariantCulture);
        _addFidLat.text = gps.latitude.ToString(CultureInfo.InvariantCulture);

        if (CurrentUIState == UIState.PlacingFiducial)
            FiducialController.Instance.PlaceOrUpdateNewFiducial(int.Parse(_addFidId.text), position, rotation);
        else if (CurrentUIState == UIState.UpdatingFiducial)
            FiducialController.Instance.UpdateFiducial(id, position, rotation);
    }

    private void OnAddFiducialGPSValuesChanged(string value)
    {
        Vector3 rotation = new Vector3(float.Parse(_addFidRotX.text, CultureInfo.InvariantCulture),
            float.Parse(_addFidRotY.text, CultureInfo.InvariantCulture), float.Parse(_addFidRotZ.text, CultureInfo.InvariantCulture));
        int id = int.Parse(_addFidId.text);
        GeoPointWGS84 gps = new GeoPointWGS84
        {
            altitude = double.Parse(_addFidAlt.text, CultureInfo.InvariantCulture),
            latitude = double.Parse(_addFidLat.text, CultureInfo.InvariantCulture),
            longitude = double.Parse(_addFidLon.text, CultureInfo.InvariantCulture)
        };

        Vector3 unityPos = gps.ToUTM().ToUnity();
        _addFidPosX.text = unityPos.x.ToString(CultureInfo.InvariantCulture);
        _addFidPosY.text = unityPos.y.ToString(CultureInfo.InvariantCulture);
        _addFidPosZ.text = unityPos.z.ToString(CultureInfo.InvariantCulture);

        if (CurrentUIState == UIState.PlacingFiducial)
            FiducialController.Instance.PlaceOrUpdateNewFiducial(int.Parse(_addFidId.text), unityPos, rotation);
        else if (CurrentUIState == UIState.UpdatingFiducial)
            FiducialController.Instance.UpdateFiducial(id, unityPos, rotation);
    }

    private void OnAddFiducialRotationValuesChanged(string value)
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
        ROSController selectedRobot = RobotMasterController.Instance.GetRosControllerFromName(_selectRobot.options[newIndex].text);
        UpdateUI(selectedRobot);
        RobotMasterController.Instance.SelectRobot(selectedRobot);
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

    private void LegendToggleValueChanged(bool isOn)
    {
        SorroundPhotoController.Instance.SetCameraPositionVisibility(_legendSorroundPhotoToggle.isOn);
        FiducialController.Instance.SetFiducialVisibility(_legendFiducialToggle.isOn);
    }

    private void OnRobotDebugListValueChanged(int newIndex)
    {
        string selectedRobot = _robotDebugSelectRobot.options[newIndex].text;
        InitialiseRobotDebugPanel(RobotMasterController.Instance.GetRosControllerFromName(selectedRobot).GetRobotLogs());
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
                case UIState.SetRobotPosition:
                    _robotOverridePosition = hit.point;
                    CurrentUIState = UIState.SetRobotOrientation;
                    break;
                case UIState.SetRobotOrientation:
                    _robotOverrideOrientation = Quaternion.LookRotation(hit.point - _robotOverridePosition, Vector3.up);
                    FinalizeRobotPositionOverride();
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

        GeoPointWGS84 gps = fiducialToUpdate.position.ToUTM().ToWGS84();
        _addFidLon.text = gps.longitude.ToString(CultureInfo.InvariantCulture);
        _addFidAlt.text = gps.altitude.ToString(CultureInfo.InvariantCulture);
        _addFidLat.text = gps.latitude.ToString(CultureInfo.InvariantCulture);

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

        GeoPointWGS84 gps = clickPos.ToUTM().ToWGS84();
        _addFidLon.text = gps.longitude.ToString(CultureInfo.InvariantCulture);
        _addFidAlt.text = gps.altitude.ToString(CultureInfo.InvariantCulture);
        _addFidLat.text = gps.latitude.ToString(CultureInfo.InvariantCulture);

        Vector3 position = new Vector3(float.Parse(_addFidPosX.text, CultureInfo.InvariantCulture),
            float.Parse(_addFidPosY.text, CultureInfo.InvariantCulture), float.Parse(_addFidPosZ.text, CultureInfo.InvariantCulture));
        FiducialController.Instance.PlaceOrUpdateNewFiducial(int.Parse(_addFidId.text), position, Vector3.zero);
    }

    public void PhotoClicked()
    {
        CurrentUIState = UIState.SorroundPhoto;
    }

    #endregion

    private void InitialiseRobotDebugPanel(List<RobotLog> logs)
    {
        foreach (RobotDebugListItem item in _robotDebugListItems)
        {
            DestroyImmediate(item.gameObject);
        }
        _robotDebugListItems = new List<RobotDebugListItem>();
        foreach (RobotLog log in logs)
        {
            RobotDebugListItem item = Instantiate(_robotDebugMessagePrefab, _robotDebugContentsParent).GetComponent<RobotDebugListItem>();
            item.Initialise(log.Timestamp, log.Message);
            _robotDebugListItems.Add(item);
        }
    }

    private void OnLegendAnimationDone()
    {
        _isLegendAnimating = false;
    }

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

    private void FinalizeRobotPositionOverride()
    {
        RobotMasterController.SelectedRobot.OverridePositionAndOrientation(_robotOverridePosition, _robotOverrideOrientation);
        UnregisterMouseClick();
        CurrentUIState = UIState.Navigation;
    }

    private IEnumerator RobotDebugListen()
    {
        while (true)
        {
            string selectedRobot = _robotDebugSelectRobot.options[_robotDebugSelectRobot.value].text;
            InitialiseRobotDebugPanel(RobotMasterController.Instance.GetRosControllerFromName(selectedRobot).GetRobotLogs());
            yield return new WaitForSeconds(1);
        }
    }

    public void SetDriveMode(bool isDriving)
    {
        _driveRobot.colors = isDriving ? _driveRobotStopColorBlock : _driveRobotColorBlock;
        _driveRobotText.text = isDriving ? "Stop" : "Drive";
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
        //TODO: Load path of selected robot if moving
        if (robot == null)
        {
            CurrentRobotDrivingUIState = RobotDrivingUIState.NoRobotSelected;
            WaypointController.Instance.ClearAllWaypoints();
            _overrideRobotPosition.interactable = false;
            _clearAllWaypoints.interactable = false;
            _routeName.interactable = false;
            _loadRoute.interactable = false;
            _saveRoute.interactable = false;
            _resetRobot.interactable = false;
        }
        else
        {
            _overrideRobotPosition.interactable = true;
            _clearAllWaypoints.interactable = true;
            _routeName.interactable = true;
            _resetRobot.interactable = true;
            if (robot.CurrenLocomotionType == ROSController.RobotLocomotionType.DIRECT)
                CurrentRobotDrivingUIState = RobotDrivingUIState.RobotStopped;
            else
            {
                if (robot.CurrentRobotLocomotionState == ROSController.RobotLocomotionState.MOVING)
                    CurrentRobotDrivingUIState = RobotDrivingUIState.RobotDriving;
                else if (robot.CurrentRobotLocomotionState == ROSController.RobotLocomotionState.STOPPED)
                    CurrentRobotDrivingUIState = RobotDrivingUIState.RobotStopped;
            }
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
            listItem.Initialise(robot.Name, robot.Uri, robot.Port, robot.Connected);
            listItem.OnConnectClicked += RobotListConnectClick;
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
        _shouldUpdateRobotList = true;
    }

    public void AddRobotToList(string robotName)
    {
        if (_selectRobot.options.Count <= 1)
            _robotDebug.interactable = true;
        _selectRobot.options.Add(new Dropdown.OptionData(robotName));
        _selectRobot.RefreshShownValue();
    }

    public void RemoveRobotFromList(string robotName)
    {
        for (int i = 0; i < _selectRobot.options.Count; i++)
        {
            if (_selectRobot.options[i].text == robotName)
            {
                _selectRobot.options.RemoveAt(i);
                if (i == _selectRobot.value)
                {
                    int robotValue = RobotMasterController.ActiveRobots.Count > 0 ? 1 : 0;

                    _selectRobot.value = robotValue;
                    _selectRobot.RefreshShownValue();
                    OnSelectedRobotValueChanged(robotValue);
                }
                return;
            }
        }
        _selectRobot.RefreshShownValue();
        if (_selectRobot.options.Count <= 1)
            _robotDebug.interactable = false;
    }

    public void SelectDefaultRobot()
    {
        if (_selectRobot.options.Count > 1)
        {
            _selectRobot.value = 1;
        }
        else
        {
            _selectRobot.value = 0;
        }
        _selectRobot.RefreshShownValue();
    }

}