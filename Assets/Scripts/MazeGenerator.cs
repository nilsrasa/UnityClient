using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEditor;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{

    //Public variables and settings

    //---------------------------------------------------------------------------------------------------------
    [Header("General init settings")]
    [Tooltip("Roomsize in units/meters")]
    public Vector2 RoomSize;
    [Tooltip("The prefab which will be used to generate the obstacles")]
    public GameObject ObstaclePrefab;

    //---------------------------------------------------------------------------------------------------------
    [Header("Maze creation options")]
    [Tooltip("When active, forces the created obstacles to have only rotations that are multiples of 90 degrees.")]
    public bool SnapAngles = false;
    [Tooltip("When active, forces the created obstacles to have only rotations that are multiples of 90 degrees.")]
    public bool SetLocations = false;
    [Tooltip("Creates obstacles between the positions that the user clicked")]
    public bool CreateObstacles = false;
    [Tooltip("Clears the temporary drawing positions of the user. Meant to be used before obstacles have been generated")]
    public bool ClearClickedPositions = false;
    [Tooltip("Clears the temporary drawing positions, as well as all the created obstacles so far. Does not affect saved prefabs")]
    public bool ResetAll = false;
   
    //---------------------------------------------------------------------------------------------------------

    [Header("Save options")]
    [Tooltip("The name of the maze prefab that will be generated")]
    public string MazeName;
    [Tooltip("Filepath where the prefab will be saved")]
    public string SaveLocation = "Prefabs/VirtualSkylab/VirtualMazes";
    [Tooltip("Saves the current generated obstacles in a prefab")]
    public bool SaveMaze = false;
    //---------------------------------------------------------------------------------------------------------

    [Header("Camera zoom settings")]
    public float CameraMinZoom = 2f;
    public float CameraMaxZoom = 15f;
    public float CameraZoomSpeed = 10f;

    //---------------------------------------------------------------------------------------------------------

    //Private variables
    private GameObject Floor;
    private GameObject ObstacleParent; // used for easily removing all obstacles
    private List<Vector2> TemporaryClickedPositions;
    private List<GameObject> MazeComponents;
    public Vector3 MazeStartLocation;
    public Vector3 StartFacingDirection;
    public Vector3 MazeEndLocation;
  
    //setlocation vars

    private int SetupLocationProgress = 0;
    private Vector2 po1;
    private Vector2 po2;
   

    // Use this for initialization
	void Start ()
	{
        //setting the background plane to the desired size to fit the outline of the room that the maze will be in

        //Assumes that structure is ok and does not check for errors. not safe 
	    Floor = this.transform.GetChild(0).gameObject;
	    ObstacleParent = this.transform.GetChild(1).gameObject;


	    Vector3 CurrentFloorScale = Floor.transform.localScale;
	    CurrentFloorScale = new Vector3(RoomSize.x/10, CurrentFloorScale.y, RoomSize.y/10);
	    Floor.transform.localScale = CurrentFloorScale;

        //Save file path
        SaveLocation = Application.dataPath +"/" +SaveLocation;

        TemporaryClickedPositions = new List<Vector2>();
        MazeComponents = new List<GameObject>();
	   
	}
	
	// Update is called once per frame
	void Update () {


	   
	        //Onclick send ray from mouse position to world and retrieve the position 
	    if (Input.GetMouseButtonDown(0))
	    {
	        Vector3 MouseLocation = CastRayToWorld();
	        Vector2 PointCoords = new Vector2(MouseLocation.x, MouseLocation.z);
           
            if (!SetLocations)
	        {
	          
	            if (ValidCoordinatePoint(PointCoords))
	            {
	                TemporaryClickedPositions.Add(PointCoords);
	            }
	        }
	        else
            {
                SetupLocation(PointCoords);

            }
        }
	      
	    //Camera zoom in/out
        float CameraZoomAmount = Input.GetAxis("Mouse ScrollWheel");
	    Camera.main.orthographicSize =
                 Mathf.Clamp(Camera.main.orthographicSize - CameraZoomAmount * CameraZoomSpeed, CameraMinZoom, CameraMaxZoom);

        //Button commands -- Could be done through UI for more efficiency but this is fine for now
	    {
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

	        if (ClearClickedPositions)
	        {
	            ClearTemporaryPoints();
	            ClearClickedPositions = false;

	        }
	        if (SaveMaze)
	        {
	            SaveMazePrefab();
	            SaveMaze = false;
	        }
	    }

	}

    void SetupLocation(Vector2 MouseCoords)
    {
        // PUT SOME UI TO GUIDE THE PROCESS INSTEAD OF DEBUGS
        switch (SetupLocationProgress)
        {
            //save first position 
            case 0:
            {
                Debug.Log("Initial location saved");
                po1 = MouseCoords;
                SetupLocationProgress++;
                break;
            }
                
            //save second position and calculate initial location transform
            case 1:
            {
                Debug.Log("Rotation calculated");
                po2 = MouseCoords;
                MazeStartLocation = new Vector3(po1.x, 0.1f, po1.y);

                Vector2 temp = po2-po1;
                StartFacingDirection = Vector3.Normalize(new Vector3(temp.x,0.0f,temp.y));
                SetupLocationProgress++;
                break;
            }
            case 2:
            {
                Debug.Log("Final location calculated");
                MazeEndLocation= new Vector3(MouseCoords.x, 0.1f, MouseCoords.y);
                SetupLocationProgress = 0;
                SetLocations = false;
                break;

            } 
            default:
                break;

        }
    }

    

    //creates a maze in order from the click points
    //Requires at least two points
    void CreateMaze()
    {

        if (TemporaryClickedPositions.Count >= 2)
        {
            Debug.Log("Generating obstacles");
            for (int i = 0; i < TemporaryClickedPositions.Count -1; i++)
            {
             
                //Take two points and generate prefab correctly
                Vector2 p1 = TemporaryClickedPositions[i];
                Vector2 p2 = TemporaryClickedPositions[i + 1];
     
                float ObstacleSize = (p2-p1).magnitude;
                Vector3 NewPosition = new Vector3((p1.x + p2.x) / 2.0f , 0f , (p1.y + p2.y) / 2.0f);

                Vector2 tempVec = p2 - p1;
                float angle = Vector2.SignedAngle(tempVec, Vector2.up);

                if (SnapAngles) angle = SnapAngle(angle);
            
                GameObject NewObstacle = Instantiate(ObstaclePrefab);
                NewObstacle.transform.position = NewPosition;
                NewObstacle.transform.localScale = new Vector3(1,1,ObstacleSize); 
                NewObstacle.transform.Rotate(Vector3.up,angle);
                MazeComponents.Add(NewObstacle);
                NewObstacle.transform.parent = ObstacleParent.transform;


            }
            TemporaryClickedPositions.Clear();
        }
        else
        {
            Debug.Log("Not enough points to generate obstacles");
        }
    }


    float SnapAngle(float angle)
    {
        if (angle > -45f && angle <= 45f)
        {
            return 0;
        }
        else if (angle > 45f && angle <= 135f)
        {
            return 90;
        }
        else if (angle <-45f && angle >= -135f )
        {
            return -90;
        }
        else if (angle > 135f || angle < -135)
        {
            return 180;
        }
        else return 0;

    }

    void SaveMazePrefab()
    {
        GameObject NewMaze = new GameObject(MazeName);
        foreach (GameObject component in MazeComponents)
        {
            component.transform.parent = NewMaze.transform;
        }

        //Current global transforms will be assigned to the saved maze
     
        MazeInformation comp = NewMaze.AddComponent<MazeInformation>();
        comp.StartLocation = MazeStartLocation;
        comp.StartFacingDirection = StartFacingDirection;
        comp.FinishLocation = MazeEndLocation;

        string savepath = SaveLocation + "/" + MazeName + ".prefab";

        if (savepath.StartsWith(Application.dataPath))
        {
            savepath = "Assets" + savepath.Substring(Application.dataPath.Length);
        }
        Debug.Log("Maze saved in location : " + savepath);
        GameObject prefab = PrefabUtility.CreatePrefab(savepath, NewMaze);
        PrefabUtility.ReplacePrefab(NewMaze, prefab, ReplacePrefabOptions.ConnectToPrefab);

        NewMaze.transform.parent = ObstacleParent.transform;

        //ResetEverything();
    }

    Vector3 CastRayToWorld()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        return ray.origin + (ray.direction * 20);
     
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
        Debug.Log("Resetting tool ");
        TemporaryClickedPositions.Clear();

        Debug.Log("All generated obstacles cleared");
        for (int i = 0; i < ObstacleParent.transform.childCount; i++)
        {
            Destroy(ObstacleParent.transform.GetChild(i).gameObject);
        }
        MazeComponents.Clear();
    }

    void ClearTemporaryPoints()
    {
        Debug.Log("Temporary locations cleared ");
        TemporaryClickedPositions.Clear();
    }
}
