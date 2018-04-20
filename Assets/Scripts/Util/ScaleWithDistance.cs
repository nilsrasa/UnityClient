using UnityEngine;

public class ScaleWithDistance : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private float _maxSize = 3;
    [SerializeField] private float _minSize = 0.1f;
    [SerializeField] private float _scaleMultiplier = 1;

    private float _originalScale;

    void Awake ()
    {
        if (_camera == null)
            _camera = Camera.main;
        _originalScale = transform.localScale.x;
    }
	
	void LateUpdate ()
	{
	    float distanceToCamera = Vector3.Distance(_camera.transform.position, transform.position);
	    float scale = Mathf.Clamp(_originalScale * distanceToCamera * _scaleMultiplier, _minSize, _maxSize);
        transform.localScale = new Vector3(scale, scale, scale);
    }
}
