using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestController : MonoBehaviour {

	public static TestController Instance { get; private set; }

    [SerializeField] private InputField _rosmasteruri;
    [SerializeField] private InputField _rosmasterport;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RobotControlTrackPad _trackpad;

    [Header("Test variables")]
    [SerializeField] private InputField _messageMinIntervalMs;

    private bool _isRunning;
    private float _timer;
    private RobotControlTrackPad _robotControl;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (!_isRunning) return;
        float pollingRate = 100;
        if (!string.IsNullOrEmpty(_messageMinIntervalMs.text))
            pollingRate = int.Parse(_messageMinIntervalMs.text);
        _timer += Time.deltaTime;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) {
            RobotControlTrackPad robotControl = hit.transform.GetComponent<RobotControlTrackPad>();
            if (robotControl != null)
            {
                _robotControl = robotControl;
                robotControl.OnHover();
                Vector2 controlResult = robotControl.GetControlResult(hit.point);
                if (robotControl.IsActivated && _timer >= pollingRate / 1000f)
                {
                    ArlobotROSController.Instance.MoveDirect(controlResult);
                    _timer = 0;
                }
            }
            
        }
        else
        {
            if (_robotControl == null) return;
            _robotControl.OnUnhover();
            if (_timer >= pollingRate / 1000f)
                ArlobotROSController.Instance.MoveDirect(Vector2.zero);

        }
    }

    public void StartRos()
    {
        if (!string.IsNullOrEmpty(_rosmasteruri.text))
        {
            string hostname = _rosmasteruri.text.Contains("http://") ? _rosmasteruri.text + ":" + _rosmasterport.text : "http://" + _rosmasteruri.text + ":" +_rosmasterport.text;
            ArlobotROSController.Instance.StartROS(hostname);
        }
        else
            ArlobotROSController.Instance.StartROS();
        
        _canvas.gameObject.SetActive(false);
        _trackpad.gameObject.SetActive(true);
        _isRunning = true;
    }
}
