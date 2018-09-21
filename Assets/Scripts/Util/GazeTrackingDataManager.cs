using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ProBuilder2.Common;
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
        public string VisitTimeStamp;
        public float VisitDuration;

        public GazeVisitInformation(int ID,string Timestamp , float duration)
        {
            VisitID = ID;
            VisitTimeStamp = Timestamp;
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

    //The following 3 structures are used to store for the JSON serialization. All are similar to the structures above which are 
    //used internally by this manager. Is it possible to use them instantly instead of copying the datA?
    [Serializable]
    public struct JsonVisitData
    {
        public string Time;
        public float VisitDuration;
    }

    [Serializable]
    public struct JsonGazeSegmentData
    {
        //0,0 is top left and then they go rowise
        public int x;
        public int y;
        public int TotalSegmentVisits;
        public float AverageVisitDuration;
        public string TotalVisitPercentage;
        public JsonVisitData[] GazeVisits;

    }
    [Serializable]
    public class JsonGazeData
    {
        public int UserID;
        public int Maze;
        public string StartDate;
        public string EndDate;
        public JsonGazeSegmentData[] SegmentID; 

    }

    //singleton
    public static GazeTrackingDataManager Instance { get; private set; }


    public GazeSegment[,] PanelSegments;
    public int NoPanelGazeSegments;
    public float RecordDataTimeInterval;
    public float GazeTimeThreshold;
    public bool RecordingData;
    public bool EndRecording;

    [Header("JSON formatting ")]
    [Tooltip("Makes the JSON file valid by removing special characters used for appending information.")]
    [SerializeField] private bool FinalizeJsonFile = false;
    [Tooltip("Resets the JSON file to an initial preset format.")]
    [SerializeField] private bool ClearJsonFile = false;

    private QuestionManager QueryManager;
    private long TotalGazeIterations;
    private float Timer;
    private float SegmentGazeDurationTimer;
    private string DataLogFilePath;
    private Vector2 NewGazeSegment;
    private Vector2 PreviousGazeSegment;
    private DateTime StartRecordingDate;
    private DateTime EndRecordingDate;

    void Awake()
    {
        Instance = this;

        //make this path public maybe and add another file for the other  grid
        DataLogFilePath = Application.streamingAssetsPath + "/TestLogData/GazeData.json";
        EndRecording = false;
        TotalGazeIterations = 0;
       
        Timer = 0;
        RecordingData = false;
        NewGazeSegment = PreviousGazeSegment = new Vector2(-1, -1);
        //initialize gaze segments
        PanelSegments = new GazeSegment[NoPanelGazeSegments, NoPanelGazeSegments];

        for (int i = 0; i < NoPanelGazeSegments; i++)
        {
            for (int j = 0; j < NoPanelGazeSegments; j++)
            {
                PanelSegments[i, j] = new GazeSegment();
            }
        }
    }

    // Use this for initialization
    void Start ()
    {
        QueryManager = gameObject.GetComponent<QuestionManager>();
    }
	
	// Update is called once per frame
	void Update () {

        //Pressing button to endrecording, do final calculations and write in json
	    if (EndRecording)
	    {
	        FinalizeStats();
	        EndRecording = false;
	    }

	    //General options 
	    if (FinalizeJsonFile)
	    {
	        DeleteJsonEndings();
	        FinalizeJsonFile = false;
	    }

	    if (ClearJsonFile)
	    {
	        ResetJSONFile();
	        ClearJsonFile = false;
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

                    //Debug.Log(SegmentGazeDurationTimer);

                }

	            //reset timer
	            Timer = 0.0f;

	        }

	    }


	}


    public void StartRecordingData()
    {
        Debug.Log("Gaze manager enabled ");
        RecordingData = true;
        StartRecordingDate = DateTime.Now;
    }

    //Increases segment visit count
    //Adds new entry of format : VisitID -> VisitDuration to the list of the segment
    private void RecordData(Vector2 segment,float duration)
    {
        //Debug.Log("Data recorded: Segment [" + segment + " ] - Duration of stare :" + duration );
        //Add to the appropriate segment a new entry on the visit list, as well as increase the time spent on the segment.
        PanelSegments[(int)segment.x, (int)segment.y].SegmentVisits.Add(
            new GazeVisitInformation(++PanelSegments[(int)segment.x, (int)segment.y].TotalNumberOfVisits,CurrentTimeString(),duration)
            );

        PanelSegments[(int) segment.x, (int) segment.y].AvgVisitDuration += duration;

    }

    public string CurrentTimeString()
    {

        string CurrentTime = DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();

        return CurrentTime;
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
                PanelSegments[i, j].VisitPercentage = ( (float) PanelSegments[i, j].TotalNumberOfVisits / TotalGazeIterations) * 100;
            }
        }

        //Stop recording data logic
        Debug.Log("Gaze manager enabled ");
        RecordingData = false;
        EndRecordingDate = DateTime.Now;
        WriteDataToFile();

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

    //JSON File stuff , copied from query manager. Maybe make them functions in a jsonmanager utility class?
    private void WriteDataToFile()
    {

        Debug.Log("Saving gaze data to file");
        //Per trial info
        JsonGazeData trialData = new JsonGazeData();
        trialData.UserID = QueryManager.GetUserID();
        trialData.Maze = QueryManager.GetMazeID();
        trialData.StartDate = StartRecordingDate.ToString();
        trialData.EndDate = EndRecordingDate.ToString();
        trialData.SegmentID= new JsonGazeSegmentData[NoPanelGazeSegments* NoPanelGazeSegments]; 

        //Per segment info
        for (int i = 0; i < NoPanelGazeSegments; i++)
        {
            for (int j = 0; j < NoPanelGazeSegments; j++)
            {

                GazeSegment CurrentSegment = PanelSegments[i, j];

                JsonGazeSegmentData SegmentInfo = new JsonGazeSegmentData();
                SegmentInfo.x = i;
                SegmentInfo.y = j;
                SegmentInfo.TotalSegmentVisits = PanelSegments[i, j].TotalNumberOfVisits;
                SegmentInfo.AverageVisitDuration = PanelSegments[i, j].AvgVisitDuration;
                SegmentInfo.TotalVisitPercentage = (PanelSegments[i, j].VisitPercentage).ToString("#.00") + "%";
                SegmentInfo.GazeVisits = new JsonVisitData[PanelSegments[i, j].SegmentVisits.Count];

                //per segment visit info
                for (int k = 0; k < PanelSegments[i, j].SegmentVisits.Count; k++)
                {
                    JsonVisitData visitdata = new JsonVisitData();
                    visitdata.Time = PanelSegments[i, j].SegmentVisits[k].VisitTimeStamp;
                    visitdata.VisitDuration = PanelSegments[i, j].SegmentVisits[k].VisitDuration;
                    SegmentInfo.GazeVisits[k]= visitdata;
                    
                }
                trialData.SegmentID[i*(NoPanelGazeSegments)+ j] = SegmentInfo;
            }
        }

        //write to file 
        string json = JsonUtility.ToJson(trialData, true);

        //Appending in the write position in the file

        //Read the existing contents of the file
        string ExistingFile = File.ReadAllText(DataLogFilePath);

        //Find the position of the $ which shows the position where we will append the new object
        int AppendPosition = ExistingFile.IndexOf("$");
        ExistingFile = ExistingFile.Insert(AppendPosition, json + ", \r\n");

        //Write the new string in the file
        File.WriteAllText(DataLogFilePath, ExistingFile);



    }

    private void ResetJSONFile()
    {

        Debug.Log("JSON gaze data file has been reset");
        string InitText = "{ \r\n \"GazeData\":[  \r\n $ \r\n ] \r\n }";

        File.WriteAllText(DataLogFilePath, InitText);
    }

    private void DeleteJsonEndings()
    {
        Debug.Log("Finalising JSON format - Deleting special characters");
        //read the existing contents of the file
        string text = File.ReadAllText(DataLogFilePath);

        int dollarIndex = text.IndexOf("$");
        int lastCommaIndex = text.LastIndexOf(",");

        text = text.Remove(dollarIndex, 1);
        text = text.Remove(lastCommaIndex, 1);

        File.WriteAllText(DataLogFilePath, text);
    }


}
