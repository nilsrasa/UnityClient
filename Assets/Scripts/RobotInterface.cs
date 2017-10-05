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
    public enum RobotType { Telerobot, LegoRobot }
    public static RobotInterface Instance { get; private set; }
    public bool Parked { get; private set; }
    
    [Header("Robot Control Parameters")]
    public RobotType ControlledRobotType = RobotType.LegoRobot;
    public bool IsConnected { get; private set; }
    public AnimationCurve SpeedCurve;

    
    [SerializeField] private int _motorMaxSpeed = 255;
    [SerializeField] private int _motorMinSpeed = 60;
    [SerializeField] private float _commandTimer = 50;
    [SerializeField] private int _portNumber;
    [SerializeField] private int baudRate = 9600;
    [SerializeField] private int dataBits = 8;

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

    void Update() {
        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            SendCommandToRobot(Vector2.left);
            _left = true;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow)) {
            SendCommandToRobot(Vector2.right);
            _right = true;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            SendCommandToRobot(Vector2.down);
            _forward = true;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow)) {
            SendCommandToRobot(Vector2.up);
            _reverse = true;
        }
        if (Input.GetKeyUp(KeyCode.LeftArrow)) {
            SendCommandToRobot(Vector2.zero);
            _left = false;
        }
        if (Input.GetKeyUp(KeyCode.RightArrow)) {
            SendCommandToRobot(Vector2.zero);
            _right = false;
        }
        if (Input.GetKeyUp(KeyCode.UpArrow)) {
            SendCommandToRobot(Vector2.zero);
            _forward = false;
        }
        if (Input.GetKeyUp(KeyCode.DownArrow)) {
            SendCommandToRobot(Vector2.zero);
            _reverse = false;
        }
    }

    void Start() {
        try {
            Debug.Log("Trying to open port to robot control on port: " + _portName);
            HWPort = new SerialPort(_portName, baudRate);
            HWPort.Open();
            Debug.Log("Success open serial port to robot control ");
        }
        catch (Exception ex) {
            Debug.Log("Serial port to robot control error: " + ex.Message.ToString());
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
