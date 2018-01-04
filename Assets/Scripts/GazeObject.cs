using UnityEngine;
using UnityEngine.Events;

//Base class of all gaze-interacted objects. 
public class GazeObject : MonoBehaviour {

    [SerializeField] protected float _dwellTime;
    [SerializeField] protected bool _oneTimeUse;
    [SerializeField] protected bool _retainAccumulatedDwell;
    [SerializeField] protected bool _useToggle;
    [SerializeField] protected bool _resetOnUnhover = true;
    [SerializeField] protected bool _startStatus;

    [SerializeField] protected UnityEvent _onActivate;

    protected float _dwellTimer;
    protected bool _locked;
    protected BoxCollider _collider;
    protected RectTransform _rect;
    protected bool _isEnabled = true;

    public delegate void OnActivated(GazeObject button);
    public event OnActivated Activated;
    public delegate void OnHovered(GazeObject button);
    public event OnHovered Hovered;
    public delegate void OnUnhovered(GazeObject button);
    public event OnUnhovered Unhovered;
    public bool Gazed { get; protected set; }
    public bool IsActivated { get; protected set; }

    protected virtual void Awake() {
        _collider = GetComponent<BoxCollider>();
        IsActivated = _startStatus;
        _rect = GetComponent<RectTransform>();
    }

    protected virtual void Start()
    {
        _dwellTime = _dwellTime / 1000f;
    }

    protected virtual void Update() {
        if (!Gazed || _locked || IsActivated && !_useToggle) return;

        if (_dwellTimer < _dwellTime) {
            _dwellTimer += Time.deltaTime;
        }
        else if (_dwellTimer >= _dwellTime) {
            Activate();
        }
    }

    protected virtual void Activate() {
        if (IsActivated && !_useToggle) return;
        _onActivate.Invoke();
        _dwellTimer = 0;

        if (_resetOnUnhover)
            _locked = true;
        if (_useToggle)
            IsActivated = !IsActivated;
        else IsActivated = true;

        Activated?.Invoke(this);
    }

    public virtual void OnHover() {
        Gazed = true;
        Hovered?.Invoke(this);
    }

    public virtual void OnUnhover() {
        if (IsActivated && _oneTimeUse) return;
        Gazed = false;
        _locked = false;
        Unhovered?.Invoke(this);

        if (!_retainAccumulatedDwell) _dwellTimer = 0;
        if (!_useToggle)
            IsActivated = false;
    }

    public virtual void SetEnabled(bool isEnabled) {
        _collider.enabled = isEnabled;
        _isEnabled = isEnabled;
    }

    public virtual void SetSize(Vector2 sizeDelta)
    {
        _rect.sizeDelta = sizeDelta;
        _collider.size = new Vector3(sizeDelta.x, sizeDelta.y, _collider.size.z);
    }

    public virtual void SetState(bool isOn)
    {
        IsActivated = isOn;
    }
}
