using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotMasterController : MonoBehaviour
{
    public static RobotMasterController Instance { get; set; }

    [SerializeField] private List<ROSController> _activeRobots;

    private int _selectedRobotIndex;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        
    }

    public ROSController GetNextRobot()
    {
        if (_activeRobots.Count == 0) return null;

        _selectedRobotIndex++;
        if (_selectedRobotIndex > _activeRobots.Count - 1)
            _selectedRobotIndex = 0;
        return _activeRobots[_selectedRobotIndex];
    }
}
