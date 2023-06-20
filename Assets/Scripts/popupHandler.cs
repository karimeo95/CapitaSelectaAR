using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this script is used to trigger a popup if the user is within range of a location marker
//this was put in a separate script because it is in a different scene.

public class popupHandler : MonoBehaviour
{
    public GameObject atLocationPopup;

    public void showLocationPopup()
    {
        atLocationPopup.SetActive(true);
    }

    public void hideLocationPopup()
    {
        atLocationPopup.SetActive(false);
    }


    //To do: add a popup for no gps signal
}
