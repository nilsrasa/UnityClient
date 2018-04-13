using UnityEngine;
using UnityEngine.UI;

public class RobotListItem : MonoBehaviour
{

    [SerializeField] private Text _robotName;
    [SerializeField] private Text _uriPort;
    [SerializeField] private Button _connect;
    [SerializeField] private Text _connectText;
    [SerializeField] private Image _background;

    [SerializeField] private Color _connectDisconnectNormal;
    [SerializeField] private Color _connectDisconnectClick;
    [SerializeField] private Color _notActiveBackgroundColor;
    [SerializeField] private Color _activeBackgroundColor;
    [SerializeField] private Color _connectedBackgroundColor;

    public delegate void ConnectWasClicked(bool shouldConnect);
    public event ConnectWasClicked OnConnectClicked;

    private bool _isActive;
    public bool IsActive
    {
        get { return _isActive; }
        set
        {
            _isActive = value;
            _background.color = value ? _activeBackgroundColor : _notActiveBackgroundColor;
            if (value)
                _connectText.text = _connectDisconnect ? "Disconnect" : "Connect";
            else
                _connectText.text = "Not Active";

            _connect.interactable = value;
        }
    }

    private ColorBlock _disconnectColorBlock;
    private ColorBlock _connectColorBlock;

    private bool _connectDisconnect;
    private bool ConnectDisconnect
    {
        get { return _connectDisconnect; }
        set
        {
            _connectDisconnect = value;
            _connect.colors = value ? _disconnectColorBlock : _connectColorBlock;
            _connectText.text = value ? "Disconnect" : "Connect";
            _background.color = value ? _connectedBackgroundColor : _activeBackgroundColor;
        }
    }

    void Awake()
    {
        _connectColorBlock = _connect.colors;
        _disconnectColorBlock = _connect.colors;
        _disconnectColorBlock.normalColor = _connectDisconnectNormal;
        _disconnectColorBlock.highlightedColor = _connectDisconnectNormal;
        _disconnectColorBlock.pressedColor = _connectDisconnectClick;
        ConnectDisconnect = false;
        IsActive = false;
        _connect.onClick.AddListener(OnConnectClick);
    }

    private void OnConnectClick()
    {
        ConnectDisconnect = !_connectDisconnect;
        if (OnConnectClicked != null)
            OnConnectClicked(ConnectDisconnect);
    }

    public void Initialise(string robotName, string uri, int port)
    {
        _robotName.text = robotName;
        _uriPort.text = uri + ":" + port;
    }

}
