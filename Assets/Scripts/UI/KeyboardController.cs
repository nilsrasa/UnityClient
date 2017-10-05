using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

//Controls keyboard GUI interface
public class KeyboardController : MonoBehaviour
{
    public static KeyboardController Instance { get; private set; }

    [SerializeField] private System.Collections.Generic.List<GazeKeyboardButton> _keyboardButtons;
    [SerializeField] private InputField _textViewer;
    [SerializeField] private Image _background;
    [SerializeField] private GazeButton _commandSwitch;
    
    [Header("Normal Colors")]
    [SerializeField] private Color _backgroundColor;
    [SerializeField] private Color _textViewerColor;
    [SerializeField] private Color _buttonHoverColor;
    [SerializeField] private Color _buttonTextColor;
    [SerializeField] private Color _buttonBorderColor;
    [SerializeField] private Color _buttonActivatedColor;
    
    [Header("Command Mode Colors")]
    [SerializeField] private Color _backgroundColorCmd;
    [SerializeField] private Color _textViewerColorCmd;
    [SerializeField] private Color _buttonHoverColorCmd;
    [SerializeField] private Color _buttonTextColorCmd;
    [SerializeField] private Color _buttonBorderColorCmd;
    [SerializeField] private Color _buttonActivatedColorCmd;

    public delegate void OnOpen();
    public event OnOpen Opened;
    public delegate void OnClose();
    public event OnClose Closed;
    public string CurrentString { get; private set; }
    
    private Image _textViewerBackground;
    private Image _textViewerTextBackground;
    private bool _isActive;
    private bool _commandModeIsActive;
    private bool _caps = true;
    private Vector3 _openPosition;
    private Vector3 _openScale;

    void Awake()
    {
        Instance = this;
        _textViewerBackground = _textViewer.transform.parent.GetComponent<Image>();
        _textViewerTextBackground = _textViewer.GetComponent<Image>();
        CurrentString = "";
    }

    void Start()
    {
        _openPosition = transform.localPosition;
        _openScale = transform.localScale;
        foreach (GazeKeyboardButton button in _keyboardButtons)
        {
            button.Activated += KeyActivated;
        }
    }

    void Update()
    {
        if (!_isActive) return;

        HandleKeyboardInput();
        _textViewer.text = CurrentString;
    }

    private void SendChatMessage()
    {
        ChatController.Instance.CreateChatMessage("Me", CurrentString, true);
        CurrentString = "";
    }

    private void SendCommand()
    {
        CurrentString = "";
    }

    private void ToggleCaps()
    {
        _caps = !_caps;
        foreach (GazeKeyboardButton button in _keyboardButtons)
        {
            if (button.Letter == '_' || button.Letter == '+' || button.Letter == '-' || button.Letter == '/' || button.Letter == '*') continue;
            button.Letter = _caps ? char.ToUpper(button.Letter) : char.ToLower(button.Letter);
        }
    }

    private void DoneAnimating(bool shouldShow)
    {
        foreach (GazeKeyboardButton button in _keyboardButtons) {
            button.SetEnabled(shouldShow);
        }
        _isActive = shouldShow;
    }

    /// <summary>
    /// Event called when keyboard GUI button is pressed.
    /// Some buttons are exchanged with char symbols to simplyfy the system.
    /// </summary>
    /// <param name="letter"> Char of the corresponding button.</param>
    public void KeyActivated(char letter)
    {
        switch (letter) {
            //Space
            case '_':
                CurrentString += " ";
                break;
            //Enter
            case '+':
                if (_commandModeIsActive)
                    SendCommand();
                else
                    SendChatMessage();
                break;
            //Backspace
            case '-':
                if (CurrentString.Length > 0)
                    CurrentString = CurrentString.Substring(0, CurrentString.Length - 1);
                break;
            //Clear
            case '/':
                CurrentString = "";
                break;
            //Capslock
            case '*':
                ToggleCaps();
                break;
            //Letter
            default:
                CurrentString += letter;
                break;
        }

    }

    /// <summary>
    /// Checks keyboard for inputs every frame to allow for typing on a physical keyboard as well as the virtual one.
    /// </summary>
    public void HandleKeyboardInput()
    {
        foreach (KeyCode kcode in Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(kcode)) continue;
            switch (kcode)
            {
                case KeyCode.Space:
                    CurrentString += " ";
                    break;
                case KeyCode.Return:
                    if (_commandModeIsActive)
                        SendCommand();
                    else
                        SendChatMessage();
                    break;
                case KeyCode.Backspace:
                    if (CurrentString.Length > 0)
                        CurrentString = CurrentString.Substring(0, CurrentString.Length - 1);
                    break;
                case KeyCode.CapsLock:
                    ToggleCaps();
                    break;
                default:
                    CurrentString += Input.inputString;
                    break;
            }
        }
    }

    public void ShowKeyboard()
    {
        iTween.ScaleTo(gameObject, new Hashtable {{"time", 0.75f}, {"scale", _openScale}, {"easetype", iTween.EaseType.easeInQuart},
            { "onstart", "DoneAnimating"}, { "onstartparams", true } });
        iTween.MoveTo(gameObject, new Hashtable {{"time", 0.75f}, {"position", _openPosition}, {"islocal", true}, {"easetype", iTween.EaseType.easeInQuart}});
        if (Opened != null)
            Opened();
    }

    public void HideKeyboard()
    {
        iTween.ScaleTo(gameObject, new Hashtable {{"time", 0.75f}, {"scale", Vector3.zero}, {"easetype", iTween.EaseType.easeInQuart},
            { "oncomplete", "DoneAnimating"}, { "oncompleteparams", false } });
        iTween.MoveTo(gameObject, new Hashtable { { "time", 0.75f }, { "position", new Vector3(_openPosition.x, _openPosition.y-2, _openPosition.z)},
            { "easetype", iTween.EaseType.easeInQuart }, {"islocal", true} });
        if (Closed != null)
            Closed();
    }

    public void ClearString()
    {
        CurrentString = "";
    }

    /// <summary>
    /// Inputs command string into keyboard and changes keyboard to command mode.
    /// </summary>
    /// <param name="command"> String of the command.</param>
    public void SendCommand(string command)
    {
        SetCommandMode(true);
        CurrentString = command;
        _commandSwitch.SetState(true);

    }

    public void ToggleKeyboard()
    {
        if (_isActive) HideKeyboard();
        else ShowKeyboard();
    }

    /// <summary>
    /// Sets command mode of keyboard allowing furture commands input on keyboard to be interpreted as commands instead of chat messages.
    /// </summary>
    public void SetCommandMode(bool isActive)
    {
        _commandModeIsActive = isActive;
        foreach (GazeKeyboardButton button in _keyboardButtons)
        {
            button.SetColor(new Color(0,0,0,0), 
                isActive ? _buttonHoverColorCmd : _buttonHoverColor,
                isActive ? _buttonTextColorCmd : _buttonTextColor,
                isActive ? _buttonBorderColorCmd : _buttonBorderColor,
                isActive ? _buttonActivatedColorCmd : _buttonActivatedColor);
        }
        _background.color = _textViewerBackground.color = isActive ? _backgroundColorCmd : _backgroundColor;
        _textViewerTextBackground.color = isActive ? _textViewerColorCmd : _textViewerColor;
    }

    public void ToggleCommandMode()
    {
        SetCommandMode(!_commandModeIsActive);
    }
}
