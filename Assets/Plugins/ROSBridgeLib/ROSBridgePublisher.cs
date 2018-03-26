using UnityEngine;

namespace ROSBridgeLib {
	public abstract class ROSBridgePublisher : ROSAgent
	{
        public string GetMessageTopic()
	    {
	        return TopicName;
	    }

	    public string GetMessageType()
	    {
	        return MessageType;
	    }

	    public virtual void PublishData(ROSBridgeMsg msg)
	    {
	        if (ROSConnection.IsConnected)
	        {
                Debug.Log("Publishing to Topic [" + TopicName + "] Message [" + msg.ToYAMLString() + "]");
	            ROSConnection.Publish(TopicName, msg);
	        }
	    }

	    protected sealed override void StartAgent(ROSBridgeWebSocketConnection rosConnection, string topicName, string messageType)
	    {
	        TopicName = topicName;
	        MessageType = messageType;
	        ROSConnection = rosConnection;
	        ROSConnection.AddPublisher(this);
	    }

    }
}
