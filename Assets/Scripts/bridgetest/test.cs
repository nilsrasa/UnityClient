using ROSBridgeLib;
using ROSBridgeLib.geometry_msgs;
using ROSBridgeLib.std_msgs;
using UnityEngine;

public class test : MonoBehaviour{

    private ROSBridgeWebSocketConnection ros = null;
    private float timer;
    private float pollRate = 2;
    private testpubpose _publisher;
    private testsubpose _subscriber;


    void Start () {
	    Initialise();
        
        Application.runInBackground = true;
    }
	
	void Update ()
	{
	    if (ros == null) return;
		
	    timer -= Time.deltaTime;
	    ros.Render();

        if (timer <= 0)
	    {
	        timer = pollRate;
	        _publisher.PublishData(new StringMsg("LELLEREN"));
            //PointMsg point = new PointMsg(123, 123, 123);
            //QuaternionMsg quat = new QuaternionMsg(1, 2, 3, 4);
            //_publisher.PublishData(new PoseMsg(point, quat));
	        StringMsg msg = testsubpose.TryGetReceivedMessage();
            if (msg != null)
                Debug.Log("Got Message " + msg._data);
                //Debug.Log("Got Message " + msg._position + ",  " + msg._orientation);
            else
            {
                Debug.Log("No Data");
            }
        }
    }

    private void Initialise()
    {
        ros = new ROSBridgeWebSocketConnection("ws://192.168.255.40", 9090);
        _publisher = new testpubpose(ros, "/testTopic2", StringMsg.GetMessageType());
        _subscriber = new testsubpose(ros, "/testTopic2", StringMsg.GetMessageType());
        ros.Connect();
        ros.Render();
    }

    void OnApplicationQuit()
    {
        ros.Disconnect();
    }

}
