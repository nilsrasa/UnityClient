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

    //Master Robot hostname, Corresponding Robot
    public static Dictionary<string, Robot> Robots { get; private set; }
    private static Dictionary<string, ROSController> _activeRobots;
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
            foreach (KeyValuePair<string, ROSController> pair in _activeRobots)
            {
                pair.Value.Destroy();
            }
        }

        _activeRobots = new Dictionary<string, ROSController>();
        Robots = new Dictionary<string, Robot>();
        string[] robotConfigPaths = Directory.GetFiles(_configPath, "*.json");
        foreach (string path in robotConfigPaths)
        {
            string robotName = Path.GetFileNameWithoutExtension(path);
            string robotFileJson = File.ReadAllText(path);
            RobotConfigFile robotFile = JsonUtility.FromJson<RobotConfigFile>(robotFileJson);

            if (!robotFile.Campuses.Contains(campusId)) continue;

            string uri = robotFile.RosMasterUri;
            if (!uri.Contains("ws://"))
                uri = "ws://" + uri;
            
            ROSBridgeWebSocketConnection rosBridge = new ROSBridgeWebSocketConnection(uri, robotFile.RosMasterPort);

            Robot robot = new Robot(robotFile.Campuses, rosBridge, robotName, uri, robotFile.RosMasterPort, false, robotFile);
            Robots.Add(uri, robot);
            robot.RosBridge.Connect(ConnectionResult);
        }

        PlayerUIController.Instance.LoadRobots(Robots.Select( robot => robot.Value).ToList());
    }

    private void ConnectionResult(string uri, bool isAlive)
    {
        Robots[uri].IsActive = isAlive;
        PlayerUIController.Instance.RobotRefreshed();
        if (isAlive)
            Robots[uri].RosBridge.Disconnect();
    }

    public Robot GetRobotFromName(int campusId, string robotName)
    {
        foreach (KeyValuePair<string, Robot> pair in Robots)
        {
            if (pair.Value.Name == robotName && pair.Value.Campuses.Contains(campusId))
                return pair.Value;
        }
        return null;
    }

    public ROSController GetRosControllerFromName(int campusId, string robotName)
    {
        Robot robot = GetRobotFromName(campusId, robotName);
        if (robot == null)
            return null;
        ROSController rosController = null;
        if (_activeRobots.TryGetValue(robot.Uri, out rosController))
        {
            return rosController;
        }
        else
            return null;

    }

    public List<string> GetRobotNames(int campusId)
    {
        return Robots.Where(pair => pair.Value.Campuses.Contains(campusId)).Select(pair => pair.Value.Name).ToList();
    }

    public void RobotLostConnection(ROSController robot)
    {
        Debug.LogError("Robot [" + robot.gameObject.name + "] lost connection!");
        DisconnectRobot(robot.RobotConfig.RosMasterUri);
        PlayerUIController.Instance.UpdateRobotList();
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

    public void ConnectToRobot(string uri)
    {
        Robots[uri].Connected = true;
        Robots[uri].RosBridge.Connect();
        Robot robot = Robots[uri];
        GameObject robotObject = Instantiate(Resources.Load(_robotPrefabPath + robot.Name)) as GameObject;
        ROSController rosController = robotObject.GetComponent<ROSController>();
        rosController.InitialiseRobot(robot.RosBridge, robot.RobotConfig);
        _activeRobots.Add(uri, rosController);

        PlayerUIController.Instance.AddRobotToList(robot.Name);
    }

    public void DisconnectRobot(string uri)
    {
        Robots[uri].Connected = false;
        _activeRobots[uri].Destroy();
        _activeRobots.Remove(uri);

        PlayerUIController.Instance.RemoveRobotFromList(Robots[uri].Name);
    }

    public void SelectRobot(ROSController rosController)
    {
        SelectedRobot = rosController;
    }

    public class Robot
    {
        public int[] Campuses;
        public ROSBridgeWebSocketConnection RosBridge;
        public string Name;
        public string Uri;
        public int Port;
        public bool IsActive;
        public bool Connected;
        public RobotConfigFile RobotConfig;

        public Robot(int[] campuses, ROSBridgeWebSocketConnection rosBridge, string name, string uri, int port, bool isActive, RobotConfigFile robotConfig)
        {
            Campuses = campuses;
            RosBridge = rosBridge;
            Name = name;
            Uri = uri;
            Port = port;
            IsActive = isActive;
            RobotConfig = robotConfig;
        }
    }

}
