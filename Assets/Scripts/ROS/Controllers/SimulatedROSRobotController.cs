using System;
using System.Collections.Generic;
using Ros_CSharp;
using UnityEngine;

public class SimulatedROSRobotController : ROSController 
{
    [SerializeField] private float _dataSendRateMs = 50;
    [SerializeField] private string _ROS_MASTER_URI = "127.0.0.1:11311";
    [SerializeField] private bool _alwaysGenerateRobot;

    private float _dataSendTimer;
    private SensorBusController _sensorBusController;
    private Dictionary<Type, ROSAgent> _rosAgents;
    private List<Type> _agentsWaitingToStart;
    private bool _robotInitialised;
    private Transform _robot;

    internal string _robotDescription = "";

    public static SimulatedROSRobotController Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        _rosAgents = new Dictionary<Type, ROSAgent>();
        _agentsWaitingToStart = new List<Type>();
    }

    void Start()
    {
        _sensorBusController = SensorBusController.Instance;
        if (!string.IsNullOrEmpty(_ROS_MASTER_URI)) {
            if (!_ROS_MASTER_URI.Contains("http://"))
                _ROS_MASTER_URI = "http://" + _ROS_MASTER_URI;
            ROS.ROS_MASTER_URI = _ROS_MASTER_URI;
        }
        StartROS();
    }

    void Update()
    {
        if (!(ROS.ok || ROS.isStarted()))
            return;
        if (!_robotInitialised)
            InitialiseRobot();
        if (_agentsWaitingToStart.Count > 0)
        {
            foreach (Type type in _agentsWaitingToStart)
            {
                StartAgent(type);
            }
            _agentsWaitingToStart = new List<Type>();
        }
        _dataSendTimer += Time.deltaTime;
        if (_dataSendTimer >= _dataSendRateMs / 1000f)
            TransmitSensorData();
    }
    
    private void InitialiseRobot()
    {
        if (Param.has("robot_description"))
        {
            Debug.Log("Generating robot from robot description");
            Param.get("robot_description", ref _robotDescription);
            GenerateRobot(_robotDescription);
        }
        else 
            Debug.Log("---No robot description available - could not automatically generate robot---");
        _robotInitialised = true;
    }

    private void TransmitSensorData()
    {
        _dataSendTimer = 0;
        foreach (SensorBus sensorBus in _sensorBusController.SensorBusses)
        {
            if (!_rosAgents.ContainsKey(sensorBus.ROSAgentType)) continue;
            _rosAgents[sensorBus.ROSAgentType].PublishData(sensorBus.GetSensorData());
        }
    }

    private void GenerateRobot(string robotDescription)
    {
        _robot = RobotUrdfUtility.GenerateRobotGameObjectFromDescription(robotDescription, _alwaysGenerateRobot).transform;
    }

    public void StartAgent(Type agentType)
    {
        if (!(ROS.ok || ROS.isStarted()))
        {
            _agentsWaitingToStart.Add(agentType);
            return;
        }
        ROSAgent agent = (ROSAgent) Activator.CreateInstance(agentType);
        agent.StartAgent(ROSAgent.AgentJob.Publisher, _clientNamespace);
        _rosAgents.Add(agentType, agent);
    }
    
}
