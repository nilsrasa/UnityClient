using ROSBridgeLib;
using UnityEngine;

//Connecst to the Theta S UVC stiching software and outputs a square image
public class ThetaWebcam : RobotModule
{
    [SerializeField] private MeshRenderer _primarySphere;
    [SerializeField] private MeshRenderer _icoSphere;
    [SerializeField] private bool _useDummyImage;
    [SerializeField] private Texture _dummyImage;
    [SerializeField] private Texture _text;

    private WebCamTexture mycam;

    public override void Initialise(ROSBridgeWebSocketConnection rosBridge)
    {
        if (_useDummyImage)
        {
            _primarySphere.material.mainTexture = _dummyImage;
            return;
        }
        mycam = new WebCamTexture();
        string camName = "THETA UVC FullHD Blender";
        mycam.deviceName = camName;
        _primarySphere.sharedMaterial.mainTexture = mycam;
        _text = mycam;
        mycam.Play();
    }

    public override void StopModule()
    {
        mycam.Stop();
    }
}