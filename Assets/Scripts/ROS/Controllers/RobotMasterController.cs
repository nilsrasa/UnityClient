using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ros_CSharp;
using UnityEngine;

public class RobotMasterController : MonoBehaviour
{
    public static RobotMasterController Instance { get; set; }
    public static ROSController SelectedRobot { get; private set; }
    public static RobotConfigFile RobotConfigFile { get; private set; }

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
            GameObject robot = Instantiate(Resources.Load(_robotPrefabPath + robotName)) as GameObject;
            ROSController rosController = robot.GetComponentInChildren<ROSController>();
            rosController.StartRobot(config);
            SelectedRobot = rosController;
            return rosController;
        }
        else
        {
            return null;
        }
    }

}
