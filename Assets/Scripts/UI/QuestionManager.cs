using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class QuestionManager : MonoBehaviour {

    //struct to save the pair of response times for each query
    public class ResponseTimes
    {
        // Response time for the popup
        public float PRS;
        // Response time for the query
        public float QRS;

        //constructor for fast init
        public ResponseTimes()
        {
            PRS = 0.0f;
            QRS = 0.0f;
        }

    }

    //struct to assist with saving date time in json format
    struct JsonDateTime
    {
        public long value;
        public static implicit operator DateTime(JsonDateTime jdt)
        {
            Debug.Log("Converted to time");
            return DateTime.FromFileTimeUtc(jdt.value);
        }
        public static implicit operator JsonDateTime(DateTime dt)
        {
            Debug.Log("Converted to JDT");
            JsonDateTime jdt = new JsonDateTime();
            jdt.value = dt.ToFileTimeUtc();
            return jdt;
        }
    }

    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Items;
        }

        public static string ToJson<T>(T[] array)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper);
        }

        public static string ToJson<T>(T[] array, bool prettyPrint)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
    }


    [Serializable]
    public class JsonData
    {
        public long Date;
        public int UserID;
        public float[] PRS;
        public float[] QRS;
    }


    //Inspector vars
    public float PresetDelayTime;
    public int TestSubjectID;
    public bool IsActive;
    public List<string> QueriesList;
    

    //Private reference vars
    private GameObject PopUpMessagePanel;
    private GameObject QueryPanel;
    private Text QueryText;
    private AudioSource source;

    //private vars
    private int QueryCounter;
    private List<ResponseTimes> RSList;
    private bool DisplayingPopUp;
    private bool DisplayingQuery;
    private bool DelayingQuery;
    private float PopUpTimer;
    private float QueryTimer;
    private float DelayTimer;
    private float QueryDelay;
    private DateTime ExperimentDate;
    private string DataLogFilePath ;


    // Use this for initialization
    void Start ()
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
	        QueryText.text = QueriesList[0];
            //Deactivate the panels
            PopUpMessagePanel.SetActive(false);
	        QueryPanel.SetActive(false);
        }
	    else
	    {
	        Debug.Log("QueryManager could not locate predefined panels");
	    }

	    QueryCounter = 0;
	    QueryDelay = 0;
	    DisplayingPopUp = DisplayingQuery = DelayingQuery = false;
        ResetTimers();
        //Initialize the list of response times
        RSList = new List<ResponseTimes>();
	    foreach (var query in QueriesList)
	    {
            RSList.Add(new ResponseTimes());
	    }

        ExperimentDate = DateTime.Now;
        Debug.Log(ExperimentDate);
	    DataLogFilePath = Application.streamingAssetsPath + "/TestLogData/UserTestData.json";

	}
	
	// Update is called once per frame
	void Update ()
	{
        //If the question manager is not active then do not check for keyboard input.
        //This ensures that keys will not call the QuestionManager's functionality while typing in other situations e.g during chat message
	    if (!IsActive)
            return;


        
        //Nothing displayed to the user
	    if (!DisplayingPopUp && !DisplayingQuery)
	    {
            //Tester Input
	        if (Input.GetKeyDown(KeyCode.M))
	        {
	          //  Debug.Log("Pressed M when nothing displayed");
                ShowPopUp();
	            DisplayingPopUp = true;
	        }
        }
        //PopUp is displaying
        else if (DisplayingPopUp)
	    {
            //while the popup is displayed, increment timer
	        if (!DelayingQuery)
	        {
	            PopUpTimer += Time.deltaTime;
	            if ((Input.GetKeyDown(KeyCode.Y) || Input.GetKeyDown(KeyCode.N)) )
	            {
	                ClosePopUp();
	                
	                //Tester presses response key according to user's verbal answer
	                // Y -> Yes
	                // N -> No
                    if (Input.GetKeyDown(KeyCode.Y))
	                {
	                 //   Debug.Log("Pressed Yes in PopUp");
                        //Save timer
                        RSList[QueryCounter].PRS = PopUpTimer;
                        //Essentially instant query show
	                    QueryDelay = 0.1f;
	                }
	                else if (Input.GetKeyDown(KeyCode.N))
	                {
	                  //  Debug.Log("Pressed No in PopUp");
                        RSList[QueryCounter].PRS = PresetDelayTime;
                        //Delay showing query by full preset time
	                    QueryDelay = PresetDelayTime;
	                }

	                DelayingQuery = true;
	            //    Debug.Log("Delaying started");
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

	        if (Input.GetKeyDown(KeyCode.K))
	        {
	          //  Debug.Log("Pressed K while display query");
                //Save QRS
	           // Debug.Log(QueryCounter);
                RSList[QueryCounter].QRS = QueryTimer;
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
       // Debug.Log("PopUp Active");
    }

    private void ClosePopUp()
    {
        //For now no animations or anything else
        PopUpMessagePanel.SetActive(false);
       // Debug.Log("PopUp Inactive");
    }

    private void ShowQuery()
    {
        //For now no animations or anything else
        QueryPanel.SetActive(true);
       // Debug.Log("Query Active");
    }
    private void CloseQuery()
    {
        //If there are still questions in the list, then prepare the next question's text
        if (QueryCounter < QueriesList.Count-1)
        {
            QueryText.text = QueriesList[++QueryCounter];
          //  Debug.Log("Incrementing question counter" + QueryCounter);
        }
        //If all questions were answered, then save the data on a json and make the QueryManager inactive
        else
        {
            WriteDataToFile();
            IsActive = false;
        }

        //For now no animations or anything else
        QueryPanel.SetActive(false);
       // Debug.Log("Query inactive");
    }

  

    private void WriteDataToFile()
    {

       
        Debug.Log("Writing to file");

        JsonData data = new JsonData();
        data.UserID = TestSubjectID;
        data.Date = ExperimentDate.ToFileTimeUtc();

        data.PRS = new float[RSList.Count];
        data.QRS = new float[RSList.Count];

        for (int i = 0; i < RSList.Count ; i++)
        {
            data.PRS[i] = RSList[i].PRS;
            data.QRS[i] = RSList[i].QRS;
            //Debug.Log("PRS : " + rs.PRS + "     QRS : " + rs.QRS);
        }
        string json = JsonUtility.ToJson(data,true);

        //TODO fix format of json
        File.AppendAllText(DataLogFilePath,json);
    }

    private void ResetTimers()
    {
        PopUpTimer = QueryTimer = DelayTimer =  0.0f;
    }
}
