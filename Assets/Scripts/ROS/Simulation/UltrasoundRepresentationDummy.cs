using System.Collections.Generic;
using UnityEngine;

public class UltrasoundRepresentationDummy : SensorRepresentation
{
    [SerializeField] private List<Sprite> _ultrasoundLevels;

    private SpriteRenderer _spriteRenderer;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        Ray ray = new Ray(transform.position - transform.right * 0.25f, transform.right * 3);
        RaycastHit hit;
        float sensorData = 3;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.distance < 0.2f)
                sensorData = 0.2f;
            else if (hit.distance > 3)
                sensorData = 3;
            else
                sensorData = hit.distance;
        }
        _spriteRenderer.sprite = _ultrasoundLevels[GetLevelOfValue(sensorData, 3, _ultrasoundLevels.Count)];
    }
}