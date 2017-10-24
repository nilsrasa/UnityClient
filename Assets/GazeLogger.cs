using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class GazeLogger : MonoBehaviour
{
    private string FILEPATH;
    private readonly string DELIMITER = ",";

    [SerializeField] private List<GazeObject> _loggedGazeObjects;
    private List<GazeObject> _gazeObjectsHovered;
    private Dictionary<GazeObject, GazeLog> _logDictionary;

	// Use this for initialization
	void Start () {
        FILEPATH = Application.streamingAssetsPath + "/GazeLog-" + DateTime.Now + ".csv";
	    if (!File.Exists(FILEPATH))
	        File.Create(FILEPATH);
        _gazeObjectsHovered = new List<GazeObject>();
        _logDictionary = new Dictionary<GazeObject, GazeLog>();
	    foreach (GazeObject gazeObject in _loggedGazeObjects)
	    {
	        _logDictionary.Add(gazeObject, new GazeLog());
	        gazeObject.Hovered += OnGazeObjectHovered;
	        gazeObject.Unhovered += OnGazeObjectUnhovered;
	    }
	}

    void Update()
    {
        foreach (GazeObject gazeObject in _gazeObjectsHovered)
        {
            GazeLog log = new GazeLog();
            log.GazeTime = _logDictionary[gazeObject].GazeTime + Time.deltaTime;
            _logDictionary.Remove(gazeObject);
            _logDictionary.Add(gazeObject, log);
        }

        if (Input.GetKeyDown(KeyCode.F))
            OutputToFile();
    }
	
    private struct GazeLog
    {
        public float GazeTime;
    }

    private void OnGazeObjectHovered(GazeObject sender)
    {
        if (!_gazeObjectsHovered.Contains(sender))
            _gazeObjectsHovered.Add(sender);    
    }

    private void OnGazeObjectUnhovered(GazeObject sender)
    {
        if (_gazeObjectsHovered.Contains(sender))
            _gazeObjectsHovered.Remove(sender);
    }

    public void OutputToFile()
    {
        StringBuilder csvFile = new StringBuilder();

        foreach (GazeObject key in _logDictionary.Keys)
        {
            string newline = string.Format("{0}, {1}", key.gameObject.name, _logDictionary[key].GazeTime);
            csvFile.AppendLine(newline);
        }

        File.WriteAllText(FILEPATH, csvFile.ToString());
        
    }
}
