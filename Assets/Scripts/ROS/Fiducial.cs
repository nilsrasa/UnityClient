using UnityEngine;

public class Fiducial : MonoBehaviour
{
    [SerializeField] private TextMesh _frontText;
    [SerializeField] private TextMesh _backText;
    [SerializeField] private GameObject _front;
    [SerializeField] private GameObject _back;

    private int _fiducialId;

    public int FiducialId
    {
        get { return _fiducialId; }
        set
        {
            _fiducialId = value;
            _frontText.text = value.ToString();
            _backText.text = value.ToString();
        }
    }

    private SphereCollider _sphereCollider;

    void Awake()
    {
        _sphereCollider = GetComponent<SphereCollider>();
    }

    void Update()
    {
        if (transform.hasChanged)
        {
            if (Vector3.Dot(transform.up, Vector3.down) < 0)
            {
                _front.SetActive(true);
                _back.SetActive(false);
            }
            else
            {
                _front.SetActive(false);
                _back.SetActive(true);
            }
            transform.hasChanged = false;
        }
    }

    public void SetCollider(bool isEnabled)
    {
        _sphereCollider.enabled = isEnabled;
    }
}