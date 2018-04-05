using System.Collections;
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
    private ROSGenericSubscriber<ImageMsg> _genericSub;
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
            HeaderMsg header = new HeaderMsg(0, new TimeMsg(0, 0), "0" );
            PointMsg point = new PointMsg(0, 1, 2);
            QuaternionMsg quaternion = new QuaternionMsg(0, 1, 2, 3);
	        PoseMsg pose = new PoseMsg(point, quaternion);
            Vector3Msg vector = new Vector3Msg(0, 1, 2);
            TwistMsg twist = new TwistMsg(vector, vector);
            PoseWithCovarianceMsg poseCo = new PoseWithCovarianceMsg(pose);
            PoseStampedMsg poseStamp = new PoseStampedMsg(header, pose);


            ImageMsg msg = new ImageMsg(header, 0, 0, "ads", true, 0, new byte[100]);
            _genericPub.PublishData(msg);
        }
    }

    private void Initialise()
    {
        ros = new ROSBridgeWebSocketConnection("ws://192.168.255.40", 9090);
        _genericPub = new ROSGenericPublisher(ros, "/testImage3", ImageMsg.GetMessageType());
        _genericSub = new ROSGenericSubscriber<ImageMsg>(ros, "/testImage3", ImageMsg.GetMessageType(), msg => new ImageMsg(msg));
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
