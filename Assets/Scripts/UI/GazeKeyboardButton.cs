using UnityEngine;

//Individual button on keyboard
public class GazeKeyboardButton : GazeButton
{
    [SerializeField] private char _letter;

    public char Letter
    {
        get { return _letter; }
        set
        {
            _letter = value;
            _buttonText.text = value.ToString();
        }
    }

    public new delegate void OnActivated(char letter);

    public new event OnActivated Activated;

    protected override void Awake()
    {
        base.Awake();
        string text = "";
        switch (Letter)
        {
            case '_':
                text = "SPACE";
                break;
            case '+':
                text = "ENTER";
                break;
            case '-':
                text = "BACK\nSPACE";
                break;
            case '/':
                text = "CLEAR";
                break;
            case '*':
                text = "CAPS LOCK";
                break;
            default:
                text = Letter.ToString();
                break;
        }
        _buttonText.text = text;
    }

    protected override void Activate()
    {
        base.Activate();
        if (Activated != null)
            Activated(Letter);
    }
}