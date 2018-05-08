using System;

[Serializable]
public class RobotConfigFile
{
    public int[] Campuses;
    public string RosBridgeUri;
    public int RosBridgePort;
    public float MaxLinearSpeed;
    public float MaxAngularSpeed;
    public float LinearSpeedParameter;
    public float AngularSpeedParameter;
    public float RollSpeedParameter;
    public float PitchSpeedParameter;
}