using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ROSBridgeLib;
using UnityEngine;
using UnityEngine.AI;

public class RobotMasterController : MonoBehaviour
{
    public static RobotMasterController Instance { get; set; }

    [SerializeField] private List<ROSController> _activeRobots;

    //Master URI, Corresponding Socket
    private readonly Dictionary<string, ROSBridgeWebSocketConnection> _rosBridges = new Dictionary<string, ROSBridgeWebSocketConnection>();
    private int _selectedRobotIndex;
    private Dictionary<string, RobotConfigFile> _robotConfigs;
    private string _configPath;
    private string _robotPrefabPath;

    void Awake()
    {
        Instance = this;
        _configPath = Application.streamingAssetsPath + "/Config/Robots/";
        _robotPrefabPath = "Prefabs/Robots/";

    }

    void Start()
    {
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
        RobotConfigFile config;
        if (_robotConfigs.TryGetValue(robotName, out config))
        {
            ROSBridgeWebSocketConnection rosBridge;
            string path = string.Format("{0}:{1}", config.RosMasterUri, config.RosMasterPort);
            if (!_rosBridges.TryGetValue(path, out rosBridge))
            {
                rosBridge = new ROSBridgeWebSocketConnection(config.RosMasterUri, config.RosMasterPort);
                rosBridge.Connect();
                _rosBridges.Add(path, rosBridge);
            }

            GameObject robot = Instantiate(Resources.Load(_robotPrefabPath + robotName)) as GameObject;
            ROSController rosController = robot.GetComponent<ROSController>();
            rosController.InitialiseRobot(rosBridge, config);

            return rosController;
        }
        else
        {
            return null;
        }
    }

}
