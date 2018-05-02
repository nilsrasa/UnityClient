using UnityEngine;
using UnityEngine.UI;

//GazeObject with extra functionality such as on/off color
class ParkingBrakeButton : GazeObject
{
    [SerializeField] private Text _text;
    [SerializeField] private Image _background;
    [SerializeField] private Color _offColor;
    [SerializeField] private Color _offHoverColor;
    [SerializeField] private Color _onColor;
    [SerializeField] private Color _onHoverColor;

    protected override void Awake()
    {
        base.Awake();
        _background.color = !IsActivated ? _onColor : _offColor;
    }

    protected override void Activate()
    {
        base.Activate();
        _background.color = !IsActivated ? _onColor : _offColor;
        _text.text = !IsActivated ? "D" : "P";
    }

    public override void OnHover()
    {
        base.OnHover();
        _background.color = !IsActivated ? _onHoverColor : _offHoverColor;
    }

    public override void OnUnhover()
    {
        base.OnUnhover();
        _background.color = !IsActivated ? _onColor : _offColor;
    }
}