using UnityEngine;
using UnityEngine.UI;

public class RobotDebugListItem : MonoBehaviour
{
    [SerializeField] private Text _timestamp;
    [SerializeField] private Text _message;

    public void Initialise(string timestamp, string message)
    {
        _timestamp.text = timestamp;
        _message.text = message;
    }
}