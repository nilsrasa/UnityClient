using ROSBridgeLib;
using UnityEngine;

public class KeyboardRemoteControl : RobotModule
{
    [SerializeField] private int _publishingRateMs = 300;

    private ROSLocomotionDirect _cmdVel;

    private float _timer;
    private bool _stopped;

	void Update ()
	{
        Vector2 vel = Vector2.zero;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            vel += new Vector2(1, 0);
	    if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            vel += new Vector2(-1, 0);
	    if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            vel += new Vector2(0, 1.5f);
	    if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            vel += new Vector2(0, -1.5f);

	    _timer += Time.deltaTime;
	    if (_timer >= (float) _publishingRateMs / 1000)
	    {
	        _timer = 0;
	        if (vel == Vector2.zero && !_stopped)
	        {
	            _stopped = true;
                _cmdVel.PublishData(vel.y, vel.x);
            }
            else if (vel != Vector2.zero)
	        {
	            _stopped = false;
                _cmdVel.PublishData(vel.y, vel.x);
            }
	    }
    }

    public override void StopModule()
    {
        this.enabled = false;
    }

    public override void Initialise(ROSBridgeWebSocketConnection rosBridge)
    {
        base.Initialise(rosBridge);
        _cmdVel = new ROSLocomotionDirect(ROSAgent.AgentJob.Publisher, rosBridge, "/cmd_vel");
    }
}
