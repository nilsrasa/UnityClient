using System;
using System.IO.Ports;
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
    public enum RobotType { Telerobot, LegoRobot, Arlobot }
    public static RobotInterface Instance { get; private set; }
    public bool Parked { get; private set; }
    
    [Header("Robot Control Parameters")]
    public RobotType ControlledRobotType = RobotType.LegoRobot;
    public bool IsConnected { get; private set; }
    public AnimationCurve SpeedCurve;

    
    [SerializeField] private int _motorMaxSpeed = 255;
    [SerializeField] private int _motorMinSpeed = 60;
    [SerializeField] private float _commandTimer = 50;

    [Header("Serialport Parameters")]
    [SerializeField] private int _portNumber;
    [SerializeField] private int baudRate = 9600;
    [SerializeField] private int dataBits = 8;

    [Header("ROS Parameters")]
    [SerializeField] private string _ROS_MASTER_URI = "127.0.0.1:11311";

    private float _timer = 0;
    private string _portName;
    private SerialPort HWPort;
    private bool _left, _right, _forward, _reverse;

    void Awake() {
        Instance = this;
        if (_portNumber > 9)
            _portName += @"\\.\";
        _portName += "COM" + _portNumber;
    }

    void Start() {
        switch (ControlledRobotType)
        {
            case RobotType.Telerobot:
            case RobotType.LegoRobot:
                try {
                    Debug.Log("Trying to open port to robot control on port: " + _portName);
                    HWPort = new SerialPort(_portName, baudRate);
                    HWPort.Open();
                    Debug.Log("Success open serial port to robot control ");
                    Connect();
                }
                catch (Exception ex) {
                    Debug.Log("Serial port to robot control error: " + ex.Message.ToString());
                }
                break;
            case RobotType.Arlobot:
                if (!string.IsNullOrEmpty(_ROS_MASTER_URI))
                    ROSController.Instance.StartROS(_ROS_MASTER_URI);
                else
                    ROSController.Instance.StartROS();
                Connect();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
    }

    void OnApplicationQuit() {
        //HWPort.Close();     
    }

    private string GetMotorSpeedString(float speed) {
        int intIntensity = (int)(SpeedCurve.Evaluate(Mathf.Abs(speed)) * (_motorMaxSpeed - _motorMinSpeed) + _motorMinSpeed);
        if (speed == 0)
            return "000";
        else
            return intIntensity.ToString("000");
    }

    private void SendCommandToRobot(Vector2 controlOutput) {
        switch (ControlledRobotType)
        {
            case RobotType.Telerobot:
            case RobotType.LegoRobot:
                int leftMotorDrive, rightMotorDrive;
                leftMotorDrive = rightMotorDrive = 1;
                float leftMotorSpeed, rightMotorSpeed;
                leftMotorSpeed = rightMotorSpeed = 0;

                string commandString = "DK00";
                if (controlOutput == Vector2.zero)
                    leftMotorDrive = rightMotorDrive = 2;
                else if (controlOutput.x == 0) {
                    if (controlOutput.y > 0)
                        leftMotorDrive = rightMotorDrive = 2;
                    else
                        leftMotorDrive = rightMotorDrive = 0;
                    leftMotorSpeed = rightMotorSpeed = controlOutput.y;
                }
                else if (controlOutput.y == 0) {
                    if (controlOutput.x > 0) {
                        leftMotorDrive = 0;
                        rightMotorDrive = 2;
                    }
                    else {
                        leftMotorDrive = 2;
                        rightMotorDrive = 0;
                    }
                    leftMotorSpeed = rightMotorSpeed = controlOutput.x;
                }
                else {
                    if (controlOutput.x < 0) {
                        leftMotorSpeed = controlOutput.y * 0.75f;
                        rightMotorSpeed = (Mathf.Abs(controlOutput.x) + 1) / 2;
                    }
                    else if (controlOutput.x > 0) {
                        rightMotorSpeed = controlOutput.y * 0.75f;
                        leftMotorSpeed = (Mathf.Abs(controlOutput.x) + 1) / 2;
                    }
                    leftMotorDrive = rightMotorDrive = controlOutput.y > 0 ? 2 : 0;
                }

                commandString += "" + leftMotorDrive + GetMotorSpeedString(leftMotorSpeed) + rightMotorDrive +
                                 GetMotorSpeedString(rightMotorSpeed);

                if (HWPort.IsOpen)
                    HWPort.Write(commandString);
                break;
            case RobotType.Arlobot:
                Vector2 movement = new Vector2(-controlOutput.x, -controlOutput.y);
                ROSController.Instance.Move(movement);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
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
        IsConnected = true;
    }
}
