using System.Collections;
using UnityEngine;
using UnityEngine.Video;

//Video player for 360 video for testing
public class TestVideoPlayer : MonoBehaviour
{
    private VideoPlayer _videoPlayer;

    void Awake()
    {
        _videoPlayer = GetComponent<VideoPlayer>();
    }

    void Start()
    {
        StartCoroutine(playVideo());
    }

    void Update()
    {
    }

    private IEnumerator playVideo()
    {
        _videoPlayer.errorReceived += VideoPlayer_errorReceived;
        _videoPlayer.prepareCompleted += VideoPlayer_prepareCompleted;

        _videoPlayer.playOnAwake = false;

        _videoPlayer.source = VideoSource.Url;
        _videoPlayer.url = "http://localhost:8081/video.mjpg?q=75&fps=33&id=0.8826213513689396&r=1519218756772";
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

        Debug.Log("will prepare Video");

        _videoPlayer.Prepare();
        yield return 0;
    }

    private void VideoPlayer_prepareCompleted(VideoPlayer source)
    {
        Debug.Log("Done Preparing Video");
        //Play Video
        _videoPlayer.Play();
    }

    private void VideoPlayer_errorReceived(VideoPlayer source, string message)
    {
        Debug.Log("Error:" + message);
    }
}