using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

public class UrdfModelGenerator : MonoBehaviour
{

    [SerializeField] private GameObject _successText;

    private string _urdfFolderPath;
    private int _robotsToLoad;

    void Start()
    {
        string[] paths = Directory.GetFiles(Application.streamingAssetsPath + "/URDF/");
        string[] filteredPaths = paths.Where(p => !p.Contains(".meta")).ToArray();
        _robotsToLoad = filteredPaths.Length;
        foreach (string path in filteredPaths)
        {
            StartCoroutine(GenerateRobot(path));
        }
    }

    private IEnumerator GenerateRobot(string path)
    {
        string urd = File.ReadAllText(path);
        RobotUrdfUtility.GenerateRobotGameObjectFromDescription(urd);
        yield return new WaitForEndOfFrame();
        LoadDone();
    }

    private void LoadDone()
    {
        _robotsToLoad--;
        if (_robotsToLoad == 0)
            _successText.SetActive(true);
    }
}
