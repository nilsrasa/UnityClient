using UnityEngine;

public class Fiducial : MonoBehaviour
{
    [SerializeField] private TextMesh _frontText;
    [SerializeField] private TextMesh _backText;
    [SerializeField] private GameObject _front;
    [SerializeField] private GameObject _back;
    [SerializeField] private Transform _floorMarker;
    [SerializeField] private Transform _line;

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

    public void InitaliseFloorMarker(float heightAboveFloor)
    {
        _line.gameObject.SetActive(true);
        _floorMarker.gameObject.SetActive(true);

        _floorMarker.position = transform.position - new Vector3(0, heightAboveFloor, 0);
        _line.transform.position = (transform.position + _floorMarker.position) / 2;
        _line.transform.localScale = new Vector3(_line.transform.localScale.x, heightAboveFloor, _line.transform.localScale.z);
    }
}