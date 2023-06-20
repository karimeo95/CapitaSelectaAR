using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Mapbox.Examples;
using System.Diagnostics;
using Mapbox.Utils;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.Map;
using System;


//this script is used as main logic.
//it fist imports the array with location coordinates of points of interest from mapbox (see SpawnOnMap script attached to eventSpawner).
//additionally, it imports building names (can also be other names) for these coordinates. Make sure that the array entries match
//eg: location string entry 0 should match name array element 0. 

public class positionHandler : MonoBehaviour
{
    //create an array of latitude and longitudes, to hold the position of marker objects as made by MapBox. See eventSpawner object for the coordinates. Fixed amount entered in inspector so we use array.
    public float[] latitude;
    public float[] longitude;

    //declare float arrays for distances and directions. these will hold, for each building or other point of interest, the distance and direction from the user to the building. Fixed size so we use array.
    public float[] distances;
    public float[] directions;

    //ensure there is a link to SpawnOnMap script, which is the mapbox script to create markers on the 2D map.
    private SpawnOnMap SpawnOnMap;

    //create a reference to the popupHandler in the map scene
    private popupHandler popupHandler;

    //create a list for xPositions and zPositions. we need a list here instead of array because the amount of positions can vary throughout, depending which buildings are in range. 
    public List<float> xPositions = new List<float>();
    public List<float> zPositions = new List<float>();

    //for the building names, declare an array and a list. the buildingNames is again fixed size, but the buildingsInRange can varry, so we again need list. 
    private string[] buildingNames;
    public List<string> buildingsInRange = new List<string>();

    //at the start, attempt to retrieve the location markers, create a link to the popuphandler in case we are in range, and start a coroutine for delaytime so we do not update the gps every frame. 
    private void Start()
    {
        RetrieveLocationMarkers();
        popupHandler = FindObjectOfType<popupHandler>();
        StartCoroutine(delayTime());
    }

    //this function retrieves the marker coordinates from SpawnOnMap.
    private void RetrieveLocationMarkers()
    {
        //clear xPositions and zPositions of beams so we don't keep these stored. Otherwise, the array will get populated with the same entries. This is in case we have switched back and forth. 
        xPositions.Clear();
        zPositions.Clear();

        //also clear the names of buildings and buildings in range, for the same reason. 
        buildingsInRange.Clear();

        //clear the array with building names for the same reason. we need a nullcheck to avoid a null error the first time we run the script. 
        if (buildingNames != null)
        {
            Array.Clear(buildingNames, 0, buildingNames.Length);
        }

        //create a link to the mapbox spawnonmap script. 
        SpawnOnMap = FindObjectOfType<SpawnOnMap>();

        //if there is a spawnonmap script,
        if (SpawnOnMap != null)
        {
            //first obtain an array of coordinates strings that were entered in the inspector. 
            string[] coordinateStrings = SpawnOnMap.LocationStrings;

            //additionally, obtain the corresponding array of building names. 
            buildingNames = SpawnOnMap.BuildingNames;
            
            //set the size of the latitude and longitude arrays to be the same as the amount of coordinate string we obtained. 
            latitude = new float[coordinateStrings.Length];
            longitude = new float[coordinateStrings.Length];

            // for each latitude and longitude entry in the coordinate strings arrays:
            for (int i = 0; i < coordinateStrings.Length; i++)
            {
                //for the current i position, obtain the string of coordinates. 
                string locationString = coordinateStrings[i];

                // The lat and longs are stored in strings in SpawnOnMap, so we need to move  these into separate arrays. hence, split the string at the current i position,parse the result in a coordinates array with 2 elements. 
                string[] coordinates = locationString.Split(',');

                //the coordinate array is split into two separate arrays. the 0 indicates we are parsing the first element of the array (value before the comma split into the latitude); second element into longitude. 
                float.TryParse(coordinates[0], out latitude[i]);
                float.TryParse(coordinates[1], out longitude[i]);
            }
        }
    }

    //incorporate a waiting time before each GPS update, to avoid excessive load. 
    private IEnumerator delayTime()
    {
        while (true)
        {
            yield return new WaitForSeconds(5.0f);
            StartCoroutine(gpsUpdate());
        }
    }

    //this part of the code compares the user's current position with the obtained latitude and longitude arrays to obtain a distance and direction array.
    //then, it checks which of these are in range (if any). if so, we compute x and z positions at which to instantiate objects by converting distance and directions. 
    //starting logic was based on the tutorial: https://www.youtube.com/watch?v=4mnzTWyrK5A, Author AbolFazl Tanha, date June 2, 2022. Video title "Step 3. GPS Single Point"

    private IEnumerator gpsUpdate()
    {
        //start location services
        Input.location.Start();

        //wait until initializing of services is completed
        while (Input.location.status == LocationServiceStatus.Initializing)
        {
            yield return new WaitForSeconds(0.5f);
        }

        //if the location service is now running, proceed:
        if (Input.location.status == LocationServiceStatus.Running)
        {
            //first, we obtain the users current position (latitude and longitude) and store this.
            float startLatitude = Input.location.lastData.latitude;
            float startLongitude = Input.location.lastData.longitude;

            //the part below is to update the center of the map to the user position (if it can be found, and we are therefore not in the AR scene).
            //this is to ensure we are always at the center of the map and we cannot "walk off the map". 

            AbstractMap map = FindObjectOfType<AbstractMap>();
            if (map != null)
            {
                //if there is a map element, we are currently in the 2D map scene. Then, update the center of the map with the user position
                //this is so the user remains in the center of the map, regardless of movement.
                map.UpdateMap(new Vector2d(startLatitude, startLongitude), map.Zoom);
            }
            else
            {
                UnityEngine.Debug.LogError("Cannot find a map object.");
            }

            //create two arrays to store the distance and direction in. its size must be the same as the amount of coordinates in the retrieved arrays.
            distances = new float[latitude.Length];
            directions = new float[latitude.Length];

            //clear xPositions and zPositions of beams so we don't keep these stored. Otherwise, the array will get populated with the same entries. 
            xPositions.Clear();
            zPositions.Clear();

            // For each latitude and longitude:
            for (int i = 0; i < latitude.Length; i++)
            {
                //for the current i position, obtain the latitude and longitude
                float markerLatitude = latitude[i];
                float markerLongitude = longitude[i];

                //then, the distance between the user and the marker at the current i position. multiply by 1000 to get meters.
                float distance = calculateDistance(markerLatitude, markerLongitude, startLatitude, startLongitude) * 1000f;

                //determine direction from the user to the marker at the current i position. 
                float direction = calculateDirection(markerLatitude, markerLongitude, startLatitude, startLongitude);

                //populate the distance and direction arrays at position i with the results. 
                distances[i] = distance;
                directions[i] = direction;

                UnityEngine.Debug.Log("Distance is " + distance);
                UnityEngine.Debug.Log("Direction is " + direction);

                // check if the distance between the user and any array entries is below 200 metres. 
                if (distance < 200f)
                {
                    UnityEngine.Debug.Log("user is within range");

                    //add the building name of the marker at the current i position to the list of the buildings in range. 
                    buildingsInRange.Add(buildingNames[i]);

                    // execute the determinePositions with arguments distance and direction to find the x and z positions at which to instantiate the object at this marker position. 
                    determinePositions(distance, direction);

                    // if there is a popupHandler, we are in the mapbox 2d scene. If so, we need to show a popup that we are in range and that we can switch to the AR scene.
                    popupHandler popupHandler = FindObjectOfType<popupHandler>();

                    if (popupHandler != null)
                    {
                        popupHandler.showLocationPopup();
                    }
                }
            }
        }
        else
        {
            //add a popup here to indicate to the user there is no GPS access. 
            UnityEngine.Debug.LogError("no GPS access provided by the user");
        }
    }


    //calculate the distance between two points.
    //Formula is the Haversine formula, implementation based on https://github.com/ombharatiya/Unity-GPS-Dist-Count/blob/master/Assets/GPSManager.cs and https://www.youtube.com/watch?v=4mnzTWyrK5A

    private float calculateDistance(float latitude1, float longitude1, float latitude2, float longitude2)
    {
        var radius = 6378.137f;
        float deltaLatitude = (latitude2 - latitude1) * Mathf.Deg2Rad;
        float deltaLongitude = (longitude2 - longitude1) * Mathf.Deg2Rad;
        float a = Mathf.Sin(deltaLatitude / 2) * Mathf.Sin(deltaLatitude / 2) +
            Mathf.Cos(latitude1 * Mathf.Deg2Rad) * Mathf.Cos(latitude2 * Mathf.Deg2Rad) *
            Mathf.Sin(deltaLongitude / 2) * Mathf.Sin(deltaLongitude / 2);
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1f - a));
        var d = radius * c;
        return d;
    }


    //calculate the direction from one point to the other. Based on http://www.movable-type.co.uk/scripts/latlong.html and https://stackoverflow.com/questions/49839339/bearing-calculation-in-c-sharp

    public float calculateDirection(float latitude1, float longitude1, float latitude2, float longitude2)
    {
        float deltaLongitude = (longitude2 - longitude1) * Mathf.Deg2Rad;
        float y = Mathf.Sin(deltaLongitude) * Mathf.Cos(latitude2 * Mathf.Deg2Rad);
        float x = Mathf.Cos(latitude1 * Mathf.Deg2Rad) * Mathf.Sin(latitude2 * Mathf.Deg2Rad) -
                  Mathf.Sin(latitude1 * Mathf.Deg2Rad) * Mathf.Cos(latitude2 * Mathf.Deg2Rad) * Mathf.Cos(deltaLongitude);
        float theta = Mathf.Atan2(y, x);
        float direction = (float)(theta * Mathf.Rad2Deg + 360.0f) % 360.0f;
        return direction;
    }


    //logic to determine game positions (x,y) at which to display the elements.
    //we use the direction and distance for this
    //then, we can apply simple trigonometry (y = distance * sin theta; x = distance * cos theta). 
    //the mobile phone screen has x axis from left to right and y axis from bottom to up. the z axis points outwards from the camera, essentially.
    //hence, we need to determine x and z. 

    public void determinePositions(float distance, float direction)
    {
        float x = distance * Mathf.Sin(direction * Mathf.Deg2Rad);
        float z = distance * Mathf.Cos(direction * Mathf.Deg2Rad);

        xPositions.Add(x);
        zPositions.Add(z);


        //some debugging to check if the calculations are correct.
        UnityEngine.Debug.Log("x position: " + x);
        UnityEngine.Debug.Log("z Position: " + z);
    }
}

