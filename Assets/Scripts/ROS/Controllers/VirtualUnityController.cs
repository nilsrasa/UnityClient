using System.Collections;
using System.Collections.Generic;
using EZCameraShake;
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
    [SerializeField] private float BackwardsVelocity = 0.3f;

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
    private float TargetRotLow;
    private float TargetRotHigh;
    private float TargetRot;
    private float InitRot;
    private bool InitialShake;
    bool balancing = false;
    private float val = 0.0f;
    private float rotateSpeed = 0.01f;


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
        TargetRotLow = 0.0f;
        TargetRotHigh = 359.0f;
        InitRot =  0.0f;
        InitialShake = false;
    }
	
	// Update is called once per frame
	void Update ()
	{

        //UGLY BUT WORKS -> PUT IN FUNCTION FOR OPTIMIZING MAYBE 
	    float CurrentAngle = gameObject.transform.eulerAngles.x;
	    //Debug.Log(CurrentAngle);

        if (CurrentAngle > 2.0f && CurrentAngle < 10.0f && !balancing)
	    {
	        balancing = true;
	        val = 0.0f;
	        InitRot = CurrentAngle;
	        TargetRot = TargetRotLow;
	    }
	    else if (CurrentAngle < 358.0f && CurrentAngle > 340.0f && !balancing)
	    {
	        balancing = true;
	        val = 0.0f;
	        InitRot = CurrentAngle;
	        TargetRot = TargetRotHigh;
	    }

        if (balancing)
	    {
	        val = val + rotateSpeed;
	        float angle = Mathf.Lerp(InitRot, TargetRot, val);
            //rotate only x axis, ugly but it works
	        transform.rotation = Quaternion.Euler(angle, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);

	        if (val >= 1.0f)
	        {
	            balancing = false;
	        }
        }

    }

    public void JoystickCommand(Vector2 input)
    {
       
        Vector2 movement = new Vector2(input.y, input.x);

        if (!movement.Equals(new Vector2(0, 0)) && !InitialShake)
        {
            CameraShaker.Instance.ShakeOnce(1f, 1f, 0.2f, 0.2f);
            InitialShake = true;
        }


        if (movement.Equals(new Vector2(0, 0)))
        {
            InitialShake = false;
        }
    
        // Debug.Log(movement.x);
        if (movement.x > MaxLinearVelocity) movement.x = MaxLinearVelocity;
        else if (movement.x < 0) movement.x = -BackwardsVelocity;

        if (Mathf.Abs(movement.y) > MaxAngularVelocity)
        {
            if (movement.y < 0) movement.y = -MaxAngularVelocity;
            else movement.y = MaxAngularVelocity;
        }
       
        Vector3 velocity = this.gameObject.transform.right * movement.x;
        
        VirtualBot.AddForce(velocity, ForceMode.VelocityChange);
        this.gameObject.transform.Rotate(this.gameObject.transform.up, Mathf.Rad2Deg * Time.deltaTime * movement.y, Space.World);
    }

    void BalanceRobot(Vector2 input)
    {
       

    }


    void OnCollisionEnter(Collision collision)
    {

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            CameraShaker.Instance.ShakeOnce(2f, 2f, 0.5f, 0.5f);
            VirtualBot.drag = 10.0f;
        }
        
    }

    //void OnCollisionStay(Collision collision)
    //{
       
    //    if (collision.gameObject.CompareTag("Obstacle"))
    //    {
    //        VirtualBot.drag = 20.0f;
    //    }
    //    else
    //    {
    //        VirtualBot.drag = 0.0f;
    //    }
      
    //    //CameraShaker.Instance.ShakeOnce(2f, 2f, 0.5f, 0.5f);
    //    //Debug.Log("Collided!");
    //}

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            CameraShaker.Instance.ShakeOnce(2f, 2f, 0.5f, 0.5f);
            VirtualBot.drag = 0.0f;
        }

    }

    public void GazeCommand(Vector2 input)
    {
        //map correctly x axis to angular and y axis to linear from the input of the gazepad.
        // no need to reverse 
        Vector2 command = new Vector2(input.y, input.x);

        if (!InsideDeadZone(command.x, command.y))
        {
            //normalize speed and send data
           // Debug.Log(FilterLinearVelocity(command.x));
            VirtualBot.velocity = this.gameObject.transform.right * FilterLinearVelocity(command.x);
            //VirtualBot.angularVelocity = this.gameObject.transform.up; // * FilterAngularVelocity(command.y);
            this.gameObject.transform.Rotate(this.gameObject.transform.up, Mathf.Rad2Deg *Time.deltaTime*FilterAngularVelocity(command.y), Space.World);
        }
        else
        {
          StopRobot();
        }
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
