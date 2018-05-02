using UnityEngine;

public class CoordinateTester : MonoBehaviour
{
    [Header("Coordinates from point")] [SerializeField] private Transform _testPoint;
    [SerializeField] private bool _test;

    [Header("Find WGS Coordinate")] [SerializeField] private GeoPointWGS84 _wgsPointToTest;
    [SerializeField] private bool _testCoord;

    [Header("Distance between two points")] [SerializeField] private Transform _testDistancePointA;
    [SerializeField] private Transform _testDistancePointB;
    [SerializeField] private bool _testDistance;

    [Header("Look at object")] [SerializeField] private Transform _testOrientationPoint;
    [SerializeField] private bool _testOrientation;

    [Header("Find distance point")] [SerializeField] private float _distance;
    [SerializeField] private bool _testDistancePoint;

    [Header("Move to distance")] [SerializeField] private float _moveDistance;
    [SerializeField] private bool _moveToDistance;

    private int i = 10;

    // Update is called once per frame
    void Update()
    {
        if (_test)
        {
            GeoPointUTM coordinatesUtm = _testPoint.transform.position.ToUTM();
            Debug.Log("Test point - UTM: " + coordinatesUtm);
            GeoPointWGS84 wgs84 = coordinatesUtm.ToWGS84();
            Debug.Log("Test point - WGS84: " + wgs84);
            GeoPointMercator utm = coordinatesUtm.ToMercator();
            Debug.Log("Test point - Mercator: " + utm);

            _test = false;
        }
        if (_testCoord)
        {
            GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            point.transform.position = _wgsPointToTest.ToUTM().ToUnity();
            _testCoord = false;
        }
        if (_testDistance)
        {
            Debug.Log(Vector3.Distance(_testDistancePointA.position, _testDistancePointB.position) + "meters");
            _testDistance = false;
        }

        if (_testOrientation)
        {
            transform.LookAt(_testOrientationPoint);
            _testOrientation = false;
        }

        if (_testDistancePoint)
        {
            GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            point.transform.position = transform.position + transform.forward * _distance;
            _testDistancePoint = false;
        }

        if (_moveToDistance)
        {
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, float.PositiveInfinity))
            {
                float offset = hit.distance - _moveDistance;
                transform.position = transform.position + transform.forward * offset;
            }
            _moveToDistance = false;
        }

        Debug.DrawRay(transform.position, transform.forward * 100);
    }
}