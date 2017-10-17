using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArloBotSimulationController : MonoBehaviour {

    [SerializeField] private Arlobot _simulatedRobot;

    [Header("Simulation Properties")]
    [SerializeField] private bool _usePingSimulation;
    [SerializeField] private float _minPing;
    [SerializeField] private float _maxPing;

    [Header("Sensors")]
    [SerializeField] private float _sensorPollTimeMs = 50;
    [SerializeField] private List<SensorVisualiser> _sensorVisualisers;
    
    private float _sensorPollTimer;

    void Update()
    {

        _sensorPollTimer += Time.deltaTime;
        if (_sensorPollTimer >= _sensorPollTimeMs/1000f)
        {
            StartCoroutine(SimulatePing(() => {
                VisualizeSensors(_simulatedRobot.GetSensorData(typeof(Ultrasound)));
            }, GetPing()));
            _sensorPollTimer = 0;
        }
    }

    private float GetPing()
    {
        return _usePingSimulation ? UnityEngine.Random.Range(_minPing, _maxPing) : 0;
    }

    private IEnumerator SimulatePing(Action action, float ping)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(_minPing, _maxPing)/1000f);
        action();
    }

    private void VisualizeSensors(SensorDataDTO data)
    {
        foreach (SensorData sensorData in data.Data)
        {
            foreach (SensorVisualiser visualiser in _sensorVisualisers)
            {
                if (visualiser.SensorId == sensorData.SensorId)
                    visualiser.HandleData(sensorData);
            }
        }
    }
    
}
