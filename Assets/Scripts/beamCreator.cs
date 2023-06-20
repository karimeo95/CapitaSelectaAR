using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Mapbox.Examples;
using Mapbox.Utils;
using Mapbox.Unity.Utilities;
using Mapbox.Unity.Map;
using System.Diagnostics;
using TMPro;

public class beamCreator : MonoBehaviour
{
    public List<float> xPositions;
    public List<float> zPositions;

    public GameObject beamPrefab;
    public GameObject beamHolder;

    private positionHandler positionHandler;
    private List<GameObject> instantiatedBeams = new List<GameObject>();

    public Gradient heatGradient;
    public Gradient electricityGradient;
    public Gradient gasGradient;

    public float heatValue;
    public float electricityValue;
    public float gasValue;

    public Color beamColor;

    public Material BeamMaterial;

    public string selectedData = "heat";

    private List<string> buildingNames;


    // Reference to API_Handler script
    private API_Handler apiHandler;

    void Start()
    {
        beamColor.a = 0.1f;

        // Get reference to the API_Handler script
        apiHandler = GetComponent<API_Handler>();

    }

    //method to generate the beams when we switch to the AR scene. the colouring happens after data retrieval. 
    public void generateBeams()
    {
        //first, make a connection to the positionHandler script, as it is in another scene
        positionHandler = FindObjectOfType<positionHandler>();

        //from the positionHandler script, obtain the calculated xPositions and zPosition arrays.
        xPositions = positionHandler.xPositions;
        zPositions = positionHandler.zPositions;

        //also obtain the buildingNames array. 
        buildingNames = positionHandler.buildingsInRange;

        UnityEngine.Debug.Log("Number of points received: " + xPositions.Count);

        //for each entry in the xPositions array,  
        for (int i = 0; i < xPositions.Count; i++)
        {
            //set the float x and z equal to that xPosition and zPosiiton at the i-entry of the array. 
            float x = xPositions[i];
            float z = zPositions[i];
            
            //set the string of the buildingName for the current i position in the array 
            string buildingName = buildingNames[i];

            //some debugging to verify. 
            UnityEngine.Debug.Log("Building Name for beam " + i + ": " + buildingName);

            //set a Vector3 for the beam position using the correct x and z values for this beam at position i. 
            Vector3 beamPosition = new Vector3(x, 0f, z);

            // Create the beam prefab at the correct position. 
            GameObject instantiatedBeam = Instantiate(beamPrefab, beamPosition, Quaternion.identity);


            //set the instantiated beam to be a parent of the beamHolder, so they dont accidentally get parented to the camera, causing them to move as we move. 
            instantiatedBeam.transform.SetParent(beamHolder.transform);

            //add to the list 
            instantiatedBeams.Add(instantiatedBeam);

            //check the children of each instantiated beam for a text element with tag "buildingName". Note: here we use GetComponentsInChildren, because using GetComponentInChildren only looks at the first child. 
            TextMeshPro[] buildingNameTexts = instantiatedBeam.GetComponentsInChildren<TextMeshPro>(true);


            foreach (TextMeshPro buildingNameText in buildingNameTexts)
            {
                //if the textg element has tag buildingName, then set that tag equal to the correct building name. 
                if (buildingNameText.CompareTag("buildingName"))
                {
                    buildingNameText.text = buildingName;
                }
            }


        }

        colorBeams();
    }

    //in order to ensure that the data is facing the user, we need to rotate the cylinders to face the camera.
    //we do this as such: the negative z-axis of the beams should face the camera. if this is done the text will face the camera. 
    public void Update()
    {
        //create a reference to the AR camera.

        GameObject AR_Camera = GameObject.Find("AR Session Origin/AR Camera");

        //if we cannot find the AR_Camera, we are not in the AR scene yet. 
        if (AR_Camera == null)
        {
            return;
        }

        //if we do find the AR camera, then: 
        Vector3 cameraPosition = AR_Camera.transform.position;

        //for each cylinder in the array
        foreach (GameObject instantiatedBeam in instantiatedBeams)
        {
            //first, determine the direction from the beam towards the camera. 
            Vector3 beamToCamera = cameraPosition - instantiatedBeam.transform.position;

            //then, determine the required rotation for the beam about the z-axis such that it's negative Z-axis points towards the camera
            Quaternion targetRotation = Quaternion.LookRotation(-beamToCamera, Vector3.up);

            //lastly, apply the rotation. 
            instantiatedBeam.transform.rotation = targetRotation;
        }
    }

    //method to recolour the beams given the data retrieved. 
    public void colorBeams()
    {
        //for each array entry in instantiatedBeams:
        for (int i = 0; i < instantiatedBeams.Count; i++)
        {
            //create a reference to the gameobject at position i in this array. 
            GameObject instantiatedBeam = instantiatedBeams[i];
            
            //for the current gameobject at position i, get its beamRenderer
            Renderer beamRenderer = instantiatedBeam.GetComponent<Renderer>();

            //instantiate a material for this particular beam, so we can change only this beams colour and text. 
            Material instanceMaterial = new Material(BeamMaterial);
            beamRenderer.material = instanceMaterial;

            // Set the color based on the appropriate gradient
            Color beamColor = Color.white;

            //three if statements, one for each data type currently added: heat, electricity and gas.
            //the logic in each follows the same.
            
            //if the data type is heat: 
            if (selectedData == "heat")
            {
                //and the apivalues we retreived is not null
                if (API_Handler.apiValues[i] != null)
                {
                    // value in GJ

                    //specify a minimum and maximum value for our gradient. 
                    float minValue = 0;
                    float maxValue = 25f;

                    //for the current i position, check where in the range the retrieved value falls. set the colour of the instantiated beam to this. 
                    beamColor = heatGradient.Evaluate(Mathf.InverseLerp(minValue, maxValue, API_Handler.apiValues[i]));

                    //get the text component. 
                    TextMeshPro dataValue = instantiatedBeam.GetComponentInChildren<TextMeshPro>();

                    //find the text compoment for the data value. then, paste the current api value at i there + the unit. 
                    if (dataValue.CompareTag("dataValue"))
                    {
                        dataValue.text = API_Handler.apiValues[i].ToString() + "GJ";
                    }

                }
                //if the api value is null: 
                else
                {
                    //colour the beam gray to indicate no data. 
                    beamColor = Color.gray;

                    //set the text to show "no data". 
                    TextMeshPro dataValue = instantiatedBeam.GetComponentInChildren<TextMeshPro>();
                    if (dataValue.CompareTag("dataValue"))
                    {
                        dataValue.text = "no data";
                    }
                }
            }
            else if (selectedData == "electricity")
            {
                if (API_Handler.apiValues[i] != null)
                {
                    // value in kWh
                    float minValue = 500f;
                    float maxValue = 10000f;

                    beamColor = heatGradient.Evaluate(Mathf.InverseLerp(minValue, maxValue, API_Handler.apiValues[i]));

                    TextMeshPro dataValue = instantiatedBeam.GetComponentInChildren<TextMeshPro>();
                    if (dataValue.CompareTag("dataValue"))
                    {
                        dataValue.text = API_Handler.apiValues[i].ToString() + "kWh";
                    }
                }
                else
                {
                    beamColor = Color.gray;
                    TextMeshPro dataValue = instantiatedBeam.GetComponentInChildren<TextMeshPro>();
                    if (dataValue.CompareTag("dataValue"))
                    {
                        dataValue.text = "no data";
                    }
                }
            }
            else if (selectedData == "gas")
            {
                if (API_Handler.apiValues[i] != null)
                {
                    // value in m3
                    float minValue = 0;
                    float maxValue = 300f;
                    beamColor = heatGradient.Evaluate(Mathf.InverseLerp(minValue, maxValue, API_Handler.apiValues[i]));

                    TextMeshPro dataValue = instantiatedBeam.GetComponentInChildren<TextMeshPro>();
                    if (dataValue.CompareTag("dataValue"))
                    {
                        dataValue.text = API_Handler.apiValues[i].ToString() + "m3";
                    }


                }
                else
                {
                    beamColor = Color.gray;

                    TextMeshPro dataValue = instantiatedBeam.GetComponentInChildren<TextMeshPro>();
                    if (dataValue.CompareTag("dataValue"))
                    {
                        dataValue.text = "no data";
                    }
                }
            }

            //apply the color to the instance material. 
            instanceMaterial.SetColor("_Color", beamColor);
        }
    }

    //method to set the current data type selected. this is needed to communicate the data type with other scripts. 
    public void dataSelect(string data)
    {
        selectedData = data;
    }

    //method to destroy the instantiated beams and empty the list. called in the sceneHandler when exiting the AR scene. 
    public void destroyBeams()
    {
        foreach (GameObject instantiatedBeam in instantiatedBeams)
        {
            Destroy(instantiatedBeam);
        }

        instantiatedBeams.Clear();
    }
}
