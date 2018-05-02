using ROSBridgeLib;
using UnityEngine;

public abstract class RobotKit : MonoBehaviour
{
    public abstract void Initialise(ROSBridgeWebSocketConnection rosBridge);
}