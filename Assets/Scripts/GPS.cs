//based on tutorial = https://www.youtube.com/watch?v=4mnzTWyrK5A&list=PLoioalm_uXfYM3nN7FzCdILd2qR5cTj52

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using Mapbox.Examples;
using System.Diagnostics;
using Mapbox.Utils;
using Mapbox.Unity.Utilities;


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPS : MonoBehaviour
{
    public float[] latitude;
    public float[] longitude;
    public GameObject[] atLocationPopup;

    public float[] atLocationPopupLatitude;
    public float[] atLocationPopupLongitude;


    private int Counter;
    private float startLatitude;
    private float startLongitude;
    public float radius = 10f;
    private SpawnOnMap _spawnOnMap;


    void Start()
    {
        Input.location.Start();
        startLatitude = Input.location.lastData.latitude;
        startLongitude = Input.location.lastData.longitude;
        StartCoroutine(gpsUpdate());
        _spawnOnMap = FindObjectOfType<SpawnOnMap>();
        RetrieveLocationMarkers();
    }

    void RetrieveLocationMarkers()
    {
        if (_spawnOnMap != null)
        {
            string[] locationStrings = _spawnOnMap.LocationStrings;
            latitude = new float[locationStrings.Length];
            longitude = new float[locationStrings.Length];

            for (int i = 0; i < locationStrings.Length; i++)
            {
                string locationString = locationStrings[i];
                UnityEngine.Debug.Log("Location " + i + ": " + locationString);

                // split the location string into latitude and longitude values
                string[] coordinates = locationString.Split(',');
                float.TryParse(coordinates[0], out latitude[i]);
                float.TryParse(coordinates[1], out longitude[i]);


            }
        }
        else
        {
            UnityEngine.Debug.LogError("SpawnOnMap component not found.");
        }
    }


    IEnumerator gpsUpdate()
    {
        while (true)
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                startLatitude = Input.location.lastData.latitude;
                startLongitude = Input.location.lastData.longitude;
            }
            yield return new WaitForSeconds(10);
        }
    }

    void CheckDistance()
    {
        // First, check if the user is within range of the atLocationPopup objects
        for (int i = 0; i < atLocationPopup.Length; i++)
        {
            if (atLocationPopup[i] != null)
            {
                float distance = GetDistanceFromLatLonInKm(startLatitude, startLongitude, atLocationPopupLatitude[i], atLocationPopupLongitude[i]);

                if (distance <= radius && !atLocationPopup[i].activeSelf)
                {
                    atLocationPopup[i].SetActive(true);
                }
                else if (distance > radius && atLocationPopup[i].activeSelf)
                {
                    atLocationPopup[i].SetActive(false);
                }
            }
        }

        // Next, check if the user is within range of the locationMarkers
        for (int i = 0; i < latitude.Length; i++)
        {
            float markerLatitude = latitude[i];
            float markerLongitude = longitude[i];

            // calculate the distance between the user's current location and the location marker
            float distance = GetDistanceFromLatLonInKm(startLatitude, startLongitude, markerLatitude, markerLongitude) * 1000f;

            if (distance <= radius)
            {
                // show the popup if the user is within range of the location marker
                atLocationPopup[i].SetActive(true);
                return;
            }
        }
    }



    float GetDistanceFromLatLonInKm(float lat1, float lon1, float lat2, float lon2)
    {
        var R = 6371f; // Radius of the earth in km
        var dLat = (lat2 - lat1) * Mathf.Deg2Rad;  // deg2rad below
        var dLon = (lon2 - lon1) * Mathf.Deg2Rad;
        var a =
            Mathf.Sin(dLat / 2f) * Mathf.Sin(dLat / 2f) +
            Mathf.Cos(lat1 * Mathf.Deg2Rad) * Mathf.Cos(lat2 * Mathf.Deg2Rad) *
            Mathf.Sin(dLon / 2f) * Mathf.Sin(dLon / 2f)
            ;
        var c = 2f * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1f - a));
        var d = R * c; // Distance in km
        return d;
    }


}
