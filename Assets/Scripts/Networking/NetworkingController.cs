using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class NetworkingController : MonoBehaviour
{

    private TcpClient _client;
    private Receiver _receiver;
    private NetworkStream _stream;
    private Sender _sender;

    void Start()
    {
        Connect("localhost", 3001);
    }

    void OnApplicationQuit()
    {
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
        Debug.Log("DATA RECEIVED");
        var handler = DataReceived;
        if (handler != null) DataReceived(this, e);  // re-raise event
    }


    public class Sender {
        private NetworkStream _stream;
        private Thread _thread;

        internal void SendData(byte[] data) {
            // transition the data to the thread and send it...
        }

        internal Sender(NetworkStream stream) {
            _stream = stream;
            _thread = new Thread(Run);
            _thread.Start();
        }

        private void Run() {
            // main thread loop for sending data...
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

                        int numBytesRead;
                        while ((numBytesRead = _stream.Read(data, 0, data.Length)) > 0) {
                            ms.Write(data, 0, numBytesRead);
                        }
                        str = Encoding.ASCII.GetString(ms.ToArray(), 0, (int)ms.Length);
                        Debug.Log(str);
                    }
                }
                    Debug.Log("ASD");
                Thread.Sleep(500);
            }
        }

        public void TerminateThreads()
        {
            _thread.Abort();
        }
    }
}
 