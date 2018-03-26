using System.Collections.Generic;
using System.IO;
using System.Linq;
using ROSBridgeLib;
using UnityEngine;

public class RobotMasterController : MonoBehaviour
{
    public static RobotMasterController Instance { get; private set; }

    private static ROSController _selectedRobot;
    public static ROSController SelectedRobot
    {
        get { return _selectedRobot; }
        private set
        {
            if (_selectedRobot == null)
            {
                //PlayerUIController.Instance.SetDriveMode();
            }

            _selectedRobot = value;
        }
    }

    //Master URI, Corresponding Socket
    private readonly Dictionary<string, ROSBridgeWebSocketConnection> _rosBridges = new Dictionary<string, ROSBridgeWebSocketConnection>();
    private Dictionary<string, RobotConfigFile> _robotConfigs;
    private string _configPath;
    private string _robotPrefabPath;
    private Dictionary<string, ROSController> _activeRobots;

    void Awake()
    {
        Instance = this;
        _configPath = Application.streamingAssetsPath + "/Config/Robots/";
        _robotPrefabPath = "Prefabs/Robots/";
    }

    void Start()
    {
        _activeRobots = new Dictionary<string, ROSController>();
        Initialise();
    }

    void Update()
    {
        foreach (KeyValuePair<string, ROSBridgeWebSocketConnection> bridge in _rosBridges)
        {
            bridge.Value.Render();
        }
    }

    void OnApplicationQuit()
    {
        foreach (KeyValuePair<string, ROSBridgeWebSocketConnection> bridge in _rosBridges)
        {
            bridge.Value.Disconnect();    
        }
    }

    private void Initialise()
    {
        _robotConfigs = new Dictionary<string, RobotConfigFile>();
        string[] robotConfigPaths = Directory.GetFiles(_configPath, "*.json");

        foreach (string path in robotConfigPaths)
        {
            string robotName = Path.GetFileNameWithoutExtension(path);
            string robotFileJson = File.ReadAllText(path);
            RobotConfigFile robotFile = JsonUtility.FromJson<RobotConfigFile>(robotFileJson);
            _robotConfigs.Add(robotName, robotFile);
        }
    }

    public List<string> GetRobotNames(int campusId)
    {
        return _robotConfigs.Where(pair => pair.Value.Campuses.Contains(campusId)).Select(pair => pair.Key).ToList();
    }

    public ROSController LoadRobot(string robotName)
    {
        if (robotName == "No Robot Selected")
        {
            SelectedRobot = null;
            return null;
        }
        RobotConfigFile config;
        if (_robotConfigs.TryGetValue(robotName, out config))
        {
            ROSBridgeWebSocketConnection rosBridge;
            string path = string.Format("{0}:{1}", config.RosMasterUri, config.RosMasterPort);
            if (!_rosBridges.TryGetValue(path, out rosBridge))
            {
                string uri = config.RosMasterUri;
                if (!uri.Contains("ws://"))
                    uri = "ws://" + uri;
                rosBridge = new ROSBridgeWebSocketConnection(uri, config.RosMasterPort);
                rosBridge.Connect();
                _rosBridges.Add(path, rosBridge);
            }

            ROSController robot;
            if (_activeRobots.TryGetValue(robotName, out robot))
            {
                SelectedRobot = robot;
            }
            else
            {
                ROSController rosController = (Instantiate(Resources.Load(_robotPrefabPath + robotName)) as GameObject).GetComponent<ROSController>();
                rosController.InitialiseRobot(rosBridge, config);
                _activeRobots.Add(robotName, rosController);
                SelectedRobot = rosController;
            }

            return SelectedRobot;
        }
        else
        {
            return null;
        }
    }

}
