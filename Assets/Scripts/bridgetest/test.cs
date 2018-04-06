using System.Collections;
using NUnit.Framework.Constraints;
using ROSBridgeLib;
using ROSBridgeLib.auv_msgs;
using ROSBridgeLib.fiducial_msgs;
using ROSBridgeLib.geographic_msgs;
using ROSBridgeLib.geometry_msgs;
using ROSBridgeLib.nav_msgs;
using ROSBridgeLib.sensor_msgs;
using ROSBridgeLib.std_msgs;
using UnityEngine;
using UnityEngine.Networking;

public class test : MonoBehaviour{

    private ROSBridgeWebSocketConnection ros = null;
    private float timer;
    private float pollRate = 2;
    private ROSGenericPublisher _genericPub;
    private ROSGenericSubscriber<UInt64MultiArrayMsg> _genericSub;
    private bool _running = false;


    void Start () {
	    Initialise();
        
        Application.runInBackground = true;
    }
	
	void Update ()
	{
	    if (!_running) return;
		
	    timer -= Time.deltaTime;
	    ros.Render();

        if (timer <= 0)
	    {
	        timer = pollRate;

            //_genericPub.PublishData(msg);
        }
    }

    private void Initialise()
    {
        ros = new ROSBridgeWebSocketConnection("ws://192.168.255.40", 9090);
        _genericPub = new ROSGenericPublisher(ros, "/u64a", UInt64MultiArrayMsg.GetMessageType());
        _genericSub = new ROSGenericSubscriber<UInt64MultiArrayMsg>(ros, "/u64a", UInt64MultiArrayMsg.GetMessageType(), msg => new UInt64MultiArrayMsg(msg));
        _genericSub.OnDataReceived += OnDataReceived;
        ros.Connect();
        ros.Render();
        StartCoroutine(WaitForRunning());
    }

    private void OnDataReceived(ROSBridgeMsg msg)
    {
        Debug.Log("Got Message " + msg.ToString());
    }

    private IEnumerator WaitForRunning()
    {
        yield return new WaitForSeconds(1);
        _running = true;
    }

    void OnApplicationQuit()
    {
        ros.Disconnect();
    }

}
