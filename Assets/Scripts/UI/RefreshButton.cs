using UnityEngine;

public class RefreshButton : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = -150;

    private bool _shouldRotate;

    public bool ShouldRotate
    {
        get { return _shouldRotate; }
        set
        {
            _shouldRotate = value;
            if (!value) 
                transform.rotation = Quaternion.identity;
        }
    }

    void Update()
    {
        if (_shouldRotate) 
            transform.localEulerAngles += new Vector3(0, 0, Time.deltaTime * _rotationSpeed);
    }
}
