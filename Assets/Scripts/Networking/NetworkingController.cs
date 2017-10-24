using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Image = UnityEngine.Experimental.UIElements.Image;

public class NetworkingController : MonoBehaviour
{
    [SerializeField] private float _sendInterval = 100;

    private TcpClient _client;
    private Receiver _receiver;
    private NetworkStream _stream;
    private Sender _sender;

    private float _sendTimer;
    private bool _transmit;

    void Start()
    {
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
            Connect("localhost", 13000);

        if (Input.GetKeyDown(KeyCode.S))
        {
            byte[] data = new byte[1024];
            string stringdata = "This is a test! _:D 24412";
            Encoding.ASCII.GetBytes(stringdata).CopyTo(data, 0);
            _sender.SendData(data);
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            _transmit = true;
        }

        if (!_transmit) return;
        if (_sendTimer <= 0)
        {
            _sendTimer = _sendInterval;

            // Read screen contents into the texture

        }
        else
            _sendTimer -= Time.deltaTime;
    }


    void OnApplicationQuit()
    {
        if (_client == null) return; 
        _client.Close();
        _receiver.TerminateThreads();
        _sender.TerminateThreads();
    }

    public void Connect(string hostname, int port) {
        _client = new TcpClient(hostname, port);
        _stream = _client.GetStream();

        _receiver = new Receiver(_stream);
        _sender = new Sender(_stream);

        _receiver.DataReceived += OnDataReceived;
    }

    public void SendData(byte[] data) {
        _sender.SendData(data);
    }

    public event EventHandler<DataReceivedEventArgs> DataReceived;

    private void OnDataReceived(object sender, DataReceivedEventArgs e) {
        var handler = DataReceived;
        if (handler != null) DataReceived(this, e);  // re-raise event
    }


    public class Sender {
        private NetworkStream _stream;
        private Thread _thread;
        private bool _isDataToSend;
        private byte[] _dataToSend;

        internal void SendData(byte[] data)
        {
            _dataToSend = data;
            _isDataToSend = true;
        }

        internal Sender(NetworkStream stream) {
            _stream = stream;
            _thread = new Thread(Run);
            _thread.Start();
        }

        private void Run() {
            while (true)
            {
                if (_isDataToSend)
                {
                    _stream.Write(_dataToSend, 0, _dataToSend.Length);
                    _dataToSend = new byte[_dataToSend.Length];
                    _isDataToSend = false;
                }
            }
        }

        public void TerminateThreads() {
            _thread.Abort();
        }

    }
    
    public class Receiver {
        internal event EventHandler<DataReceivedEventArgs> DataReceived;
        private NetworkStream _stream;
        private Thread _thread;

        internal Receiver(NetworkStream stream) {
            _stream = stream;
            _thread = new Thread(Run);
            _thread.Start();
        }

        private void Run()
        {
            string str = "";
            while (true)
            {
                if (_stream.DataAvailable && DataReceived != null)
                {
                    byte[] data = new byte[1024];
                    using (MemoryStream ms = new MemoryStream()) {

                        int numBytesRead = _stream.Read(data, 0, data.Length);
                        ms.Write(data, 0, numBytesRead);
                        str = Encoding.ASCII.GetString(ms.ToArray(), 0, (int)ms.Length);
                        Debug.Log(str);
                    }
                }
                Thread.Sleep(500);
            }
        }

        public void TerminateThreads()
        {
            _thread.Abort();
        }
    }
    public class ImageReceiver {
        internal event EventHandler<DataReceivedEventArgs> DataReceived;
        private NetworkStream _stream;
        private Thread _thread;

        internal ImageReceiver(NetworkStream stream) {
            _stream = stream;
            _thread = new Thread(Run);
            _thread.Start();
        }

        private void Run() {
            while (true) {
                if (_stream.DataAvailable && DataReceived != null) {
                    byte[] data = new byte[600000];
                    using (MemoryStream ms = new MemoryStream()) {

                        int numBytesRead = _stream.Read(data, 0, data.Length);
                        ms.Write(data, 0, numBytesRead);
                        
                        Texture2D tex = new Texture2D(512, 512);
                        tex.LoadRawTextureData(ms.GetBuffer());
                    }
                }
                Thread.Sleep(100);
            }
        }

        public void TerminateThreads() {
            _thread.Abort();
        }
    }
}
