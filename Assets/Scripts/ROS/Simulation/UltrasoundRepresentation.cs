using System.Collections.Generic;
using UnityEngine;

public class UltrasoundRepresentation : SensorRepresentation
{
    [SerializeField] private string _sensorId;
    [SerializeField] private List<Sprite> _ultrasoundLevels;
    [SerializeField] private float _sensorMaxValue = 3;

    private SpriteRenderer _spriteRenderer;
    private bool _hasData;
    private float _dataToHandle;

    void Awake()
    {
        SensorId = _sensorId;
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start() 
    {
        SensorRepresentationBusController.Instance.Register<UltrasoundRepresentationBus>(this);
    }

    void Update() 
    {
        if (_hasData) {
            _spriteRenderer.sprite = _ultrasoundLevels[GetLevelOfValue(_dataToHandle, _sensorMaxValue, _ultrasoundLevels.Count)];
            _hasData = false;
        }
    }

    public override void HandleData(object value)
    {
        float reading = (float) value;
        _dataToHandle = reading;
        _hasData = true;
    }

}
