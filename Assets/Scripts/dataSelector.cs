using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class dataSelector : MonoBehaviour
{
    public Button Heat;
    public Button Gas;
    public Button Electricity;

    public static string selectedData;

    public TextMeshProUGUI minValueText;
    public TextMeshProUGUI maxValueText;

    //refference to API_Handler script
    private API_Handler API_Handler;

    //reference to beamCreator script
    private beamCreator beamCreator;


    //add listeners for button presses. if the button is pressed, the argument is passed onto the buttonPress method as string data. 
    void Start()
    {
        Heat.onClick.AddListener(() => buttonPress("heat"));
        Gas.onClick.AddListener(() => buttonPress("gas"));
        Electricity.onClick.AddListener(() => buttonPress("electricity"));

        beamCreator = FindObjectOfType<beamCreator>();
        API_Handler = FindObjectOfType<API_Handler>();

    }

    void buttonPress(string data)
    {

        //set the public string to be equal to the select data type
        selectedData = data;

        //forward the selected datatype to beamCreator so we know which gradients and values to use. 
        beamCreator.dataSelect(data);

        //trigger the API method in API_Handler script with the current selected data type. 
        API_Handler.initiateAPI();

        // Set specific values based on selected data. This should rather be automated via the values in beamCreator but this was not implemented yet. 
        if (selectedData == "heat")
        {
            minValueText.text = "0";
            maxValueText.text = "25";
        }
        else if (selectedData == "gas")
        {
            minValueText.text = "0";
            maxValueText.text = "300";
        }
        else if (selectedData == "electricity")
        {
            minValueText.text = "500";
            maxValueText.text = "10000";
        }
    }

    //this could be used to update the text of the minimum and maximum value based on the entered minValue and maxValue in the beamCreator script in the colorBeams() method. Right now this is not happening yet. 
    public void updateText()
    {
    }
}
