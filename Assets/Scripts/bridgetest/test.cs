using System.Collections;
using ROSBridgeLib;
using ROSBridgeLib.geometry_msgs;
using ROSBridgeLib.nav_msgs;
using ROSBridgeLib.sensor_msgs;
using ROSBridgeLib.std_msgs;
using UnityEngine;

public class test : MonoBehaviour
{
    private ROSBridgeWebSocketConnection ros = null;
    private float timer;
    private float pollRate = 2;
    private ROSGenericPublisher _genericPub;
    private ROSGenericSubscriber<CameraInfoMsg> _genericSub;
    private bool _running = false;


    void Start()
    {
        Initialise();

        Application.runInBackground = true;
    }

    void Update()
    {
        ros.Render();

        if (!_running) return;

        timer -= Time.deltaTime;

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
            HeaderMsg header = new HeaderMsg(1, new TimeMsg(1, 1), "0");

            PoseStampedMsg ps = new PoseStampedMsg(header, pose);
            PathMsg msg = new PathMsg(header, new PoseStampedMsg[] {ps, ps, ps});

            BoolMsg boolmsg = new BoolMsg(true);
            StringMsg str = new StringMsg("This is a test");
            RegionOfInterestMsg roi = new RegionOfInterestMsg(0, 1, 2, 3, true);
            CameraInfoMsg caminfo = new CameraInfoMsg(header, 100, 200, "plumb_bob", new double[5] , new double[9], new double[9], new double[12] , 14, 123, roi);

            _genericPub.PublishData(caminfo);
        }
    }

    private void Initialise()
    {
        ros = new ROSBridgeWebSocketConnection("ws://192.168.255.40", 9090, "Test");
        _genericPub = new ROSGenericPublisher(ros, "/raspicam_node/camera_info", CameraInfoMsg.GetMessageType());
        _genericSub = new ROSGenericSubscriber<CameraInfoMsg>(ros, "/raspicam_node/camera_info", CameraInfoMsg.GetMessageType(), msg => new CameraInfoMsg(msg));
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