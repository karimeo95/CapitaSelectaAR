using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

//this script is responsible for retrieving data from the Energy Data Platform of the University of Twente.
//The API and documentation can be seen: https://energyapi.utwente.nl/index.html
public class API_Handler : MonoBehaviour
{
    private positionHandler positionHandler;
    private string[] buildingNames;

    //declare the apiValues as a public float such that other scripts can obtain the values but not modify them. 
    public static float[] apiValues { get; private set; }


    public beamCreator beamCreator;

    public void Start()
    {
        //create  a reference to the beamCreator, so we can call the colouring function later when we retrieved the data. 
        beamCreator = FindObjectOfType<beamCreator>();

        //additionally, create a  reference to the positionHandler, so we can obtain the list of buildings in range. 
        positionHandler = FindObjectOfType<positionHandler>();
    }

    public void initiateAPI()
    {
        //create a reference to the data selector, so we can know what data the user has selected.
        dataSelector selector = FindObjectOfType<dataSelector>();
        string selectedData = dataSelector.selectedData;

        //the buildings in range we retrieve from positionHandler and then store in a string array. 
        buildingNames = positionHandler.buildingsInRange.ToArray();

        //create an array to hold the output values from the api. this size should be the same as the amount of buildings in range. 
        apiValues = new float[buildingNames.Length];

        //for all the buildings in range:
        for (int i = 0; i < buildingNames.Length; i++)
        {
            //trigger the coroutine to retrieve the api data for the building at the i position in the array.
            //we call it with the variable buildingNames, the data type, and the i for the current index in the array.
            //this is important so we know that the obtained data matches the building. 

            StartCoroutine(retrieveAPIdata(buildingNames[i], selectedData, i));
        }
    }

    //method to retrieve the API dat ausing a webrequest. 
    //tutorial follows; https://www.youtube.com/watch?v=K9uVHI645Pk
    //uploaded Feb 8, 2021. User: Tarodev. Video Name: "Sending Web Requests in Unity - UnityWebRequest".
    //adjusted. 

    IEnumerator retrieveAPIdata(string buildingName, string energyType, int index)
    {
        //at current, we only use data from today until tomorrow (this is actually only today, but the API will not let us call with only today as date for from and to). 
        //more options could be added to change the data type to week/month etc. 

        ///get the date of today and the day of tomorrow. 
        DateTime currentDate = DateTime.Now;
        DateTime tomorrowDate = currentDate.AddDays(1);

        //the energyapi of the UT specified a format of YYYY-MM-DD. therefore, we must conver the date to this format. 
        string fromDate = currentDate.ToString("yyyy-MM-dd");
        string toDate = tomorrowDate.ToString("yyyy-MM-dd");

        // Construct the API URL.
        //both the buildingname and energy type have to be lower case to be accepted by the api. 
        //corrected can be adjusted to false if desired. 

        string apiUrl = $"https://energyapi.utwente.nl/api/Energy/{buildingName.ToLower()}/{energyType.ToLower()}?resolution=day&from={fromDate}&to={toDate}&corrected=true";

        //call the webrequest object constructor with our URL 
        UnityWebRequest retrieveData = CreateRequest(apiUrl);

        //wait for the webrequest to complete. 
        yield return retrieveData.SendWebRequest();

        //if our webrequest was succesfull. 
        if (retrieveData.result == UnityWebRequest.Result.Success)
        {
            //retrieve the webresponse in string format. 
            string response = retrieveData.downloadHandler.text;

            //deserialize the webresponse we got. 
            deserializedData apiResponse = JsonUtility.FromJson<deserializedData>(response);

            //if the response is not null and if we have at least one data element 
            if (apiResponse.results != null && apiResponse.results.data.Count > 0)
            {
                //retreive the deserialized float value.  
                float value = apiResponse.results.data[0].value;

                //store the retrieved value in our array at the i position. 
                apiValues[index] = value;
            }

            //recolour the beams now that we have obtained the data. 
            beamCreator.colorBeams();
        }

        retrieveData.Dispose();
    }

    //function to construct the webrequest object. 
    private UnityWebRequest CreateRequest(string url)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("accept", "application/json");
        return request;
    }


    //define three serializable classeses, so we can convert the JSON response into data formats we can use. 
    //deserializedData is the main component, which has the fields string and results. 
    //we decompose results further into unit and a list of data
    //the list dataentry is decmposed into timestamp, value and corrected. 
    //it is the value we need to use. 

    [Serializable]
    public class deserializedData
    {
        public string error;
        public Results results;
    }

    [Serializable]
    public class Results
    {
        public string unit;
        public List<DataEntry> data;
    }

    [Serializable]
    public class DataEntry
    {
        public string timestamp;
        public float value;
        public bool corrected;
    }
}
