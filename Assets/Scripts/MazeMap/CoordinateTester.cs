using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
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
	    if (Input.GetKeyDown(KeyCode.Space))
	    {
            ArlobotROSController.Instance.MoveToPoint(transform.position.ToMercator().ToWGS84());
        }

	    if (_test)
	    {
	        GeoPointMercator coordinatesMercator = _testPoint.transform.position.ToMercator();
	        Debug.Log("Test point - Mercator: " + coordinatesMercator);
	        GeoPointWGS84 wgs84 = coordinatesMercator.ToWGS84();
            Debug.Log("Test point - WGS84: " + wgs84);
	        GeoPointUTM utm = wgs84.ToUTM();
            Debug.Log("Test point - UTM: " + utm);

            _test = false;
	    }
	    if (_testCoord) {
	        GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
	        point.transform.position = _wgsPointToTest.ToMercator().ToUnity();
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
