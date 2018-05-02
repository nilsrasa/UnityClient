using UnityEngine;

//Unity light that lights up depending on input
class LightIndicator : MonoBehaviour
{
    [SerializeField] private MeshRenderer _bulbRenderer;
    [SerializeField] private Color _onColor;
    [SerializeField] private Color _offColor;
    [SerializeField] private Color _onEmissionColor;
    [SerializeField] private Color _offEmissionColor;

    [SerializeField] private Light _light;

    [SerializeField] private bool _startStatus;
    private bool _isOn;

    public bool IsOn
    {
        get { return _isOn; }
        set
        {
            _bulbRenderer.material.SetColor("_EmissionColor", value ? _onEmissionColor : _offEmissionColor);
            _bulbRenderer.material.color = value ? _onColor : _offColor;
            _isOn = value;
            if (_light != null)
            {
                _light.enabled = value;
                _light.color = value ? _onColor : _offColor;
            }
        }
    }

    void Start()
    {
        IsOn = _startStatus;
    }
}