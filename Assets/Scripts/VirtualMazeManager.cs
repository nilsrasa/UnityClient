using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VirtualMazeManager : MonoBehaviour
{
    public int MazeIndex;
    public bool ChangeMaze = false;

    public GameObject VirtualRoom;
    public GameObject VirtualRobot;
    public List<GameObject> Mazes;

    private GameObject CurrentActiveMaze;

	// Use this for initialization
	void Start () {
		
       
	    LoadMaze(MazeIndex);

    }
	
	// Update is called once per frame
	void Update () {


	    if (ChangeMaze)
	    {
	        LoadMaze(MazeIndex);
            ChangeMaze = false;
	    }
	}

    void LoadMaze(int index)
    {
        //if there was a pointer to the maze gameobject delete that 
        if (CurrentActiveMaze)
        {
            Destroy(CurrentActiveMaze);
        }
        //reinstantiate from prefab
        CurrentActiveMaze = Instantiate(Mazes[MazeIndex], VirtualRoom.transform);
        MazeInformation mazeinfo = Mazes[MazeIndex].GetComponent<MazeInformation>();

        VirtualRobot.transform.localPosition = mazeinfo.StartLocation;
        //correct object rotation, since object has wrong axis
        Vector3 objectRot = Quaternion.Euler(0, -90, 0) * mazeinfo.StartFacingDirection; // 
        VirtualRobot.transform.rotation = Quaternion.LookRotation(objectRot, Vector3.up);
    }
}
