using ROSBridgeLib;
using UnityEngine;

public abstract class RobotModule : MonoBehaviour
{
    protected ROSBridgeWebSocketConnection _rosBridge;

    public virtual void Initialise(ROSBridgeWebSocketConnection rosBridge)
    {
        _rosBridge = rosBridge;
    }

    public abstract void StopModule();
}