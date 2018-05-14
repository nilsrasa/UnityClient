using System;
using System.Collections.Generic;
using ROSBridgeLib;
using ROSBridgeLib.std_msgs;

public class RobotLogger : RobotModule
{
    private const int MaxNumberOfLogs = 50;

    private ROSGenericSubscriber<StringMsg> _subscriber;
    private List<RobotLog> _logs;
    private int _rollingOffset;

    private void LogReceivedData(ROSBridgeMsg msg)
    {
        RobotLog log = new RobotLog
        {
            Timestamp = DateTime.Now.ToString("T"),
            Message = ((StringMsg) msg)._data
        };
        if (_logs.Count < MaxNumberOfLogs)
        {
            _logs.Add(log);
        }
        else
        {
            _logs[_rollingOffset] = log;
            _rollingOffset++;
            if (_rollingOffset >= MaxNumberOfLogs)
                _rollingOffset = 0;
        }
    }

    public override void Initialise(ROSBridgeWebSocketConnection rosBridge)
    {
        base.Initialise(rosBridge);
        _logs = new List<RobotLog>();
        _subscriber = new ROSGenericSubscriber<StringMsg>(rosBridge, "/debug_output", StringMsg.GetMessageType(), (data) => new StringMsg(data));
        _subscriber.OnDataReceived += LogReceivedData;
    }

    public List<RobotLog> GetLogs()
    {
        List<RobotLog> returnList = _logs.GetRange(_rollingOffset, _logs.Count - _rollingOffset);
        if (_rollingOffset > 0)
            returnList.AddRange(_logs.GetRange(0, _rollingOffset));

        return returnList;
    }

    public override void StopModule()
    {
        _logs = new List<RobotLog>();
    }
}

[Serializable]
public struct RobotLog
{
    public string Timestamp;
    public string Message;
}
