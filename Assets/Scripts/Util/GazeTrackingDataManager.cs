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

    //Standard grid
    public GazeSegment[,] PanelSegments;
    public int NoPanelGazeSegments;
    public float RecordDataTimeInterval;
    public float GazeTimeThreshold;
    public bool RecordingData;
    public bool EndRecording;


    //Save file paths
    [Header("Save path files")]
    [SerializeField] private string StandardGridDataFilepath;
    [SerializeField] private string CustomGridDataFilepath;

    //custom grid 
    public CustomSegment[] CustomSegments;

    [Header("JSON formatting ")]
    [Tooltip("Makes the JSON file valid by removing special characters used for appending information.")]
    [SerializeField] private bool FinalizeCustomSegFile = false;
    [Tooltip("Resets the JSON file to an initial preset format.")]
    [SerializeField] private bool ClearCustomSegFile = false;
    [Tooltip("Makes the JSON file valid by removing special characters used for appending information.")]
    [SerializeField] private bool FinalizeStandardSegFile = false;
    [Tooltip("Resets the JSON file to an initial preset format.")]
    [SerializeField] private bool ClearStandardSegFile = false;

    private QuestionManager QueryManager;
   
    private float Timer;
    
 


    //Standard grid info
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
        StandardGridDataFilepath = Application.streamingAssetsPath + "/" + StandardGridDataFilepath;
        CustomGridDataFilepath = Application.streamingAssetsPath + "/" + CustomGridDataFilepath;
        EndRecording = false;
        TotalGazeIterations = 0;
       
        Timer = 0;
        RecordingData = false;
        NewGazeSegment = PreviousGazeSegment = new Vector2(-1, -1);
        CNewGazeSegment = CPreviousGazeSegment = -1;

        //initialize gaze segments
        PanelSegments = new GazeSegment[NoPanelGazeSegments, NoPanelGazeSegments];

        for (int i = 0; i < NoPanelGazeSegments; i++)
        {
            for (int j = 0; j < NoPanelGazeSegments; j++)
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

        //Pressing button to endrecording, do final calculations and write in json
	    if (EndRecording)
	    {
	        FinalizeStats();
	        EndRecording = false;
	    }

	    //General options 
	    if (FinalizeStandardSegFile)
	    {
	        DeleteJsonEndings(StandardGridDataFilepath);
	        FinalizeStandardSegFile = false;
	    }

	    if (ClearStandardSegFile)
	    {
	        ResetJSONFile(StandardGridDataFilepath,"GazeData");
	        ClearStandardSegFile = false;
	    }

	    if (FinalizeCustomSegFile)
	    {
	        DeleteJsonEndings(CustomGridDataFilepath);
	        FinalizeCustomSegFile = false;
	    }

	    if (ClearCustomSegFile)
	    {
	        ResetJSONFile(CustomGridDataFilepath, "CustomGridGazeData");
	        ClearCustomSegFile = false;
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


                    //----------------StandardGrid -------------------------
                    //get gaze segment according to coordinates
	                NewGazeSegment = GetGazeSegmentID(GazeCoords);
                    

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

	                //----------------CustomGrid -------------------------
	                CNewGazeSegment = GetCustomGazeSegmentID(GazeCoords);
	                //If the new Gazesegment is different than the previous one
	                if (CNewGazeSegment != CPreviousGazeSegment)
	                {
	                    //Record stare duration data for the previous segment and reset the timer
	                    //Recording will only happen if the tester has stared at a section for more than 
	                    // the threshold of 1 second
	                    if (CSegmentGazeDurationTimer > GazeTimeThreshold)
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
	                    CSegmentGazeDurationTimer += RecordDataTimeInterval;
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

        for (int i = 0; i < CustomSegments.Length; i++)
        {
            CustomSegments[i].SegmentInfo.AvgVisitDuration = CustomSegments[i].SegmentInfo.AvgVisitDuration / CustomSegments[i].SegmentInfo.TotalNumberOfVisits;
            CustomSegments[i].SegmentInfo.VisitPercentage = ((float)CustomSegments[i].SegmentInfo.TotalNumberOfVisits / CTotalGazeIterations) *100;
        }

        //Stop recording data logic
        Debug.Log("Gaze manager disabled ");
        RecordingData = false;
        EndRecordingDate = DateTime.Now;

        //Write data in files
        WriteDataToFile(StandardGridDataFilepath,0);
        WriteDataToFile(CustomGridDataFilepath,1);

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

        Debug.Log("Saving gaze data to file");

        if (GridID == 0) { 
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
            string ExistingFile = File.ReadAllText(Filepath);

            //Find the position of the $ which shows the position where we will append the new object
            int AppendPosition = ExistingFile.IndexOf("$");
            ExistingFile = ExistingFile.Insert(AppendPosition, json + ", \r\n");

            //Write the new string in the file
            File.WriteAllText(Filepath, ExistingFile);

        }
        else if (GridID == 1)
        {
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

        Debug.Log("JSON gaze data file has been reset");
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
