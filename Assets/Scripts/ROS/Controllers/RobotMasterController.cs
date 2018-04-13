using System;
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

    //Master URI:Port, Corresponding Robot
    public static Dictionary<string, Robot> Robots { get; private set; }
    private Dictionary<string, RobotConfigFile> _robotConfigs;
    private List<ROSController> _activeRobots;
    private string _configPath;
    private string _robotPrefabPath;

    void Awake()
    {
        Instance = this;
        Robots = new Dictionary<string, Robot>();
        _configPath = Application.streamingAssetsPath + "/Config/Robots/";
        _robotPrefabPath = "Prefabs/Robots/";
    }

    void Start()
    {
        MazeMapController.Instance.OnFinishedGeneratingCampus += LoadRobotsFromCampus;
    }

    void Update()
    {
        foreach (KeyValuePair<string, Robot> bridge in Robots)
        {
            if (bridge.Value.IsActive)
                bridge.Value.RosBridge.Render();
        }
    }

    void OnApplicationQuit()
    {
        foreach (KeyValuePair<string, Robot> bridge in Robots)
        {
            if (bridge.Value.IsActive)
                bridge.Value.RosBridge.Disconnect();
        }
    }

    private void LoadRobotsFromCampus(int campusId)
    {
        if (_activeRobots != null)
        {
            foreach (ROSController robot in _activeRobots)
            {
                robot.Destroy();
            }
        }

        _activeRobots = new List<ROSController>();
        Robots = new Dictionary<string, Robot>();
        _robotConfigs = new Dictionary<string, RobotConfigFile>();
        string[] robotConfigPaths = Directory.GetFiles(_configPath, "*.json");
        foreach (string path in robotConfigPaths)
        {
            string robotName = Path.GetFileNameWithoutExtension(path);
            string robotFileJson = File.ReadAllText(path);
            RobotConfigFile robotFile = JsonUtility.FromJson<RobotConfigFile>(robotFileJson);

            if (!robotFile.Campuses.Contains(campusId)) continue;

            _robotConfigs.Add(robotName, robotFile);

            string uri = robotFile.RosMasterUri;
            if (!uri.Contains("ws://"))
                uri = "ws://" + uri;
            string uriPort = string.Format("{0}:{1}", uri, robotFile.RosMasterPort);
            
            ROSBridgeWebSocketConnection rosBridge = new ROSBridgeWebSocketConnection(uri, robotFile.RosMasterPort);

            Robot robot = new Robot(robotFile.Campuses, rosBridge, robotName, uri, robotFile.RosMasterPort, false);
            Robots.Add(uriPort, robot);
            robot.RosBridge.Connect(ConnectionResult);
        }

        PlayerUIController.Instance.LoadRobots(Robots.Select( robot => robot.Value).ToList());
    }

    private void ConnectionResult(string uriPort, bool isAlive)
    {
        Robots[uriPort].IsActive = isAlive;
        PlayerUIController.Instance.RobotRefreshed();
    }

    public List<string> GetRobotNames(int campusId)
    {
        return _robotConfigs.Where(pair => pair.Value.Campuses.Contains(campusId)).Select(pair => pair.Key).ToList();
    }

    public void RobotLostConnection(ROSController robot)
    {
        Debug.LogError("Robot [" + robot.gameObject.name + "] lost connection!");
    }

    public void RefreshRobotConnections()
    {
        foreach (KeyValuePair<string, Robot> pair in Robots)
        {
            if (pair.Value.IsActive)
            {
                PlayerUIController.Instance.RobotRefreshed();
                continue;
            }

            pair.Value.RosBridge.Connect(ConnectionResult);
        }
    }

    public class Robot
    {
        public int[] Campuses;
        public ROSBridgeWebSocketConnection RosBridge;
        public string Name;
        public string Uri;
        public int Port;
        public bool IsActive;

        public Robot(int[] campuses, ROSBridgeWebSocketConnection rosBridge, string name, string uri, int port, bool isActive)
        {
            Campuses = campuses;
            RosBridge = rosBridge;
            Name = name;
            Uri = uri;
            Port = port;
            IsActive = isActive;
        }
    }

}
