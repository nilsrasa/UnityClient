using System;
using System.Collections;
using System.Collections.Generic;
using Messages;
using Ros_CSharp;
using UnityEngine;
using XmlRpc_Wrapper;

public class SimulatedROSRobot : MonoBehaviour
{

    [SerializeField] private float _dataSendRateMs = 50;
    [SerializeField] private string _ROS_MASTER_URI = "127.0.0.1:11311";

    public const string NAMESPACE_VRClient = "/vrclient";
    public const string NAMESPACE_ARLOBOT = "/arlobot";

    private float _dataSendTimer;
    private SensorBusController _sensorBusController;
    private Dictionary<Type, ROSAgent> _rosAgents;
    private List<Type> _agentsWaitingToStart;

    public static SimulatedROSRobot Instance
    {
        get
        {
            if (_instance == null) {
                GameObject go = new GameObject();
                go.name = "ROSController";
                _instance = go.AddComponent<SimulatedROSRobot>();
            }
            return _instance;

        }
        private set
        {
            _instance = value;
        }
    }

    private static SimulatedROSRobot _instance;

    private ROSLocomotion _rosLocomotion;
    private ROSUltrasound _rosUltrasound;

    void Awake()
    {
        _instance = this;
        _rosAgents = new Dictionary<Type, ROSAgent>();
        _agentsWaitingToStart = new List<Type>();
    }

    void Start() {
        _sensorBusController = SensorBusController.Instance;
        ROS.ROS_MASTER_URI = _ROS_MASTER_URI;
        StartROS();
    }

    void Update()
    {
        if (!(ROS.ok || ROS.isStarted()))
            return;
        if (_agentsWaitingToStart.Count > 0) {
            foreach (Type type in _agentsWaitingToStart) {
                StartAgent(type);
            }
            _agentsWaitingToStart = new List<Type>();
        }
        _dataSendTimer += Time.deltaTime;
        if (_dataSendTimer >= _dataSendRateMs/1000f)
            TransmitSensorData();
        
    }

    void OnApplicationQuit() {
        if (ROS.ok || ROS.isStarted())
            StopROS();
    }

    private void TransmitSensorData()
    {
        _dataSendTimer = 0;
        foreach (SensorBus sensorBus in _sensorBusController.SensorBusses)
        {
            if (!_rosAgents.ContainsKey(sensorBus.ROSAgentType)) continue;
            IRosMessage message = sensorBus.GetSensorData();
            _rosAgents[sensorBus.ROSAgentType].PublishData(sensorBus.GetSensorData());
        }

    }

    public void StartAgent(Type agentType)
    {
        if (!(ROS.ok || ROS.isStarted()))
        {
            _agentsWaitingToStart.Add(agentType);
            return;
        }
        ROSAgent agent = (ROSAgent) Activator.CreateInstance(agentType);
        agent.StartAgent(ROSAgent.AgentJob.Publisher);
        _rosAgents.Add(agentType, agent);
    }

    public void StartROS() {
        Debug.Log("---Starting ROS---");
        if (ROS.isStarted()) return;
        ROS.Init(new string[0], "VRClient");
        XmlRpcUtil.SetLogLevel(XmlRpcUtil.XMLRPC_LOG_LEVEL.ERROR);
    }

    public void StopROS() {
        Debug.Log("---Stopping ROS---");
        ROS.shutdown();
        //ROS.waitForShutdown(); Do we need this? 
    }
}
