using System;
using ROSBridgeLib;
//using System.IO.Ports;
using UnityEngine;

//The control interface to the robot
//TODO: To be changed to ROS
public class RobotInterface : MonoBehaviour {

    public enum CommandType {
        TurnSeatLeft,
        TurnSeatRight,
        TurnRobotLeft,
        TurnRobotRight,
        DriveRobotForwards,
        DriveRobotReverse,
        ParkingBrakeEngaged,
        ParkingBrakeDisengaged,
        StopRobot 
    }
    public static RobotInterface Instance { get; private set; }
    public bool Parked { get; private set; }
    
    public bool IsConnected { get; private set; }
    public AnimationCurve SpeedCurve;

    [SerializeField] private int _motorMaxSpeed = 255;
    [SerializeField] private int _motorMinSpeed = 60;
    [SerializeField] private float _commandTimer = 50;

    private float _timer = 0;
    private string _portName;
   // private SerialPort HWPort;
    private bool _left, _right, _forward, _reverse;

    private bool _isStopped;

    private ROSLocomotionDirect _rosLocomotionDirect;
    private ROSBridgeWebSocketConnection _rosBridge;

    void Awake() {
        Instance = this;
    }

    void OnApplicationQuit() {
        if (_rosBridge != null)
            _rosBridge.Disconnect();
    }

    private string GetMotorSpeedString(float speed) {
        int intIntensity = (int)(SpeedCurve.Evaluate(Mathf.Abs(speed)) * (_motorMaxSpeed - _motorMinSpeed) + _motorMinSpeed);
        if (speed == 0)
            return "000";
        else
            return intIntensity.ToString("000");
    }

    private void SendCommandToRobot(Vector2 controlOutput) {
        Debug.Log(controlOutput);
        Vector2 movement = new Vector2(controlOutput.y, - controlOutput.x);
        _rosLocomotionDirect.PublishData(movement.x, movement.y);
        _isStopped = false;
    }   

    public void StopRobot()
    {
        if (!IsConnected || _isStopped) return;
        _rosLocomotionDirect.PublishData(0, 0);
        _isStopped = true;
    }

    public void SendCommand(Vector2 controlOutput) {
        if (!IsConnected) return;
        if (_timer < _commandTimer / 1000f) {
            _timer += Time.deltaTime;
            return;
        }
        _timer = 0;

        SendCommandToRobot(controlOutput);
    }

    public void SetParkingBrake(bool isOn) {
        if (isOn) {
            GuiController.Instance.SetRobotControlVisibility(false);
            StreamController.Instance.EnableParkedMode();
        }
        else {
            StreamController.Instance.DisableParkedMode();
            GuiController.Instance.SetSeatControlVisibility(false);
            VRController.Instance.CenterSeat();
        }
        Parked = isOn;
    }

    public void DoneEnableDrivingMode() {
        GuiController.Instance.SetRobotControlVisibility(true);
        Viewport.Instance.SetEnabled(true);
    }

    public void DoneEnableParkMode() {
        GuiController.Instance.SetSeatControlVisibility(true);
    }

    public void Connect() {
        _rosBridge = new ROSBridgeWebSocketConnection("ws://Raspi-ROS-02", 9090);
        _rosLocomotionDirect = new ROSLocomotionDirect(ROSAgent.AgentJob.Publisher, _rosBridge, "/cmd_vel");
        _rosBridge.Connect(((s, b) => {Debug.Log(s + " - " + b);}));
        IsConnected = true;
    }
}
