using UnityEngine;

//A helper script that mimics position and/or rotation of another object
public class FollowObject : MonoBehaviour
{
    public Transform ObjectToFollow;
    [Header("Position")] [SerializeField] private bool _position;
    [SerializeField] private bool _isLocalPosition;
    [SerializeField] private Vector3 _positionalOffset = Vector3.zero;

    [Header("Rotation")] [SerializeField] private bool _rotation;
    [SerializeField] private bool _isLocalRotation;
    [SerializeField] private Vector3 _rotationalOffset = Vector3.zero;

    private Vector3 _orgRotation;

    void Awake()
    {
        _orgRotation = transform.eulerAngles;
    }

    void Update()
    {
        if (_position)
        {
            if (_isLocalPosition)
                transform.localPosition = ObjectToFollow.position + _positionalOffset;
            else
                transform.position = ObjectToFollow.position + _positionalOffset;
        }
        if (_rotation)
        {
            if (_isLocalRotation)
                transform.localRotation = Quaternion.Euler(ObjectToFollow.rotation.eulerAngles + _rotationalOffset);
            else
                transform.rotation = Quaternion.Euler(ObjectToFollow.rotation.eulerAngles + _rotationalOffset + _orgRotation);
        }
    }

    public void SetPositionalOffset(Vector3 newOffset)
    {
        _positionalOffset = newOffset;
    }

    public void SetRotationalOffset(Vector3 newOffset)
    {
        _rotationalOffset = newOffset;
    }
}