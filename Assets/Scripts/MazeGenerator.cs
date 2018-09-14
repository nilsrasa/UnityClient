using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{

    public Vector2 RoomSize;
    public string MazeName;
   
    //Buttons for actions, for now in inspector
    public bool CreateObstacles = false;
    public bool SaveMaze = false;
    public bool ResetAll = false;
    public bool ResetCurrentSelection = false;

    public string SaveLocation;
    public GameObject ObstaclePrefab;

    private GameObject Floor;
    public List<Vector2> Positions;
    public List<GameObject> MazeComponents;

	// Use this for initialization
	void Start ()
	{
        //setting the background plane to the desired size to fit the outline of the room that the maze will be in
	    Floor = this.transform.GetChild(0).gameObject;
	    Vector3 CurrentFloorScale = Floor.transform.localScale;
	    CurrentFloorScale = new Vector3(RoomSize.x/10, CurrentFloorScale.y, RoomSize.y/10);
	    Floor.transform.localScale = CurrentFloorScale;

        //Save file path
        SaveLocation = Application.dataPath +"/" +SaveLocation;

      //  Debug.Log(Application.dataPath+ );
	}
	
	// Update is called once per frame
	void Update () {
		
        //Onclick send ray from mouse position to world and retrieve the position 
	    if (Input.GetMouseButtonDown(0))
	    {
	        Vector3 MouseLocation = CastRayToWorld();
	      //  Debug.Log("World point " + MouseLocation);
            Vector2 PointCoords = new Vector2(MouseLocation.x,MouseLocation.z);
	       // Debug.Log("PointCoords " + PointCoords);
	            
            if (ValidCoordinatePoint(PointCoords))
	        {
                //Save location
	            Positions.Add(PointCoords);
	        }
	    }

	    if (CreateObstacles)
	    {
            CreateMaze();
	        CreateObstacles = false;
	    }

	    if (ResetAll)
	    {
            ResetEverything();
	        ResetAll = false;
	    }

	    if (ResetCurrentSelection)
	    {
            ResetCurrentPoints();
	        ResetCurrentSelection = false;

	    }
	    if (SaveMaze)
	    {
            SaveMazePrefab();
	        SaveMaze = false;
	    }

	}

    //creates a maze in order from the click points
    //Requires at least two points
    void CreateMaze()
    {

        if (Positions.Count >= 2)
        {
            for (int i = 0; i < Positions.Count -1; i++)
            {
                Debug.Log("Generating obstacle");
                //Take two points and generate prefab correctly
                Vector2 p1 = Positions[i];
               // Debug.Log(p1);
                Vector2 p2 = Positions[i + 1];
               // Debug.Log(p2);
                float ObstacleSize = (p2-p1).magnitude;
                Vector3 NewPosition = new Vector3((p1.x + p2.x) / 2.0f , 0f , (p1.y + p2.y) / 2.0f);

                //Debug.Log(NewPosition);
                Vector2 tempVec1 = p2 - p1;
                float angle = Vector2.SignedAngle(tempVec1, Vector2.up);

                Debug.Log("Angle : " + angle);

                GameObject NewObstacle = Instantiate(ObstaclePrefab);
                NewObstacle.transform.position = NewPosition;
                NewObstacle.transform.localScale = new Vector3(1,1,ObstacleSize); 
                NewObstacle.transform.Rotate(Vector3.up,angle);
                MazeComponents.Add(NewObstacle);

                
            }
            Positions.Clear();
        }
        else
        {
            Debug.Log("Not enough points to generate obstacles");
        }
    }


    void SaveMazePrefab()
    {
        GameObject NewMaze = new GameObject(MazeName);
        foreach (GameObject component in MazeComponents)
        {
            component.transform.parent = NewMaze.transform;
        }

        //PrefabUtility.CreatePrefab(SaveLocation+"/"+MazeName+".prefab",NewMaze);
        string savepath = SaveLocation + "/" + MazeName + ".prefab";

        if (savepath.StartsWith(Application.dataPath))
        {
            savepath = "Assets" + savepath.Substring(Application.dataPath.Length);
        }
        Debug.Log(savepath);
        GameObject prefab = PrefabUtility.CreatePrefab(savepath, NewMaze);
        PrefabUtility.ReplacePrefab(NewMaze, prefab, ReplacePrefabOptions.ConnectToPrefab);

        ResetEverything();
    }

    Vector3 CastRayToWorld()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return ray.origin + (ray.direction * 20);
       // Debug.Log("World point " + point);
    }


    bool ValidCoordinatePoint(Vector2 ClickCoords)
    {
        //If click location is within the room bounds
        if (Mathf.Abs(ClickCoords.x) <= (RoomSize.x / 2.0) &&
            Mathf.Abs(ClickCoords.y) <= (RoomSize.y / 2.0))
        {
            return true;
        }
        else return false;

    }

    void ResetEverything()
    {
        Positions.Clear();
        MazeComponents.Clear();
    }

    void ResetCurrentPoints()
    {
        Positions.Clear();
    }
}
