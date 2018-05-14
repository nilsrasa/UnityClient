using System;
using System.IO;
using System.IO.Ports;
using ROSBridgeLib;
using UnityEngine;

public class WheelchairSimConnector : RobotModule
{
    [SerializeField] private float _publishingInterval = 0.2f;
    [SerializeField] private string _portId;
    [SerializeField] private float _linearCoefficient = -0.05f;
    [SerializeField] private float _angularCoefficient = -0.02f;
    [SerializeField] private float _wheelbase = 0.3f;

    private float _publishingTimer = 0;
    private ROSLocomotionDirect _locomotionDirect;
    private Connector _connector;
    private bool _isRunning;

    void Update()
    {
        if (!_isRunning) return;
        _publishingTimer -= Time.deltaTime;
        if (_publishingTimer <= 0)
        {
            Movement m = _connector.Collect();
            PublishData(m.LeftTicks, m.RightTicks);
            _publishingTimer = _publishingInterval;
        }
    }

    private void PublishData(int leftTicks, int rightTicks)
    {
        float left = leftTicks * Time.deltaTime;
        float right = rightTicks * Time.deltaTime;
        float linearSpeed = ((left + right) / 2) * _linearCoefficient;
        float angularSpeed = ((left - right) / _wheelbase) * _angularCoefficient;

        _locomotionDirect.PublishData(linearSpeed, angularSpeed);
    }

    public override void Initialise(ROSBridgeWebSocketConnection rosBridge)
    {
        base.Initialise(rosBridge);

        _connector = new Connector();
        Debug.Log(_connector.AutoConnect());
        _locomotionDirect = new ROSLocomotionDirect(ROSAgent.AgentJob.Publisher, _rosBridge, "/cmd_vel");
        _isRunning = true;
    }

    public override void StopModule()
    {
        _isRunning = false;
        _connector.Disconnect();
    }
}

public class Movement
{
    //Ticks per circle = 1024
    //Wheel diameter = 6.00 cm (60000 micrometer)
    private const double ticks2MicroMeterFactor = 1; //60000/1024;   

    public int LeftTicks { private set; get; }

    public int RightTicks { private set; get; }

    public Movement(int lTicks, int rTicks)
    {
        LeftTicks = lTicks;
        RightTicks = rTicks;
    }

    internal void IncreaseWith(Movement m)
    {
        LeftTicks += m.LeftTicks;
        RightTicks += m.RightTicks;
    }

    internal double getLeftMicroMeter()
    {
        return LeftTicks * ticks2MicroMeterFactor;
    }

    internal double getRightMicroMeter()
    {
        return RightTicks * ticks2MicroMeterFactor;
    }

    internal void Reset()
    {
        LeftTicks = 0;
        RightTicks = 0;
    }
}

public class Connector
{
    //Msg format: "wc:<left value>,<right value>;"
    private static readonly String END_CHAR = ";";

    private static readonly String COL_CHAR = ":";
    private static readonly String SEP_CHAR = ",";
    private static readonly String START_REQUEST = "s?" + END_CHAR;
    private static readonly String START_RESPONSE = "wc" + COL_CHAR;
    private static readonly String DATA_REQUEST = "d" + END_CHAR;
    private static readonly long RESPONSE_TIME_MS = 1000;

    Action<String> collectorAction = delegate(String s)
    {
        interpretIncoming(s); //TODO Implement actual method here...
    };

    Action<SerialPort> collector = delegate(SerialPort sp)
    {
        while (true) //sp.IsOpen)
            interpretIncoming(sp.ReadLine()); //TODO Implement actual method here...
    };

    //private static Thread collectorThread = new Thread() {
    //};

    private static Connector _instance;

    private static SerialPort _serialPort;
    //  public event EventHandler OnMove;

    //private static Movement _latestMovement;
    private static String incomingLeftover;

    public Connector()
    {
    }

    public string AutoConnect()
    {
        String[] ports = new String[] {"COM2", "COM3", "COM4", "COM5"}; // SerialPort.GetPortNames();


        //TODO tryDisconnect(); //Make sure it is disconnected and that we don't leave an open connection!
        foreach (String port in ports)
        {
            if (!port.Equals("COM1"))
            {
                SerialPort sp = tryConnect(port, 9600, Parity.None, 8, StopBits.One); //_serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
                if (sp != null)
                {
                    _serialPort = sp;

                    //  _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                    //collector(sp)
                    return "Connected Successfully on port " + port;
                }
            }
        }

        return "Failure in connecting to wheelchair simulator";
    }

    public Movement Collect()
    {
        if (_serialPort != null)
        {
            _serialPort.WriteLine(DATA_REQUEST);
            String s = _serialPort.ReadLine();

            Movement m = interpretIncoming(s);
            return m; // _latestMovement.IncreaseWith(m);
        }

        Console.Out.WriteLine("Serialport is null");
        return new Movement(0, 0);
    }

    public Movement GetMovement()
    {
        _serialPort.ReadTimeout = 200;
        String s = _serialPort.ReadLine();

        Movement m = /*new Movement(1, 2);/*/ interpretIncoming(s);
        return m;
    }

    private static Movement interpretIncoming(String str)
    {
        str += incomingLeftover;
        int l = 0;
        int r = 0;
        int end = str.IndexOf(END_CHAR);
        while (-1 < end)
        {
            int colIndex = str.IndexOf(COL_CHAR);
            int index = str.IndexOf(SEP_CHAR);

            try
            {
                //TODO If this fails it will turn into an constant loop... :-/
                int tl = 0;
                int tr = 0;
                try
                {
                    tl = Int32.Parse(str.Substring(colIndex + 1, index - colIndex - 1));
                    tr = Int32.Parse(str.Substring(index + 1, end - index - 1));
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                    return null;
                }

                //Remove parsed bit
                str = str.Substring(end + 1);

                //If both parsed => add to counters
                l += tl;
                r += tr;
            }
            catch (FormatException)
            {
                //Something went wrong during the parsing... Not doing anything about that.
                Console.Error.WriteLine("Unable to parse incoming substring: \"" + str.Substring(0, end) + "\"");
                continue;
            }

            end = str.IndexOf(END_CHAR);
        }

        incomingLeftover = str;

        return new Movement(l, r * -1);
    }


    private SerialPort tryConnect(String portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
    {
        SerialPort sp = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
        try
        {
            sp.Open();
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine(portName + " occupied");
            return null;
        }
        catch (IOException e)
        {
            Console.WriteLine("Not connected - " + e.Message);
            return null;
        }

        //sp.Write(START_REQUEST);
        sp.Write(DATA_REQUEST);
        DateTime end = DateTime.Now.AddMilliseconds(RESPONSE_TIME_MS);
        String incoming = "";
        do
        {
            //incoming += sp.ReadExisting();
            incoming += sp.ReadLine();
            if (incoming.Contains(START_RESPONSE))
            {
                Console.WriteLine(portName + ": Connected!");
                return sp;
            }
        } while (DateTime.Now.CompareTo(end) < 0);

        Console.WriteLine("Incoming: " + incoming);
        Console.WriteLine(portName + ": No valid response");
        return null;
    }

    public void Disconnect()
    {
        if (_serialPort == null)
            return;

        _serialPort.Close();
        _serialPort = null;
    }
}