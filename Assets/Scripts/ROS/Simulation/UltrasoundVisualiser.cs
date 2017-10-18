using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UltrasoundVisualiser : SensorVisualiser
{
    [SerializeField] private string _sensorId;
    [SerializeField] private List<Sprite> _ultrasoundLevels;
    [SerializeField] private float _sensorMaxValue = 3;
    //[SerializeField] private Transform _wall;

    private SpriteRenderer _spriteRenderer;

    void Awake()
    {
        SensorId = _sensorId;
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private int GetLevelOfValue(float value, float maxValue, int steps) {
        float stepSize = maxValue / steps;

        for (int i = 0; i < steps; i++) {
            if (value > i * stepSize && value < (i + 1) * stepSize) {
                return i;
            }
        }
        return steps - 1;
    }

    public override void HandleData(SensorData data)
    {
        _spriteRenderer.sprite = _ultrasoundLevels[GetLevelOfValue(float.Parse(data.Value), _sensorMaxValue, _ultrasoundLevels.Count)];
       //_wall.position = transform.parent.position + (transform.position-transform.parent.position ).normalized * ((float.Parse(data.Value) / 100f) + 0.4f);
        //_wall.LookAt(transform);
    }
}
