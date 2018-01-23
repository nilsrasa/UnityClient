using Messages.fiducial_msgs;
using Ros_CSharp;

/// <summary>
/// Ultrasound agent that sends or receives ultrasound data
/// </summary>
public class ROSFiducialMap : ROSAgent
{
    private const string TOPIC = "/fiducial_map";

    private NodeHandle _nodeHandle;
    private Subscriber<FiducialMapEntryArray> _subscriber;
    private Publisher<FiducialMapEntryArray> _publisher;
    private bool _isRunning;
    private AgentJob _job;
    
    ///<summary>
    ///Starts advertising loop
    /// <param name="job">Defines behaviour of agent</param>
    /// <param name="rosNamespace">Namespace the agent listens or writes to + topic</param>
    ///</summary>
    public override void StartAgent(AgentJob job) 
    {
        base.StartAgent(job);
        if (_isRunning) return;
        _nodeHandle = new NodeHandle();
        if(job == AgentJob.Subscriber)
            _subscriber = _nodeHandle.subscribe<FiducialMapEntryArray>(TOPIC, 10, ReceivedData);
        else if (job == AgentJob.Publisher)
            _publisher = _nodeHandle.advertise<FiducialMapEntryArray>(TOPIC, 1, false);
        _isRunning = true;
        _job = job;
        //Application.logMessageReceived += LogMessage;
    }

    public override void PublishData(object data)
    {
        if (_job != AgentJob.Publisher) return;
        FiducialMapEntryArray dataString = (FiducialMapEntryArray) data;
        _publisher.publish(dataString);
    }

    ///<summary>
    ///Stops advertising loop
    ///</summary>
    public void Stop() {
        if (!_isRunning) return;
        _nodeHandle.shutdown();
        _subscriber = null;
        _nodeHandle = null;
        
    }
}
