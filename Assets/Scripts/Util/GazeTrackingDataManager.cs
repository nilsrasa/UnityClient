using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
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

    [Serializable]
    public class CustomSegment 
    {
        //segment bounding box
        public float Left;
        public float Right;
        public float Bottom;
        public float Top;

        //Custom tag for this segment - easier to classify later 
        public string CustomTag;

        //Typical gaze segment info. Can I inherit these instead of repeating them?
        public GazeSegment SegmentInfo;

        public CustomSegment()
        {
            SegmentInfo = new GazeSegment();
        }
    }

    //The following 4 structures are used to store information in the desirable format for the JSON serialization. All are similar to the structures above which are 
    //used internally by this manager. They are kept separate in case we want to alter the way they are saved in the json file.
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
    public struct JsonCustomSegmentData
    {
        //0,0 is top left and then they go rowise
        public string tag;
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
        public JsonCustomSegmentData[] CustomSegmentID;

    }

    //singleton
    public static GazeTrackingDataManager Instance { get; private set; }

    //Overall manager options
    [Tooltip("Enables/disables the manager and the recording of data")]
    [SerializeField] public bool IsActive;
    [Tooltip("If active, records data for the standard grid format")]
    [SerializeField] private bool StandardGridActive;
    [Tooltip("If active, records data for the custom grid format")]
    [SerializeField] private bool CustomGridActive;
    [Space(10)]
    //Standard grid options
    [Header("Standard M x M grid options")]
    [Space(4)]
    [Tooltip("Grid dimensions (width and height). This number generates a NumSegments X NumSegments grid")]
    [SerializeField] private int NumSegments;
    [Tooltip("Time threshold for recording a segment visit. If the user stares at a segment for less time, the data will be discarded")]
    [SerializeField] private float StGazeTimeThreshold;
    [Tooltip("Record time interval")]
    [SerializeField] private float StRecordTimeInterval;
    [Space(10)]
    //Custom grid options
    [Header("Custom grid options.")]
    [Space(4)]
    [Tooltip("Allows the creation of modular segments whose dimensions are defined explicitly by the user.")]
    public CustomSegment[] CustomSegments;
    [Tooltip("Time threshold for recording a segment visit. If the user stares at a segment for less time, the data will be discarded")]
    [SerializeField] private float CustGazeTimeThreshold;
    [Tooltip("Record time interval")]
    [SerializeField] private float CustRecordTimeInterval;
    [Space(10)]

    //Recording options
    //private bool StRecordingData;
    public bool EndRecording;

    //Save file paths
    //Standard grid
    [Header("Json file options")]
    [Space(4)]
    [SerializeField] private string StandardDataFilepath;
    [Tooltip("Makes the JSON file valid by removing special characters used for appending information.")]
    [SerializeField] private bool FinalizeStandardSegFile = false;
    [Tooltip("Resets the JSON file to an initial preset format.")]
    [SerializeField] private bool ClearStandardSegFile = false;
    [Space(5)]
    //Custom grid
    [SerializeField] private string CustomDataFilepath;
    [Tooltip("Makes the JSON file valid by removing special characters used for appending information.")]
    [SerializeField] private bool FinalizeCustomSegFile = false;
    [Tooltip("Resets the JSON file to an initial preset format.")]
    [SerializeField] private bool ClearCustomSegFile = false;
   
  
    private QuestionManager QueryManager;
    private float StTimer;
    private float CustTimer;

    //Standard grid info
    private GazeSegment[,] PanelSegments;
    private Vector2 NewGazeSegment;
    private Vector2 PreviousGazeSegment;
    private DateTime StartRecordingDate;
    private DateTime EndRecordingDate;
    private float SegmentGazeDurationTimer;
    private long TotalGazeIterations;
    //Custom grid info 

    private int CNewGazeSegment;
    private int CPreviousGazeSegment;
    private DateTime CStartRecordingDate;
    private DateTime CEndRecordingDate;
    private float CSegmentGazeDurationTimer;
    private long CTotalGazeIterations;


    void Awake()
    {
        Instance = this;

        //make this path public maybe and add another file for the other  grid
        StandardDataFilepath = Application.streamingAssetsPath + "/" + StandardDataFilepath;
        CustomDataFilepath = Application.streamingAssetsPath + "/" + CustomDataFilepath;
        EndRecording = false;
        IsActive = false;
        TotalGazeIterations = 0;
        StTimer = CustTimer = 0;
       
        NewGazeSegment = PreviousGazeSegment = new Vector2(-1, -1);
        CNewGazeSegment = CPreviousGazeSegment = -1;

        //initialize segments
        PanelSegments = new GazeSegment[NumSegments, NumSegments];

        for (int i = 0; i < NumSegments; i++)
        {
            for (int j = 0; j < NumSegments; j++)
            {
                PanelSegments[i, j] = new GazeSegment();
            }
        }

        for (int i = 0; i < CustomSegments.Length; i++)
        {
            CustomSegments[i].SegmentInfo = new GazeSegment();
        }
    }

    // Use this for initialization
    void Start ()
    {
        QueryManager = gameObject.GetComponent<QuestionManager>();
    }
	
	// Update is called once per frame
	void Update () {

        //--------- The following act as buttons that will call a function when activated. This does not add unnecessary UI in our scene but keeps everything 
        //in the inspector
        
        
	    if (EndRecording)
	    {
	        FinalizeStats();
	        EndRecording = false;
	    }

	    
	    if (FinalizeStandardSegFile)
	    {
	        DeleteJsonEndings(StandardDataFilepath);
	        FinalizeStandardSegFile = false;
	    }

	    if (ClearStandardSegFile)
	    {
	        ResetJSONFile(StandardDataFilepath,"GazeData");
	        ClearStandardSegFile = false;
	    }

	    if (FinalizeCustomSegFile)
	    {
	        DeleteJsonEndings(CustomDataFilepath);
	        FinalizeCustomSegFile = false;
	    }

	    if (ClearCustomSegFile)
	    {
	        ResetJSONFile(CustomDataFilepath, "CustomGridGazeData");
	        ClearCustomSegFile = false;
	    }

        //Main loop
        //TODO REMOVE QUERYMANAGER DEPENDANCE HERE. Instead check if the panel is active?
        if (QueryManager.IsActivated() && IsActive)
	    {
	       
            //if this causes erros because it is sampled every frame instead of within the time interval put it back in each if statement
	        Vector2 GazeCoords = VRController.Instance.GetPanelGazeCoordinates();

            //Standard grid
            if (StandardGridActive)
	        {
	            StTimer += Time.deltaTime;

                //If the user is staring somewhere in the panel
                if (StTimer > StRecordTimeInterval && GazeCoords != new Vector2(-2, -2))
	            {
                    //get gaze segment according to coordinates
	                NewGazeSegment = GetGazeSegmentID(GazeCoords);
                    
                    //If the new Gazesegment has changed
                    if (NewGazeSegment != PreviousGazeSegment)
                    {
                        //Record stare duration data for the previous segment and reset the timer
                        //Recording will only happen if the tester has stared at a section for more than 
                        // the threshold of 1 second
                        if (SegmentGazeDurationTimer > StGazeTimeThreshold) { 
                            
                            RecordData(PreviousGazeSegment, SegmentGazeDurationTimer);
                            TotalGazeIterations++;
                        }
                        SegmentGazeDurationTimer = 0.0f;
                        PreviousGazeSegment = NewGazeSegment;
                    }
                    else
                    {
                        //for now simply add the interval to the timer
                        SegmentGazeDurationTimer += StRecordTimeInterval;
                    }
                }
                //reset timer
                StTimer = 0.0f;

	        }

            //Customgrid
	        if (CustomGridActive)
	        {
	            CustTimer += Time.deltaTime;
                //If the user is staring somewhere in the panel
                if (CustTimer > CustRecordTimeInterval && GazeCoords != new Vector2(-2, -2))
	            {
	                //----------------CustomGrid -------------------------
	                CNewGazeSegment = GetCustomGazeSegmentID(GazeCoords);
	                //If the new Gazesegment is different than the previous one
	                if (CNewGazeSegment != CPreviousGazeSegment)
	                {
	                    //Record stare duration data for the previous segment and reset the timer
	                    //Recording will only happen if the tester has stared at a section for more than 
	                    // the threshold of 1 second
	                    if (CSegmentGazeDurationTimer > CustGazeTimeThreshold)
	                    {
	                        RecordCustomGazeData(CPreviousGazeSegment, CSegmentGazeDurationTimer);
	                        CTotalGazeIterations++;
	                    }
	                    CSegmentGazeDurationTimer = 0.0f;
	                    CPreviousGazeSegment = CNewGazeSegment;
	                }
	                else
	                {
	                    //for now simply add the interval to the timer
	                    CSegmentGazeDurationTimer += CustRecordTimeInterval;
	                }
                }

               
            }

	    }


	}

    public void EnableManager()
    {
        Debug.Log("Gaze manager enabled");
        IsActive = true;
        StartRecordingDate = DateTime.Now;
    }

    //enable = true -> resumes recording
    //enable = false -> pauses recording
    public void RecordingData(bool enable)
    {
        //this simply (un)pauses the manager. Does not finalize anything or reset anything
        IsActive = enable;
        Debug.Log(enable ? "Gaze manager unpaused " : "Gaze manager paused ");
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

    private void RecordCustomGazeData(int segment, float duration)
    {
       
        CustomSegments[segment].SegmentInfo.SegmentVisits.Add(new GazeVisitInformation(++CustomSegments[segment].SegmentInfo.TotalNumberOfVisits, CurrentTimeString(), duration));
        CustomSegments[segment].SegmentInfo.AvgVisitDuration += duration;

    }

    public string CurrentTimeString()
    {

        string CurrentTime = DateTime.Now.Hour.ToString() + ":" + DateTime.Now.Minute.ToString() + ":" + DateTime.Now.Second.ToString();

        return CurrentTime;
    }

    private void FinalizeStats()
    {
        EndRecordingDate = DateTime.Now;
        if (StandardGridActive) { 
         //Loop through all segments and just divide the duration with the total visits. Also calculate percentages after adding
           //total number of visits
            for (int i = 0; i < NumSegments; i++)
            {
                for (int j = 0; j < NumSegments; j++)
                {
                    if (PanelSegments[i, j].TotalNumberOfVisits != 0)
                    {
                        PanelSegments[i, j].AvgVisitDuration =
                            PanelSegments[i, j].AvgVisitDuration / PanelSegments[i, j].TotalNumberOfVisits;
                        PanelSegments[i, j].VisitPercentage =
                            ((float) PanelSegments[i, j].TotalNumberOfVisits / TotalGazeIterations) * 100;
                    }
                    else
                    {
                        PanelSegments[i, j].AvgVisitDuration = 0;

                        PanelSegments[i, j].VisitPercentage = 0;
                    }

                }
            }
            WriteDataToFile(StandardDataFilepath, 0);

        }

        if (CustomGridActive)
        {
            for (int i = 0; i < CustomSegments.Length; i++)
            {
                if (CustomSegments[i].SegmentInfo.TotalNumberOfVisits != 0)
                {
                    CustomSegments[i].SegmentInfo.AvgVisitDuration =
                        CustomSegments[i].SegmentInfo.AvgVisitDuration /
                        CustomSegments[i].SegmentInfo.TotalNumberOfVisits;
                    CustomSegments[i].SegmentInfo.VisitPercentage =
                        ((float) CustomSegments[i].SegmentInfo.TotalNumberOfVisits / CTotalGazeIterations) * 100;
                }
                else
                {
                    CustomSegments[i].SegmentInfo.AvgVisitDuration = 0;
                    CustomSegments[i].SegmentInfo.VisitPercentage = 0;
                }
              
            }
            WriteDataToFile(CustomDataFilepath, 1);
        }
        

        //Stop recording data logic
        Debug.Log("Gaze manager disabled ");
        IsActive = false;   

    }

    private Vector2 GetGazeSegmentID(Vector2 GazeCoords)
    {
        float x = GazeCoords.x;
        float y = GazeCoords.y;
      
        float cellDim = 2.0f / NumSegments;
        
        int row = -1;
        int column = -1;
        //Naive double loop per segment or binary search. Depends on number of segments
        for (int i = 0; i < NumSegments; i++)
        {
            if (-1 + i * cellDim <= x && x < -1 + (i + 1) * cellDim)
            {
                column = i;
            }
        }

        for (int j = 0; j < NumSegments; j++)
        {
            if (-1 + j * cellDim <= y && y < -1 + (j + 1) * cellDim)
            {
                row = NumSegments -1 -j;
            }
        }

        return new Vector2(row,column);
    }

    private int GetCustomGazeSegmentID(Vector2 GazeCoords)
    {
        for (int i = 0; i < CustomSegments.Length; i++)
        {
            //construct bounding box and check if coords are inside it

            if (GazeCoords.x <= CustomSegments[i].Right && GazeCoords.x > CustomSegments[i].Left
                && GazeCoords.y <= CustomSegments[i].Top && GazeCoords.y > CustomSegments[i].Bottom)
            {
                return i;
                
            }
        }
        return -1;
    }

    //JSON File stuff , copied from query manager. Maybe make them functions in a jsonmanager utility class?
    private void WriteDataToFile(string Filepath , int GridID)
    {

        if (GridID == 0) {
            Debug.Log("Saving standard grid data to file");
            //Per trial info
            JsonGazeData trialData = new JsonGazeData();
            trialData.UserID = QueryManager.GetUserID();
            trialData.Maze = QueryManager.GetMazeID();
            trialData.StartDate = StartRecordingDate.ToString();
            trialData.EndDate = EndRecordingDate.ToString();
            trialData.SegmentID= new JsonGazeSegmentData[NumSegments* NumSegments]; 

            //Per segment info
            for (int i = 0; i < NumSegments; i++)
            {
                for (int j = 0; j < NumSegments; j++)
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
                    trialData.SegmentID[i*(NumSegments)+ j] = SegmentInfo;
                }
            }

            //write to file 
            string json = JsonUtility.ToJson(trialData, true);

            //Appending in the write position in the file

            //Read the existing contents of the file
            string ExistingFile = File.ReadAllText(Filepath);

            //Find the position of the $ which shows the position where we will append the new object
            int AppendPosition = ExistingFile.IndexOf("$");
            ExistingFile = ExistingFile.Insert(AppendPosition, json + ", \r\n");

            //Write the new string in the file
            File.WriteAllText(Filepath, ExistingFile);

        }
        else if (GridID == 1)
        {
            Debug.Log("Saving custom grid data to file");

            // I strongly dislike this
            //Per trial info
            JsonGazeData customTrialData = new JsonGazeData();
            customTrialData.UserID = QueryManager.GetUserID();
            customTrialData.Maze = QueryManager.GetMazeID();
            customTrialData.StartDate = StartRecordingDate.ToString();
            customTrialData.EndDate = EndRecordingDate.ToString();
            customTrialData.CustomSegmentID = new JsonCustomSegmentData[CustomSegments.Length];

            //Per segment info
            for (int i = 0; i < CustomSegments.Length; i++)
            {


                //CustomSegment CurrentSegment = CustomSegments[i];

                JsonCustomSegmentData SegmentInfo = new JsonCustomSegmentData();
                SegmentInfo.tag = CustomSegments[i].CustomTag;
                SegmentInfo.TotalSegmentVisits = CustomSegments[i].SegmentInfo.TotalNumberOfVisits;
                SegmentInfo.AverageVisitDuration = CustomSegments[i].SegmentInfo.AvgVisitDuration;
                SegmentInfo.TotalVisitPercentage = (CustomSegments[i].SegmentInfo.VisitPercentage).ToString("#.00") + "%";
                SegmentInfo.GazeVisits = new JsonVisitData[CustomSegments[i].SegmentInfo.SegmentVisits.Count];

                //per segment visit info
                for (int k = 0; k < CustomSegments[i].SegmentInfo.SegmentVisits.Count; k++)
                {
                    JsonVisitData visitdata = new JsonVisitData();
                    visitdata.Time = CustomSegments[i].SegmentInfo.SegmentVisits[k].VisitTimeStamp;
                    visitdata.VisitDuration = CustomSegments[i].SegmentInfo.SegmentVisits[k].VisitDuration;
                    SegmentInfo.GazeVisits[k] = visitdata;

                }
                customTrialData.CustomSegmentID[i] = SegmentInfo;

            }

            //write to file 
            string json = JsonUtility.ToJson(customTrialData, true);

            //Appending in the write position in the file

            //Read the existing contents of the file
            string ExistingFile = File.ReadAllText(Filepath);

            //Find the position of the $ which shows the position where we will append the new object
            int AppendPosition = ExistingFile.IndexOf("$");
            ExistingFile = ExistingFile.Insert(AppendPosition, json + ", \r\n");

            //Write the new string in the file
            File.WriteAllText(Filepath, ExistingFile);

        }
    }

  

    private void ResetJSONFile(string Filepath, string JsonObjName)
    {

        Debug.Log("JSON gaze data file has been reset : " + JsonObjName);
        string InitText = "{ \r\n \"" + JsonObjName + "\":[  \r\n $ \r\n ] \r\n }";

        File.WriteAllText(Filepath, InitText);
    }

    private void DeleteJsonEndings(string Filepath)
    {
        Debug.Log("Finalising JSON format - Deleting special characters");
        //read the existing contents of the file
        string text = File.ReadAllText(Filepath);

        int dollarIndex = text.IndexOf("$");
        int lastCommaIndex = text.LastIndexOf(",");

        text = text.Remove(dollarIndex, 1);
        text = text.Remove(lastCommaIndex, 1);

        File.WriteAllText(Filepath, text);
    }


}
