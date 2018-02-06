using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoordinateTester : MonoBehaviour
{
    [SerializeField] private Transform _testPoint;
    [SerializeField] private bool _test;

    [SerializeField] private GeoPointWGS84 _wgsPointToTest;
    [SerializeField] private bool _testCoord;

    [SerializeField] private Transform _testDistancePointA;
    [SerializeField] private Transform _testDistancePointB;
    [SerializeField] private bool _testDistance;

    [SerializeField] private Transform _testOrientationPoint;
    [SerializeField] private bool _testOrientation;

    private int i = 10;
    // Update is called once per frame
    void Update () {
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
	    if (_testCoord) {
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
        Debug.DrawRay(transform.position, transform.forward * 100);
    }
}
