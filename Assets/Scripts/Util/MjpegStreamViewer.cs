using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class MjpegStreamViewer : MonoBehaviour
{
    [SerializeField] private string _streamUrl = "";
    [SerializeField] private float _updateRate = 0.2f;

    private MeshRenderer _mesh;
    private float _timer = 0;

    void Awake()
    {
        _mesh = GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (_streamUrl == "") return;
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            StartCoroutine(UpdateImageFromStream(_streamUrl));
            _timer = _updateRate;
        }
    }

    private IEnumerator UpdateImageFromStream(string url)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();
            Debug.Log("Let's go");

            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Debug.Log(uwr.error);
            }
            else
            {
                // Get downloaded asset bundle
                _mesh.material.mainTexture = DownloadHandlerTexture.GetContent(uwr);
                Debug.Log("DONE");
            }
        }
    }
}