using UnityEngine;

public class ArlobotROSController : ROSController {

    public static ArlobotROSController Instance { get; private set; }

    private ROSLocomotion _rosLocomotion;
    private ROSUltrasound _rosUltrasound;

    void Awake()
    {
        Instance = this;
    }

    public override void StartROS() {
        base.StartROS();
        _rosLocomotion = new ROSLocomotion();
        _rosLocomotion.StartAgent(ROSAgent.AgentJob.Publisher, _clientNamespace);
        _rosUltrasound = new ROSUltrasound();
        _rosUltrasound.StartAgent(ROSAgent.AgentJob.Subscriber, _clientNamespace);
    }

    public override void Move(Vector2 movement) {
        _rosLocomotion.PublishData(movement);
    }

}