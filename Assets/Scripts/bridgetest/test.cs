using System.Collections;
using NUnit.Framework.Constraints;
using ROSBridgeLib;
using ROSBridgeLib.auv_msgs;
using ROSBridgeLib.fiducial_msgs;
using ROSBridgeLib.geometry_msgs;
using ROSBridgeLib.nav_msgs;
using ROSBridgeLib.std_msgs;
using UnityEngine;

public class test : MonoBehaviour{

    private ROSBridgeWebSocketConnection ros = null;
    private float timer;
    private float pollRate = 2;
    private ROSGenericPublisher _genericPub;
    private ROSGenericSubscriber<PathMsg> _genericSub;
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

            PointMsg point = new PointMsg(1, 2, 3);
            QuaternionMsg quat = new QuaternionMsg(1, 2, 3, 4);
	        PoseMsg pose = new PoseMsg(point, quat);
	        PoseWithCovarianceMsg posec = new PoseWithCovarianceMsg(pose);
            Vector3Msg vec3 = new Vector3Msg(1, 2, 3);
	        TwistMsg twist = new TwistMsg(vec3, vec3);
	        TwistWithCovarianceMsg twistc = new TwistWithCovarianceMsg(twist, new double[36]);
            HeaderMsg header = new HeaderMsg(1, new TimeMsg(1, 1), "0" );

            PoseStampedMsg ps = new PoseStampedMsg(header, pose);
            PathMsg msg = new PathMsg(header, new PoseStampedMsg[] { ps , ps , ps });
            _genericPub.PublishData(msg);
        }
    }

    private void Initialise()
    {
        ros = new ROSBridgeWebSocketConnection("ws://192.168.255.40", 9090);
        _genericPub = new ROSGenericPublisher(ros, "/pat", PathMsg.GetMessageType());
        _genericSub = new ROSGenericSubscriber<PathMsg>(ros, "/pat", PathMsg.GetMessageType(), msg => new PathMsg(msg));
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
