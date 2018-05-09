using System;
using System.Collections.Generic;
using System.Threading;
using SimpleJSON;
using UnityEngine;
using WebSocketSharp;

/**
 * This class handles the connection with the external ROS world, deserializing
 * json messages into appropriate instances of packets and messages.
 * 
 * This class also provides a mechanism for having the callback's exectued on the rendering thread.
 * (Remember, Unity has a single rendering thread, so we want to do all of the communications stuff away
 * from that. 
 * 
 * The one other clever thing that is done here is that we only keep 1 (the most recent!) copy of each message type
 * that comes along.
 * 
 * Version History
 * 3.1 - changed methods to start with an upper case letter to be more consistent with c#
 * style.
 * 3.0 - modification from hand crafted version 2.0
 * 
 * @author Michael Jenkin, Robert Codd-Downey and Andrew Speers
 * @version 3.1
 */

namespace ROSBridgeLib
{
    public class ROSBridgeWebSocketConnection
    {
        private class RenderTask
        {
            private ROSBridgeSubscriber _subscriber;
            private string _topic;
            private ROSBridgeMsg _msg;

            public RenderTask(ROSBridgeSubscriber subscriber, string topic, ROSBridgeMsg msg)
            {
                _subscriber = subscriber;
                _topic = topic;
                _msg = msg;
            }

            public ROSBridgeSubscriber getSubscriber()
            {
                return _subscriber;
            }

            public ROSBridgeMsg getMsg()
            {
                return _msg;
            }

            public string getTopic()
            {
                return _topic;
            }
        }

        public delegate void WasDisconnected(bool wasClean);

        public event WasDisconnected OnDisconnect;

        public WebSocket WebSocket { get; private set; }
        public bool IsConnected { get; private set; }

        private string _host;
        private int _port;
        private System.Threading.Thread _myThread;
        private List<ROSBridgeSubscriber> _subscribers; // our subscribers
        private List<ROSBridgePublisher> _publishers; //our publishers
        private string _serviceName = null;
        private string _serviceValues = null;
        private Type _serviceResponse; // to deal with service responses
        private List<RenderTask> _taskQ = new List<RenderTask>();
        private string _robotName;

        private object _queueLock = new object();

        /**
         * Make a connection to a host/port. 
         * This does not actually start the connection, use Connect to do that.
         */
        public ROSBridgeWebSocketConnection(string host, int port, string robotName)
        {
            _host = host;
            _port = port;
            _myThread = null;
            _subscribers = new List<ROSBridgeSubscriber>();
            _publishers = new List<ROSBridgePublisher>();
            _robotName = robotName;
        }


        public void AddSubscriber(ROSBridgeSubscriber subscriber)
        {
            _subscribers.Add(subscriber);
            if (IsConnected)
                WebSocket.Send(ROSBridgeMsg.Subscribe(subscriber.GetMessageTopic(), subscriber.GetMessageType()));
        }

        public void AddPublisher(ROSBridgePublisher publisher)
        {
            _publishers.Add(publisher);
            if (IsConnected)
                WebSocket.Send(ROSBridgeMsg.Advertise(publisher.GetMessageTopic(), publisher.GetMessageType()));
        }

        public void RemoveSubcriber(ROSBridgeSubscriber subscriber)
        {
            _subscribers.Remove(subscriber);
            if (IsConnected)
                WebSocket.Send(ROSBridgeMsg.UnSubscribe(subscriber.GetMessageTopic()));
        }

        public void RemovePublisher(ROSBridgePublisher publisher)
        {
            _publishers.Remove(publisher);
            if (IsConnected)
                WebSocket.Send(ROSBridgeMsg.UnAdvertise(publisher.GetMessageTopic()));
        }

        /// <summary>
        /// Connects to ROSBridge
        /// </summary>
        /// <param name="callback">Callback on connection success</param>
        /// <returns></returns>
        public void Connect(Action<string, bool> callback = null)
        {
            Debug.Log("Connecting to " + _host);
            _myThread = new Thread(() => Run(callback));
            _myThread.Start();
        }

        /// <summary>
        /// Disconnects from the ROS Bridge and unsubscribes from all subscribed topics.
        /// </summary>
        public void Disconnect()
        {
            Debug.Log("Disconnecting from " + _host);
            _myThread.Abort();
            if (WebSocket == null) return;
            foreach (ROSBridgeSubscriber subscriber in _subscribers)
            {
                WebSocket.Send(ROSBridgeMsg.UnSubscribe(subscriber.GetMessageTopic()));
                Debug.Log("Sending " + ROSBridgeMsg.UnSubscribe(subscriber.GetMessageTopic()));
            }
            foreach (ROSBridgePublisher publisher in _publishers)
            {
                WebSocket.Send(ROSBridgeMsg.UnAdvertise(publisher.GetMessageTopic()));
                Debug.Log("Sending " + ROSBridgeMsg.UnAdvertise(publisher.GetMessageTopic()));
            }
            _subscribers = new List<ROSBridgeSubscriber>();
            _publishers = new List<ROSBridgePublisher>();
            WebSocket.Close();
            IsConnected = false;
        }

        private void Run(Action<string, bool> callback)
        {
            try
            {
                WebSocket = new WebSocket(_host + ":" + _port);
                WebSocket.OnMessage += (sender, e) => this.OnMessage(e.Data);
                WebSocket.Connect();
                if (callback != null)
                    callback(_robotName, WebSocket.IsAlive);
                if (!WebSocket.IsAlive) return;
            }
            catch (Exception e)
            {
                Debug.Log(e);
                return;
            }

            IsConnected = WebSocket.IsAlive;

            WebSocket.OnClose += (sender, args) =>
            {
                if (OnDisconnect != null) OnDisconnect(args.WasClean);
            };

            foreach (ROSBridgeSubscriber subscriber in _subscribers)
            {
                WebSocket.Send(ROSBridgeMsg.Subscribe(subscriber.GetMessageTopic(), subscriber.GetMessageType()));
                Debug.Log("Sending " + ROSBridgeMsg.Subscribe(subscriber.GetMessageTopic(), subscriber.GetMessageType()));
            }
            foreach (ROSBridgePublisher publisher in _publishers)
            {
                WebSocket.Send(ROSBridgeMsg.Subscribe(publisher.GetMessageTopic(), publisher.GetMessageType()));
                Debug.Log("Sending " + ROSBridgeMsg.Advertise(publisher.GetMessageTopic(), publisher.GetMessageType()));
            }
            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        private void OnMessage(string s)
        {
            if ((s != null) && !s.Equals(""))
            {
                JSONNode node = JSONNode.Parse(s);
                string op = node["op"];
                if ("publish".Equals(op))
                {
                    string topic = node["topic"];
                    foreach (ROSBridgeSubscriber subscriber in _subscribers)
                    {
                        if (topic.Equals(subscriber.GetMessageTopic()))
                        {
                            ROSBridgeMsg msg = subscriber.ParseMessage(node["msg"]);
                            RenderTask newTask = new RenderTask(subscriber, topic, msg);
                            lock (_queueLock)
                            {
                                bool found = false;
                                for (int i = 0; i < _taskQ.Count; i++)
                                {
                                    if (_taskQ[i].getTopic().Equals(topic))
                                    {
                                        _taskQ.RemoveAt(i);
                                        _taskQ.Insert(i, newTask);
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found)
                                    _taskQ.Add(newTask);
                            }
                        }
                    }
                }
                else if ("service_response".Equals(op))
                {
                    Debug.Log("Got service response " + node.ToString());
                    _serviceName = node["service"];
                    _serviceValues = (node["values"] == null) ? "" : node["values"].ToString();
                }
                else
                    Debug.Log("Must write code here for other messages");
            }
            else
                Debug.Log("Got an empty message from the web socket");
        }

        public void Render()
        {
            RenderTask newTask = null;
            lock (_queueLock)
            {
                if (_taskQ.Count > 0)
                {
                    newTask = _taskQ[0];
                    _taskQ.RemoveAt(0);
                }
            }
            if (newTask != null)
                newTask.getSubscriber().CallBack(newTask.getMsg());
            /*
            if (_serviceName != null)
            {
                ServiceResponse(_serviceResponse, _serviceName, _serviceValues);
                _serviceName = null;
            }
            */
        }

        public void Publish(String topic, ROSBridgeMsg msg)
        {
            if (WebSocket != null)
            {
                string s = ROSBridgeMsg.Publish(topic, msg.ToYAMLString());
                //Debug.Log ("Sending " + s);
                WebSocket.Send(s);
            }
        }

        public void CallService(string service, string args)
        {
            if (WebSocket != null)
            {
                string s = ROSBridgeMsg.CallService(service, args);
                //Debug.Log("Sending " + s);
                WebSocket.Send(s);
            }
        }
    }
}