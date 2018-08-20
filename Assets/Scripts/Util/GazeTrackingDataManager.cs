using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Author : Antonios Nestoridis (nestoridis.antonai@gmail.com)
 * 
 * GazeTrackingDataManager description:
 * Responsible for saving the eye tracking data during an experiment
 * Closely connected with QuestionManager and VRController
 */

public class GazeTrackingDataManager : MonoBehaviour
{

    public struct GazeVisitInformation
    {
        public int VisitID;
        public float VisitDuration;

        public GazeVisitInformation(int ID, float duration)
        {
            VisitID = ID;
            VisitDuration = duration;
        }
    }

    public class GazeSegment
    {
        public List<GazeVisitInformation> SegmentVisits;
        public int TotalNumberOfVisits;
        public float AvgVisitDuration;
        public float VisitPercentage;

        public GazeSegment()
        {
            SegmentVisits = new List<GazeVisitInformation>();
            TotalNumberOfVisits = 0;
            AvgVisitDuration = 0;
        }
    }

    public GazeSegment[,] PanelSegments;
    public int NoPanelGazeSegments;
    public float RecordDataTimeInterval;
    public float GazeTimeThreshold;
    public bool RecordingData;
    public bool EndRecording;


    private QuestionManager QueryManager;
    private long TotalGazeIterations;
    private float Timer;
    private float SegmentGazeDurationTimer;
    private string DataLogFilePath;
    private Vector2 NewGazeSegment;
    private Vector2 PreviousGazeSegment;

    // Use this for initialization
    void Start ()
    {
        DataLogFilePath = Application.streamingAssetsPath + "/TestLogData/GazeData.json";
        EndRecording = false;
	    TotalGazeIterations = 0;
	    QueryManager = gameObject.GetComponent<QuestionManager>();
	    Timer = 0;
	    RecordingData = true;
	    NewGazeSegment = PreviousGazeSegment =new Vector2(-1,-1);
        //initialize gaze segments
        PanelSegments = new GazeSegment[NoPanelGazeSegments,NoPanelGazeSegments];

	    for (int i = 0; i < NoPanelGazeSegments; i++)
	    {
	        for (int j = 0; j < NoPanelGazeSegments; j++)
	        {
                PanelSegments [i,j] = new GazeSegment();
	        }
        }
	}
	
	// Update is called once per frame
	void Update () {

        //Pressing button to endrecording, do final calculations and write in json
	    if (EndRecording)
	    {
	        FinalizeStats();
	        EndRecording = false;
	    }

	    if (QueryManager.IsActivated() && RecordingData)
	    {
	        Timer += Time.deltaTime;
            //this could run in a coroutine as well
	        if (Timer > RecordDataTimeInterval)
	        {
	            Vector2 GazeCoords = VRController.Instance.GetPanelGazeCoordinates();
	         
                //If the user is staring somewhere at the panel
                if (GazeCoords != new Vector2(-2, -2))
	            {
                    //get gaze segment according to coordinates
	                NewGazeSegment = GetGazeSegmentID(GazeCoords);
                    //  Debug.Log("GazeCoords : " + GazeCoords + " ->" +"Segments " +NewGazeSegment);
                    //If the new Gazesegment is different than the previous one
                    if (NewGazeSegment != PreviousGazeSegment)
                    {
                        //Record stare duration data for the previous segment and reset the timer
                        //Recording will only happen if the tester has stared at a section for more than 
                        // the threshold of 1 second
                        if (SegmentGazeDurationTimer > GazeTimeThreshold) { 
                            
                            RecordData(PreviousGazeSegment, SegmentGazeDurationTimer);
                            TotalGazeIterations++;
                        }
                        SegmentGazeDurationTimer = 0.0f;
                        PreviousGazeSegment = NewGazeSegment;
                    }
                    else
                    {
                        //for now simply add the interval to the timer
                        SegmentGazeDurationTimer += RecordDataTimeInterval;
                    }

                }

	            //reset timer
	            Timer = 0.0f;

	        }

	    }


	}

    //Increases segment visit count
    //Adds new entry of format : VisitID -> VisitDuration to the list of the segment
    private void RecordData(Vector2 segment,float duration)
    {
        PanelSegments[(int)segment.x, (int)segment.y].SegmentVisits.Add(
            new GazeVisitInformation(++PanelSegments[(int)segment.x, (int)segment.y].TotalNumberOfVisits,duration));

        PanelSegments[(int) segment.x, (int) segment.y].AvgVisitDuration += duration;
    }

    private void FinalizeStats()
    {

        //Loop through all segments and just divide the duration with the total visits. Also calculate percentages after adding
        //total number of visits
        for (int i = 0; i < NoPanelGazeSegments; i++)
        {
            for (int j = 0; j < NoPanelGazeSegments; j++)
            {
                PanelSegments[i, j].AvgVisitDuration = PanelSegments[i, j].AvgVisitDuration / PanelSegments[i, j].TotalNumberOfVisits;
                PanelSegments[i, j].VisitPercentage = (float) PanelSegments[i, j].TotalNumberOfVisits / TotalGazeIterations;
            }
        }
       
    }

    private Vector2 GetGazeSegmentID(Vector2 GazeCoords)
    {
        float x = GazeCoords.x;
        float y = GazeCoords.y;
      
        float cellDim = 2.0f / NoPanelGazeSegments;
        
        int row = -1;
        int column = -1;
        //Naive double loop per segment or binary search. Depends on number of segments
        for (int i = 0; i < NoPanelGazeSegments; i++)
        {
            if (-1 + i * cellDim <= x && x < -1 + (i + 1) * cellDim)
            {
                column = i;
            }
        }

        for (int j = 0; j < NoPanelGazeSegments; j++)
        {
            if (-1 + j * cellDim <= y && y < -1 + (j + 1) * cellDim)
            {
                row = NoPanelGazeSegments -1 -j;
            }
        }

        return new Vector2(row,column);
    }


    private void WriteDataToFile()
    {

    }
}
