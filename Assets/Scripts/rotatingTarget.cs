using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class rotatingTarget : MonoBehaviour
{

    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private float amplitude = 2.0f;
    [SerializeField] private float frequency = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        HoverAndRotate();
    }

    //function to hover the target object above the map and rotate it for dynamic effect
    void HoverAndRotate()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        //change the position of the object so it moves up and down in a sine pattern. +5 is the base height. 
        transform.position = new Vector3(transform.position.x, (Mathf.Sin(Time.fixedTime * Mathf.PI * frequency) * amplitude) + 5, transform.position.z);
    }
}
