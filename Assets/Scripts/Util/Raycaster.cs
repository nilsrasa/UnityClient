using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
/ A simple sensor raycaster. Shoots a ray every frame in front of the sensor gameobject, and saves the distance to the first obstacle 
/ that will be hit. SensorManager could also provide common information to all individual sensors.
*/

public class Raycaster : MonoBehaviour
{
    //the following information will be saved appropriately by the sensor manager, in order to serialise
    public float RaycastRange;
    public float DistanceToObject;
    public string ObjectType = "Nothing";
	
	// Maybe instead of every frame, I could shoot rays every certain intervals
	void Update () {

	    RaycastHit hit;
	    // Does the ray intersect any objects excluding the player layer
	    if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, RaycastRange))
	    {
	        DistanceToObject = hit.distance;
	        ObjectType = hit.collider.gameObject.tag;
	        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.red);
	       // Debug.Log("Did Hit");
	    }
	    else
	    {
	        ObjectType = "Nothing";
            DistanceToObject = 0.0f;
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * RaycastRange, Color.green);
	       // Debug.Log("Did not Hit");
	    }
    }
}
