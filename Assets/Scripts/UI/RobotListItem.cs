using UnityEngine;
using UnityEngine.UI;

public class RobotListItem : MonoBehaviour
{
    [SerializeField] private Text _robotNameText;
    [SerializeField] private Text _uriPortText;
    [SerializeField] private Button _connect;
    [SerializeField] private Text _connectText;
    [SerializeField] private Image _background;

    [SerializeField] private Color _connectConnectNormal;
    [SerializeField] private Color _connectConnectClick;
    [SerializeField] private Color _connectDisconnectNormal;
    [SerializeField] private Color _connectDisconnectClick;
    [SerializeField] private Color _notActiveBackgroundColor;
    [SerializeField] private Color _activeBackgroundColor;
    [SerializeField] private Color _connectedBackgroundColor;
    [SerializeField] private Color _disabledColor;

    public delegate void ConnectWasClicked(bool shouldConnect, string name);

    public event ConnectWasClicked OnConnectClicked;

    private bool _isActive;

    public bool IsActive
    {
        get { return _isActive; }
        set
        {
            _isActive = value;
            if (Connected)
                _background.color = _connectedBackgroundColor;
            else
                _background.color = value ? _activeBackgroundColor : _notActiveBackgroundColor;
            if (value)
                _connectText.text = _connected ? "Disconnect" : "Connect";
            else
                _connectText.text = "Not Active";

            _connect.interactable = value;
        }
    }

    private ColorBlock _disconnectColorBlock;
    private ColorBlock _connectColorBlock;

    private bool _connected;

    private bool Connected
    {
        get { return _connected; }
        set
        {
            _connected = value;
            _connect.colors = value ? _disconnectColorBlock : _connectColorBlock;
            _connectText.text = value ? "Disconnect" : "Connect";
            if (value)
                _background.color = _connectedBackgroundColor;
            else
                _background.color = IsActive ? _activeBackgroundColor : _notActiveBackgroundColor;
        }
    }

    private string _robotName;

    void Awake()
    {
        _connectColorBlock = new ColorBlock()
        {
            colorMultiplier = 1,
            fadeDuration = 0.1f,
            disabledColor = _disabledColor
        };
        _disconnectColorBlock = _connectColorBlock;

        _connectColorBlock.normalColor = _connectConnectNormal;
        _connectColorBlock.highlightedColor = _connectConnectNormal;
        _connectColorBlock.pressedColor = _connectConnectClick;

        _disconnectColorBlock.normalColor = _connectDisconnectNormal;
        _disconnectColorBlock.highlightedColor = _connectDisconnectNormal;
        _disconnectColorBlock.pressedColor = _connectDisconnectClick;
        Connected = false;
        IsActive = false;
        _connect.onClick.AddListener(OnConnectClick);
    }

    private void OnConnectClick()
    {
        Connected = !_connected;
        if (OnConnectClicked != null)
            OnConnectClicked(Connected, _robotName);
    }

    public void Initialise(string robotName, string uri, int port, bool connected)
    {
        _robotNameText.text = robotName;
        _uriPortText.text = uri + ":" + port;
        _robotName = robotName;
        Connected = connected;
    }
}