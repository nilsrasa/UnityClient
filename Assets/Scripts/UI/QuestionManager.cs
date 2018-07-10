using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using ProBuilder2.Common;
using UnityEngine.Serialization;

public class QuestionManager : MonoBehaviour {


    
    [System.Serializable]
    public class ListWrapper
    {
        public KeyCode Hotkey;
        public bool ActiveForTest;
        public List<string> QueryList;
    }


    //struct to save the pair of response times for each query
    [Serializable]
    public class ResponseTimes
    {
        public float PRS;
        public float QRS;
       // public float[] RS;
        //constructor for fast init
        public ResponseTimes()
        {
            PRS = QRS = 0;
        }

    }

    [Serializable]
    public struct JsonRS
    {
        public string QuestionType;
        public ResponseTimes[] RSS;
    }


    [Serializable]
    public class JsonData
    {
        public string Date;
        public int UserID;
        public int Maze;
        public JsonRS[] Responses;
       
    }


    //Inspector variables
    public float PresetDelayTime;
    public int TestSubjectID;
    public int MazeID;
    public bool FormatJsonFile = false;
    public bool ClearJsonFile = false;
    public bool IsActive;
    public bool ResetTest;
    public KeyCode CloseMessageHotkey;
    public KeyCode FinalQuestionHotkey;//This key must correspond to one of the questions hotkeys
    public List<ListWrapper> QueriesList;
   
    //Private reference vars
    private GameObject PopUpMessagePanel;
    private GameObject QueryPanel;
    private Text QueryText;
    private AudioSource source;

    //private vars
    private List<List<ResponseTimes>> RSList;
    private int[] QueryListCounters; //keeps track of the current query in each of the lists
    private DateTime ExperimentDate;
    private string DataLogFilePath;
    private Dictionary<KeyCode, int> KeysToIndexMap;
    private Dictionary<KeyCode, int> CurrentActiveKeys;
    private Dictionary<KeyCode, int> InitActiveKeys;// this is pretty ugly, try to manage this in a better way

    //per algorithm loop variables
    // private int QueryCounter;
    private string CurrentQueryText;
    private int cqIndex;
    private int CurrentQListIndex;
    private KeyCode pressedKey;
    private int TypesToWrite;

    private bool DisplayingPopUp;
    private bool DisplayingQuery;
    private bool DelayingQuery;
    private float PopUpTimer;
    private float QueryTimer;
    private float DelayTimer;
    private float QueryDelay;
    

    // Use this for initialization
    void Start()
    {
        //Save the gameobjects  for fast reference in the loop
        PopUpMessagePanel = transform.GetChild(0).gameObject;
        QueryPanel = transform.GetChild(1).gameObject;
        source = gameObject.GetComponent<AudioSource>();

        //If children were retrieved succesfully 
        if (PopUpMessagePanel && QueryPanel)
        {
            //retrieve the question text and initialise the text with the first question
            QueryText = QueryPanel.transform.GetChild(3).gameObject.GetComponent<Text>();
           
            //Deactivate the panels
            PopUpMessagePanel.SetActive(false);
            QueryPanel.SetActive(false);
        }
        else
        {
            Debug.Log("QueryManager could not locate predefined panels");
        }

       
       ResetManager();

        //Filepath
        DataLogFilePath = Application.streamingAssetsPath + "/TestLogData/UserTestData.json";

    }
    // Update is called once per frame
    void Update()
    {
        //Maybe use a hotkey for this or simply do it manually
        if (FormatJsonFile)
        {

            DeleteJsonEndings();
            FormatJsonFile = false;
        }

        if (ClearJsonFile)
        {
            ResetJSONFile();
            ClearJsonFile = false;
        }

        if (ResetTest)
        {
            ResetManager();
            ResetTest = false;
        }


        //If the question manager is not active then do not check for keyboard input.
        //This ensures that keys will not call the QuestionManager's functionality while typing in other situations e.g during chat message
        if (!IsActive)
            return;

        //Nothing displayed to the user
        if (!DisplayingPopUp && !DisplayingQuery)
        {
            

            //Tester Input is one of many possible KeyCodes
            foreach (KeyCode key in CurrentActiveKeys.Keys)
            {
                if (Input.GetKeyDown(key))
                {
                    Debug.Log("Pressed " + key);

                    //Load appropriate question for the text and save 
                    pressedKey = key; 
                    CurrentQListIndex = KeysToIndexMap[key]; Debug.Log("Questiontype  "+CurrentQListIndex);//which set of questions  
                    cqIndex = QueryListCounters[CurrentQListIndex]; Debug.Log("Question index  " + cqIndex);// which question index of that previous set
                    CurrentQueryText = QueriesList[CurrentQListIndex].QueryList[cqIndex]; Debug.Log("Question : " + CurrentQueryText); // the actual text of the question

                    ShowPopUp();
                    DisplayingPopUp = true;
                }
            }

            

            //if (Input.GetKeyDown(KeyCode.M))
            //{
            //    Debug.Log("Pressed M");
            //    //  Debug.Log("Pressed M when nothing displayed");
            //    ShowPopUp();
            //    DisplayingPopUp = true;
            //}
        }
        //PopUp is displaying
        else if (DisplayingPopUp)
        {
            //while the popup is displayed, increment timer
            if (!DelayingQuery)
            {
                PopUpTimer += Time.deltaTime;
                if ((Input.GetKeyDown(KeyCode.Y) || Input.GetKeyDown(KeyCode.N)))
                {
                    ClosePopUp();

                    //Tester presses response key according to user's verbal answer
                    // Y -> Yes
                    // N -> No
                   
                    if (Input.GetKeyDown(KeyCode.Y))
                    {
                       
                        //Save PRS timer
                        RSList[CurrentQListIndex][cqIndex].PRS = PopUpTimer;
                        //Essentially instant query show
                        QueryDelay = 0.1f;
                    }
                    else if (Input.GetKeyDown(KeyCode.N))
                    {
                        //Save PRS timer as preset
                        RSList[CurrentQListIndex][cqIndex].PRS = PresetDelayTime;
                        //Delay showing query by full preset time
                        QueryDelay = PresetDelayTime;
                    }

                    DelayingQuery = true;
                   
                }
            }
            else
            {
                DelayTimer += Time.deltaTime;
                if (DelayTimer >= QueryDelay)
                {
                    //     Debug.Log("Delaying ended");

                    ShowQuery();
                    DelayingQuery = false;
                    DisplayingQuery = true;
                    DisplayingPopUp = false;
                }
            }
        }
        //Query is displaying
        else if (DisplayingQuery)
        {
            QueryTimer += Time.deltaTime;

            if (Input.GetKeyDown(CloseMessageHotkey))
            {
                //Save QRS
                RSList[CurrentQListIndex][cqIndex].QRS = QueryTimer;
                CloseQuery();
                DisplayingQuery = false;
                ResetTimers();

            }
        }

    }

    private void ShowPopUp()
    {
        //For now no animations or anything else
        PopUpMessagePanel.SetActive(true);
        source.Play();
      
    }

    private void ClosePopUp()
    {
        //For now no animations or anything else
        PopUpMessagePanel.SetActive(false);
      ;
    }

    private void ShowQuery()
    {
        QueryText.text = CurrentQueryText;
        //For now no animations or anything else
        QueryPanel.SetActive(true);

    }

    private void CloseQuery()
    {

        //If there are still questions in the list, then prepare the next question's text
        if (QueryListCounters[CurrentQListIndex] < RSList[CurrentQListIndex].Count-1)
        {
            QueryListCounters[CurrentQListIndex]++;

        }
        //If all questions from this set are answered then disable that hotkey for now.
        else
        {
            CurrentActiveKeys.Remove(pressedKey);

            //If we answered all the final questions then write to file and exit
            //This key must correspond to one of the questions hotkeys
            if (pressedKey == FinalQuestionHotkey)
            {
                WriteDataToFile();
               // ResetManager();
            }
        }


       
        
       
       
        //For now no animations or anything else
        QueryPanel.SetActive(false);
       
    }

    private void WriteDataToFile()
    {

       
        Debug.Log("Writing to file");

        //JSON object creation
        JsonData data = new JsonData();
        data.UserID = TestSubjectID;
        data.Date = ExperimentDate.ToString(); //Date and time as string
        data.Maze = MazeID;

        //this many different types of questions
        Debug.Log(InitActiveKeys.Keys.Count);
        data.Responses = new JsonRS[InitActiveKeys.Keys.Count];

        foreach (var key in InitActiveKeys)
        {
            Debug.Log(key);
        }

        int counter = 0;
        foreach (var key in InitActiveKeys.Keys)
        {
            JsonRS temp = new JsonRS();
            temp.QuestionType = key.ToString();
            int index = InitActiveKeys[key];
          //  Debug.Log("Index = "+ index);
          //  Debug.Log("No of Rss  = " + RSList[index].Count);
            temp.RSS = new ResponseTimes[RSList[index].Count];
            for (int j = 0; j < RSList[index].Count; j++)
            {
                temp.RSS[j] = new ResponseTimes();
                temp.RSS[j].PRS= RSList[index][j].PRS;
                temp.RSS[j].QRS = RSList[index][j].QRS;

            }
            data.Responses[counter]= temp;
            counter++;
        }

       
        string json = JsonUtility.ToJson(data,true);

      
        //Appending in the write position in the file
  
        //read the existing contents of the file
        string ExistingFile = File.ReadAllText(DataLogFilePath);

        //find the position of the $ which shows the position where we will append the new object
        int AppendPosition = ExistingFile.IndexOf("$");
        ExistingFile = ExistingFile.Insert(AppendPosition, json + ", \r\n");
       
       //write the new string in the file
        File.WriteAllText(DataLogFilePath,ExistingFile);

   
    }

    private void ResetTimers()
    {
        PopUpTimer = QueryTimer = DelayTimer =  0.0f;
    }

    private void ResetManager()
    {
       
        QueryDelay = 0;
        cqIndex = 0;
        CurrentQListIndex = 0;
        DisplayingPopUp = DisplayingQuery = DelayingQuery = false;
        ResetTimers();

        //Initialize data structures from scratch
        KeysToIndexMap = new Dictionary<KeyCode, int>();
        CurrentActiveKeys = new Dictionary<KeyCode, int>();
        InitActiveKeys= new Dictionary<KeyCode, int>();
        QueryListCounters = new int[QueriesList.Count];
        RSList = new List<List<ResponseTimes>>();
        for (int i = 0; i < QueriesList.Count; i++)
        {
            List<ResponseTimes> tempList = new List<ResponseTimes>();
            for (int j = 0; j < QueriesList[i].QueryList.Count; j++)
            {
                tempList.Add(new ResponseTimes());
            }
            RSList.Add(tempList);

            QueryListCounters[i] = 0;
            KeysToIndexMap.Add(QueriesList[i].Hotkey, i);

            //if the question type is active then enable the hotkey for the test
            if (QueriesList[i].ActiveForTest)
            {
                CurrentActiveKeys.Add(QueriesList[i].Hotkey, i);
                InitActiveKeys.Add(QueriesList[i].Hotkey, i);
            }
        }

        //this is the number of question types we will write to the json file 
        
        

        foreach (var key in InitActiveKeys)
        {
            Debug.Log(key);
        }
        //New Date
        ExperimentDate = DateTime.Now;


        Debug.Log("Query Manager Reset");
        //Debug log more stuff here maybe
        Debug.Log(ExperimentDate);
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

    private void ResetJSONFile()
    {

        Debug.Log("JSON file has been reset");
        string InitText = "{ \r\n \"TestData\":[  \r\n $ \r\n ] \r\n }";

        File.WriteAllText(DataLogFilePath, InitText);
    }
}
