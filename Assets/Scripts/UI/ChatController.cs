using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

//Controller for chat window
public class ChatController : MonoBehaviour
{
    public static ChatController Instance { get; private set; }

    [SerializeField] private RectTransform _contentWindow;
    [SerializeField] private ScrollView _scrollView;
    [SerializeField] private GameObject _chatPrefab;
    [SerializeField] private int _maxMessages = 15;

    [Space(10)] [Header("Debug Chat")] [SerializeField] private bool _useFakeChat;
    [SerializeField] private float _minChatSpamTime = 1;
    [SerializeField] private float _maxChatSpamTime = 10;

    private List<RectTransform> _chatmessages;
    private float _fakeChatTimer;
    private float _fakeChatTimeLimit;
    private bool _isActive;
    private Vector3 _openScale;
    private Vector3 _openPosition;

    void Awake()
    {
        Instance = this;
        _chatmessages = new List<RectTransform>();
        _fakeChatTimeLimit = Random.Range(_minChatSpamTime, _maxChatSpamTime);
    }

    void Start()
    {
        _openScale = transform.localScale;
        _openPosition = transform.localPosition;
    }

    void Update()
    {
        if (_useFakeChat)
        {
            if (_fakeChatTimer >= _fakeChatTimeLimit)
            {
                GenerateFakeChat();
                _fakeChatTimer = 0;
                _fakeChatTimeLimit = Random.Range(_minChatSpamTime, _maxChatSpamTime);
            }
            else
                _fakeChatTimer += Time.deltaTime;
        }
    }

    //Generates fake chat messages based of already input names and messages for showcase purposes
    private void GenerateFakeChat()
    {
        string fakeName = FakeChatMessages.ChatNames[Random.Range(0, FakeChatMessages.ChatNames.Count)];
        string fakeMessage = FakeChatMessages.ChatMessages[Random.Range(0, FakeChatMessages.ChatMessages.Count)];
        CreateChatMessage(fakeName, fakeMessage, false);
    }

    private void DoneAnimating(bool shouldShow)
    {
        //_canvas.enabled = shouldShow;
        _isActive = shouldShow;
    }

    public void ShowChat()
    {
        iTween.ScaleTo(gameObject, new Hashtable
        {
            {"time", 0.75f},
            {"scale", _openScale},
            {"easetype", iTween.EaseType.easeInQuart},
            {"onstart", "DoneAnimating"},
            {"onstartparams", true}
        });
        iTween.MoveTo(gameObject, new Hashtable {{"time", 0.75f}, {"position", _openPosition}, {"easetype", iTween.EaseType.easeInQuart}, {"islocal", true}});
    }

    public void HideChat()
    {
        iTween.ScaleTo(gameObject, new Hashtable
        {
            {"time", 0.75f},
            {"scale", Vector3.zero},
            {"easetype", iTween.EaseType.easeInQuart},
            {"oncomplete", "DoneAnimating"},
            {"oncompleteparams", false}
        });
        iTween.MoveTo(gameObject, new Hashtable
        {
            {"time", 0.75f},
            {"position", new Vector3(_openPosition.x, _openPosition.y - 2, _openPosition.z)},
            {"easetype", iTween.EaseType.easeInQuart},
            {"islocal", true}
        });
    }

    /// <summary>
    /// Creates UI element in chat window with certain text. 
    /// </summary>
    /// <param name="senderName"> Name of sender.</param>
    /// <param name="messageText"> Contents of chat message.</param>
    /// <param name="isFromPlayer"> If message was sent by the player, highlighting it..</param>
    public void CreateChatMessage(string senderName, string messageText, bool isFromPlayer)
    {
        GameObject chat = Instantiate(_chatPrefab, Vector3.zero, Quaternion.identity);
        chat.transform.SetParent(_contentWindow, false);
        _chatmessages.Add(chat.GetComponent<RectTransform>());
        chat.GetComponent<ChatMessage>().Initialize(senderName, messageText, isFromPlayer);
        if (_chatmessages.Count > _maxMessages)
        {
            Destroy(_chatmessages[0].gameObject);
            _chatmessages.RemoveAt(0);
        }
        LayoutRebuilder.MarkLayoutForRebuild(_contentWindow);
    }

    public void DestroyChatMessage(int index)
    {
        Destroy(_chatmessages[0].gameObject);
        LayoutRebuilder.MarkLayoutForRebuild(_contentWindow);
        _chatmessages.RemoveAt(0);
    }

    public void ToggleChat()
    {
        if (_isActive) HideChat();
        else ShowChat();
    }
}