using System.Collections;
using UnityEngine;
using UnityEngine.Video;

//Video player for 360 video for testing
public class TestVideoPlayer : MonoBehaviour
{

    private VideoPlayer _videoPlayer;
    private VideoSource _videoSource;
    private AudioSource _audioSource;

    void Awake()
    {
        _videoPlayer = GetComponent<VideoPlayer>();
        _audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        StartCoroutine(playVideo());
    }
	
	void Update () {
		
	}

    private IEnumerator playVideo() {
        _videoPlayer.errorReceived += VideoPlayer_errorReceived;
        _videoPlayer.prepareCompleted += VideoPlayer_prepareCompleted;

        _videoPlayer.playOnAwake = false;
        _audioSource.playOnAwake = false;

        _audioSource.Pause();

        _videoPlayer.source = VideoSource.Url;
        _videoPlayer.url = "http://docs.evostream.com/sample_content/assets/bun33s.mp4";
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        _videoPlayer.EnableAudioTrack(0, true);
        _videoPlayer.SetTargetAudioSource(0, _audioSource);

        Debug.Log("will prepare Video");

        _videoPlayer.Prepare();
        yield return 0;
    }

    private void VideoPlayer_prepareCompleted(VideoPlayer source) {
        Debug.Log("Done Preparing Video");
        //Play Video
        _videoPlayer.Play();
        //Play Sound
        _audioSource.Play();
    }
    private void VideoPlayer_errorReceived(VideoPlayer source, string message) {
        Debug.Log("Error:" + message);

    }
    
}
