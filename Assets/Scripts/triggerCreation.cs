using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this script is used to trigger the creation of the beams when the user switches to the AR Scene.

public class triggerCreation : MonoBehaviour
{
    private beamCreator beamCreator;

    // Start is called before the first frame update
    void Start()
    {
        beamCreator = FindObjectOfType<beamCreator>();
        beamCreator.generateBeams();
    }
}
