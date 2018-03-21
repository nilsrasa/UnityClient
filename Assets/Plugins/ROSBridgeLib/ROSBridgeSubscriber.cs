using SimpleJSON;

namespace ROSBridgeLib {
	public abstract class ROSBridgeSubscriber : ROSAgent
	{
	    public delegate void DataWasReceived(ROSBridgeMsg data);
	    public event DataWasReceived OnDataReceived;

	    public string GetMessageTopic()
	    {
	        return TopicName;
	    }

	    public string GetMessageType()
	    {
	        return MessageType;
	    }

	    public virtual void CallBack(ROSBridgeMsg msg)
	    {
	        if (OnDataReceived != null)
	            OnDataReceived(msg);
	    }

	    public abstract ROSBridgeMsg ParseMessage(JSONNode msg);

	    protected sealed override void StartAgent(ROSBridgeWebSocketConnection rosConnection, string topicName, string messageType)
	    {
	        TopicName = topicName;
	        MessageType = messageType;
	        ROSConnection = rosConnection;
            ROSConnection.AddSubscriber(this);
	    }
    }
}

