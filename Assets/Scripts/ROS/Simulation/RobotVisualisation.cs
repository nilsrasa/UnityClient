using System;
using System.Collections.Generic;
using Messages;
using Ros_CSharp;
using UnityEngine;
using XmlRpc_Wrapper;

public class RobotVisualisation : MonoBehaviour
{
    [SerializeField] private bool _runConjoinedWithSim;
    [SerializeField] private float _dataSendRateMs = 50;
    [SerializeField] private string _ROS_MASTER_URI = "127.0.0.1:11311";

    public const string NAMESPACE_VRClient = "/vrclient";
    public const string NAMESPACE_ARLOBOT = "/arlobot";

    private float _dataSendTimer;
    private SensorRepresentationBusController _sensorRepresentationBusController;
    private Dictionary<Type, ROSAgent> _rosAgents;
    private List<Type> _agentsWaitingToStart;

    public static RobotVisualisation Instance
    {
        get
        {
            if (_instance == null) {
                GameObject go = new GameObject();
                go.name = "ROSController";
                _instance = go.AddComponent<RobotVisualisation>();
            }
            return _instance;

        }
        private set
        {
            _instance = value;
        }
    }

    private static RobotVisualisation _instance;

    private ROSLocomotion _rosLocomotion;

    void Awake() {
        _instance = this;
        _rosAgents = new Dictionary<Type, ROSAgent>();
        _agentsWaitingToStart = new List<Type>();
    }

    void Start() {
        _sensorRepresentationBusController = SensorRepresentationBusController.Instance;
        if (!_runConjoinedWithSim)
            StartROS();
    }

    void Update() {
        if (!(ROS.ok || ROS.isStarted()))
            return;
        if (_agentsWaitingToStart.Count > 0) {
            foreach (Type type in _agentsWaitingToStart) {
                StartAgent(type);
            }
            _agentsWaitingToStart = new List<Type>();
        }
    }

    void OnApplicationQuit() {
        if (ROS.ok || ROS.isStarted() && !_runConjoinedWithSim)
            StopROS();
    }

    void DataReceived(ROSAgent sender, IRosMessage data)
    {
        _sensorRepresentationBusController.HandleData(sender, data);
    }

    public void StartAgent(Type agentType) {
        if (!(ROS.ok || ROS.isStarted())) {
            _agentsWaitingToStart.Add(agentType);
            return;
        }
        ROSAgent agent = (ROSAgent)Activator.CreateInstance(agentType);
        agent.StartAgent(ROSAgent.AgentJob.Subscriber);
        _rosAgents.Add(agentType, agent);
        agent.DataWasReceived += DataReceived;
    }

    public void StartROS()
    {
        if (!_runConjoinedWithSim)
            return;
        Debug.Log("---Starting ROS---");
        if (!string.IsNullOrEmpty(_ROS_MASTER_URI))
        {
            if (!_ROS_MASTER_URI.Contains("http://"))
                _ROS_MASTER_URI = "http://" + _ROS_MASTER_URI;
        }
        if (ROS.isStarted()) return;
        ROS.Init(new string[0], "VRClient");
        XmlRpcUtil.SetLogLevel(XmlRpcUtil.XMLRPC_LOG_LEVEL.ERROR);
    }

    public void StopROS()
    {
        if (!_runConjoinedWithSim)
            return;

        Debug.Log("---Stopping ROS---");
        ROS.shutdown();
        //ROS.waitForShutdown(); Do we need this? 
    }

    public void Move(Vector2 movement) {
        _rosLocomotion.PublishData(movement);
    }

}
