using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using UnityEngine;

public class CoordinateTester : MonoBehaviour
{
    [SerializeField] private Transform _testPoint;
    [SerializeField] private bool _test;

    [SerializeField] private double lon = 0;
    [SerializeField] private double lat = 0;
    [SerializeField] private bool _testCoord;

    [SerializeField] private Transform _testDistancePointA;
    [SerializeField] private Transform _testDistancePointB;
    [SerializeField] private bool _testDistance;
    // Update is called once per frame
    void Update () {
	    if (Input.GetKeyDown(KeyCode.Space))
	    {
	        GeoPointMercator coordinates = transform.position.ToMercator();
	        Debug.Log("Zero - Mercator: " + coordinates);
	        Debug.Log("Zero - WGS84: " + coordinates.ToWGS84());
        }

	    if (_test)
	    {
	        GeoPointMercator coordinatesMercator = _testPoint.transform.position.ToMercator();
	        Debug.Log("Zero - Mercator: " + coordinatesMercator);
	        GeoPointWGS84 wgs84 = coordinatesMercator.ToWGS84();
            Debug.Log("Zero - WGS84: " + wgs84);

            _test = false;
	    }
	    if (_testCoord) {
	        GeoPointWGS84 wgs84 = new GeoPointWGS84 {
	            latitude = lat,
	            longitude = lon,
	            altitude = 0,
	        };

	        GameObject point = GameObject.CreatePrimitive(PrimitiveType.Sphere);
	        point.transform.position = wgs84.ToMercator().ToUnity();
	        _testCoord = false;
	    }
        if (_testDistance)
        {
            Debug.Log(Vector3.Distance(_testDistancePointA.position, _testDistancePointB.position) + "meters");
            _testDistance = false;
        }
    }
}
