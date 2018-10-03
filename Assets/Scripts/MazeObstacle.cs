using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MazeObstacle : MonoBehaviour
{

    [Header("Adjacent obstacle distances")]
    public float YellowObstacle = 0;
    public float GreenRay = 0;
    public float RedRay = 0;
    public float BlueRay = 0;

    private float RaycastRange = 100;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        //Shoot 4 rays with 90 degrees around the object if it is the one selected
        if (Selection.activeGameObject == this.gameObject)
        {

            RaycastHit hit;
            //Top
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, RaycastRange))
            {
                RedRay = hit.distance;
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * hit.distance, Color.red);

            }
            else
            {
                RedRay = 0.0f;
                Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * RaycastRange, Color.red);
            }


            //   //Bottom
            if (Physics.Raycast(transform.position, transform.TransformDirection(-Vector3.forward), out hit, RaycastRange))
            {
                BlueRay = hit.distance;
                Debug.DrawRay(transform.position, transform.TransformDirection(-Vector3.forward) * hit.distance, Color.blue);

            }
            else
            {

                BlueRay = 0.0f;
                Debug.DrawRay(transform.position, transform.TransformDirection(-Vector3.forward) * RaycastRange, Color.blue);

            }

            //Left
            if (Physics.Raycast(transform.position + transform.TransformDirection(-Vector3.right) * 0.05f, transform.TransformDirection(-Vector3.right), out hit, RaycastRange))
            {

                YellowObstacle = hit.distance;
                Debug.DrawRay(transform.position + transform.TransformDirection(-Vector3.right) * 0.05f, transform.TransformDirection(-Vector3.right) * hit.distance, Color.yellow);

            }
            else
            {

                YellowObstacle = 0.0f;
                Debug.DrawRay(transform.position + transform.TransformDirection(-Vector3.right) * 0.05f, transform.TransformDirection(-Vector3.right) * RaycastRange, Color.yellow);

            }

            //Right

            if (Physics.Raycast(transform.position + transform.TransformDirection(Vector3.right) * 0.05f, transform.TransformDirection(Vector3.right), out hit, RaycastRange))
            {

                GreenRay = hit.distance;
                Debug.DrawRay(transform.position + transform.TransformDirection(Vector3.right) * 0.05f, transform.TransformDirection(Vector3.right) * hit.distance, Color.green);

            }
            else
            {

                GreenRay = 0.0f;
                Debug.DrawRay(transform.position + transform.TransformDirection(Vector3.right) * 0.05f, transform.TransformDirection(Vector3.right) * RaycastRange, Color.green);

            }
        }


    }
}
