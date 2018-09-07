using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*
 This controller is directly attached to the Unity virtual arlobot without connecting through ROS
 Simple velocity commands received from the VRController.
     
     */
public class VirtualUnityController : MonoBehaviour {

    public static VirtualUnityController Instance { get; private set; }

    [SerializeField] private float MaxLinearVelocity = 0.4f;
    [SerializeField] private float MinLinearVelocity = 0.05f;
    [SerializeField] private float MaxAngularVelocity = 0.8f;
    [SerializeField] private float BackwardsVelocity = 0.1f;

    [HideInInspector] public bool IsConnected = false;
    private Rigidbody VirtualBot;

    //these should be in robotcontrolpad maybe , not here as filters. They are part of the UI.
    [SerializeField] private float MaxVelocityHorizon = 0;
    [SerializeField] public float UpperDeadZoneLimit = -0.5f;
    [SerializeField] public float LowerDeadZoneLimit = -0.7f;
    [SerializeField] public float RightDeadZoneLimit = 0.2f;
    [SerializeField] public float LeftDeadZoneLimit = -0.2f;

    [SerializeField] private float LeftBackZoneLimit = -0.2f;
    [SerializeField] private float UpperBackZoneLimit = -0.8f;

    private Vector2 InitRange;
    private Vector2 NewRange;


    void Awake()
    {
        Instance = this;
    }

    // Use this for initialization
    void Start ()
    {
        InitRange = new Vector2(UpperDeadZoneLimit, MaxVelocityHorizon);
        NewRange = new Vector2(0, MaxLinearVelocity);
        VirtualBot = this.gameObject.GetComponent<Rigidbody>();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void JoystickCommand(Vector2 input)
    {
       //Filter the joystick input appropriately

    }

    public void GazeCommand(Vector2 input)
    {
        //map correctly x axis to angular and y axis to linear from the input of the gazepad.
        // no need to reverse 
        Vector2 command = new Vector2(input.y, input.x);

        if (!InsideDeadZone(command.x, command.y))
        {
            //normalize speed and send data
            VirtualBot.velocity = this.gameObject.transform.right * FilterLinearVelocity(command.x);
            VirtualBot.angularVelocity = this.gameObject.transform.up * FilterAngularVelocity(command.y);
        }
        else
        {
          StopRobot();
        }
        
       
        //angular rotation goes here
    }

    private float FilterLinearVelocity(float vel)
    {
        //From this point and upwards, you move with maximum velocity
        if (vel >= MaxVelocityHorizon)
        {
            // Debug.Log("Maximum Velocity");
            return MaxLinearVelocity;
        }
        //between that point and until the dead zone move with adjusted speed
        else if (vel < MaxVelocityHorizon && vel > UpperDeadZoneLimit)
        {
            float velnorm = NormalizeValues(vel);

            //Return minimum velocity instead of something too close to 0
            if (velnorm < MinLinearVelocity)
            {
                return MinLinearVelocity;
            }
            return velnorm;
        }
        else if (vel < LowerDeadZoneLimit)
        {
            return 0;
        }

        return 0;
    }

    private float FilterAngularVelocity(float vel)
    {
        if (Mathf.Abs(vel) > MaxAngularVelocity)
        {
            return vel > 0 ? MaxAngularVelocity : -MaxAngularVelocity;

        }

        //TODO normalize here a bit more?
        return vel;
    }

    public void StopRobot()
    {
        VirtualBot.velocity = Vector3.zero;
        VirtualBot.angularVelocity = Vector3.zero;
    }

    public float NormalizeValues(float value)
    {

        return (((NewRange.y - NewRange.x) * (value - InitRange.x)) / (InitRange.y - InitRange.x)) + NewRange.x;

    }
    //allow the commands to control the virtual robot
    public void Connect()
    {
        IsConnected = true;
    }
    //remove control over virtual robot
    public void Disconnect()
    {
        IsConnected = false;
    }

    //returns true if we are in the bounding box of the dead zone
    private bool InsideDeadZone(float x, float y)
    {
        if (y > LeftDeadZoneLimit && y <= RightDeadZoneLimit
            && x > LowerDeadZoneLimit && x < UpperDeadZoneLimit)
        {
            return true;
        }


        return false;
    }

}
