using UnityEngine;
using UnityEngine.UI;

//Button that can be activated through gaze. 
//Extended from GazeObject and brings functionality such as coloring, button text and tooltips.
public class GazeButton : GazeObject
{
    [SerializeField] protected Image _background;
    [SerializeField] protected string _activatedTextOverride;
    
    protected Text _buttonText;
    protected SpriteRenderer _border;
    protected string _orgText;
    protected string _activeText;

    public Color _backgroundColor = new Color(0,0,0,0);
    public Color _hoverColor;
    public Color _activatedColor = Color.green;

    protected override void Awake()
    {
        base.Awake();
        _border = GetComponentInChildren<SpriteRenderer>();
        _buttonText = GetComponentInChildren<Text>();
        _orgText = _buttonText.text;

        //As unity's inspector encapsulates strings, they don't allow newline character
        //and they are therefore replaced with '#'
        if (_activatedTextOverride.Length > 0)
        {
            _activeText = _activatedTextOverride.Replace('#', '\n');
            _buttonText.text = IsActivated ? _activeText : _orgText;
        }
    }

    protected override void Start()
    {
        base.Start();
        _background.color = IsActivated ? _activatedColor : _backgroundColor;
    }
    
    protected override void Activate()
    {
        base.Activate();

        _background.color = IsActivated ? _activatedColor : _backgroundColor;
        if (!_resetOnUnhover) _background.color = Gazed ? _hoverColor : _backgroundColor;
        if (_activatedTextOverride.Length > 0)
            _buttonText.text = IsActivated ? _activeText : _orgText;
    }

    public override void OnHover()
    {
        base.OnHover();
        _background.color = _hoverColor;
    }

    public override void OnUnhover()
    {
        base.OnUnhover();
        if (IsActivated && _oneTimeUse || _locked) return;
        _background.color = IsActivated ? _activatedColor : _backgroundColor;
    }

    public override void SetEnabled(bool isEnabled)
    {
        base.SetEnabled(isEnabled);
        if (_border != null)
            _border.enabled = isEnabled;
    }

    public override void SetState(bool isOn)
    {
        base.SetState(isOn);
        _background.color = IsActivated ? _activatedColor : _backgroundColor;

        if (_activatedTextOverride.Length > 0)
            _buttonText.text = IsActivated ? _activeText : _orgText;
    }

    public virtual void SetColor(Color background, Color hover, Color text, Color border, Color activated)
    {
        _background.color = background;
        _hoverColor = hover;
        _buttonText.color = text;
        _border.color = border;
        _activatedColor = activated;
    }
}
