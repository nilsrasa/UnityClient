using System.Collections;
using UnityEngine;
using UnityEngine.UI;

//Controls the camera viewport in front of the player
public class Viewport : MonoBehaviour
{
    public static Viewport Instance { get; private set; }

    [SerializeField] private Transform _objectToFollow;
    [SerializeField] private float _maxRotationSpeed = 1;
    [SerializeField] private float _animationSpeed = 2;
    [SerializeField] private Texture _viewportDrivingMask;
    [SerializeField] private Texture _viewportDefaultMask;

    public bool SlerpRotation;
    public Canvas ViewportCanvas;

    private RectTransform _canvasRectTransform;
    private RawImage _viewportImage;
    private bool _isLocked;

    void Awake()
    {
        Instance = this;
        _canvasRectTransform = ViewportCanvas.GetComponent<RectTransform>();
        _viewportImage = GetComponentInChildren<RawImage>();
    }

    void Start()
    {
        KeyboardController.Instance.Opened += LockScreenToCenter;
        KeyboardController.Instance.Closed += UnlockScreen;
    }

    void Update()
    {
        if (_isLocked) return;
        if (SlerpRotation)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, _objectToFollow.rotation,
                _maxRotationSpeed * Time.deltaTime * Quaternion.Angle(transform.rotation, _objectToFollow.rotation));
        else
            transform.rotation = _objectToFollow.rotation;
    }

    private IEnumerator AnimateCanvas(bool enlarge)
    {
        float animationTimer = 0;
        float target = 0;
        float origin = 0;
        if (enlarge)
        {
            target = 1;
            _canvasRectTransform.localScale = new Vector3(0, 0.2f, 1);
        }
        else
        {
            origin = 1;
            _canvasRectTransform.localScale = new Vector3(1, 0.8f, 1);
        }
        while (_canvasRectTransform.localScale.x != target)
        {
            _canvasRectTransform.localScale = Vector3.Slerp(_canvasRectTransform.localScale, new Vector3(target, origin, 1), animationTimer / (_animationSpeed / 2));

            animationTimer += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }
        while (_canvasRectTransform.localScale.y != target) {
            _canvasRectTransform.localScale = Vector3.Slerp(_canvasRectTransform.localScale, new Vector3(target, target, 1), animationTimer / _animationSpeed);

            animationTimer += Time.deltaTime;

            yield return new WaitForEndOfFrame();
        }
        ViewportCanvas.enabled = enlarge;
    }

    public void SetEnabled(bool isEnabled)
    {
        if (isEnabled) ViewportCanvas.enabled = true;
        StartCoroutine(AnimateCanvas(isEnabled));
    }

    public void LockScreenToCenter()
    {
        _isLocked = true;
        iTween.RotateTo(gameObject, new Hashtable {{"rotation", Vector3.zero}, {"easetype", iTween.EaseType.easeInOutCirc}, {"time", 0f}});
        _viewportImage.material.SetTexture("_Stencil", _viewportDrivingMask);
    }

    public void UnlockScreen()
    {
        _isLocked = false;
        _viewportImage.material.SetTexture("_Stencil", _viewportDefaultMask);
    }

    public void SetFollowTarget(Transform target)
    {
        _objectToFollow = target;
    }

}
