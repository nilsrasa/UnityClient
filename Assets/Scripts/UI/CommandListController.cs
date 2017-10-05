using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Controller for command list window
public class CommandListController : MonoBehaviour {

    public static CommandListController Instance { get; private set; }

    [SerializeField] private RectTransform _contentWindow;
    [SerializeField] private ScrollRect _scrollRect;
    [SerializeField] private GameObject _commandPrefab;
    [SerializeField] private float _scrollOffsetAmount = 10f;
    
    private List<RectTransform> _commands;
    private bool _isActive;
    private Vector3 _openScale;
    private Vector3 _openPosition;

    void Awake() {
        Instance = this;
        _commands = new List<RectTransform>();
    }

    void Start()
    {
        _openScale = transform.localScale;
        _openPosition = transform.localPosition;
    }

    private void DoneAnimating(bool shouldShow) {
        _isActive = shouldShow;
    }

    public void ShowCommandView() {
        iTween.ScaleTo(gameObject, new Hashtable {{"time", 0.75f}, {"scale", _openScale}, {"easetype", iTween.EaseType.easeInQuart},
            { "onstart", "DoneAnimating"}, { "onstartparams", true } });
        iTween.MoveTo(gameObject, new Hashtable { { "time", 0.75f }, { "position", _openPosition }, { "easetype", iTween.EaseType.easeInQuart }, { "islocal", true } });
    }

    public void HideCommandView() {
        iTween.ScaleTo(gameObject, new Hashtable {{"time", 0.75f}, {"scale", Vector3.zero}, {"easetype", iTween.EaseType.easeInQuart},
            { "oncomplete", "DoneAnimating"}, { "oncompleteparams", false } });
        iTween.MoveTo(gameObject, new Hashtable { { "time", 0.75f }, { "position", new Vector3(_openPosition.x, _openPosition.y-2, _openPosition.z)},
            { "easetype", iTween.EaseType.easeInQuart } , {"islocal", true}});
    }

    public void OnCommandActivated(string command)
    {
        KeyboardController.Instance.SendCommand(command);
    }

    public void OnScroll(bool up)
    {
        float offset = up ? _scrollOffsetAmount : -_scrollOffsetAmount;
        _scrollRect.verticalNormalizedPosition = _scrollRect.verticalNormalizedPosition + offset;
    }

    /// <summary>
    /// Creates command UI element based on what is currently written on keyboard
    /// </summary>
    public void OnAddToDictionary()
    {
        CreateCommand(KeyboardController.Instance.CurrentString);
    }

    public void CreateCommand(string commandText) {
        GameObject command = Instantiate(_commandPrefab, Vector3.zero, Quaternion.identity);
        command.transform.SetParent(_contentWindow, false);
        _commands.Add(command.GetComponent<RectTransform>());
        command.GetComponent<CommandElement>().Initialize(commandText);
        LayoutRebuilder.MarkLayoutForRebuild(_contentWindow);
    }

    public void DestroyCommand(int index) {
        Destroy(_commands[0].gameObject);
        LayoutRebuilder.MarkLayoutForRebuild(_contentWindow);
        _commands.RemoveAt(0);
    }

    public void ToggleCommandView()
    {
        if (_isActive) HideCommandView();
        else ShowCommandView();
    }
}
