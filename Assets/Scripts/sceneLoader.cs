using UnityEngine;
using UnityEngine.SceneManagement;
using Mapbox.Examples;

//this script is used to handle the switching between different scenes. 
public class sceneLoader : MonoBehaviour
{

    private void Awake()
    {
        initiateApp();
    }

    private void initiateApp()
    {
        initializeCheck initializeCheck = FindObjectOfType<initializeCheck>();

        //the first time we run the app, we need to load scene 2. scene 2 has a script called initializeCheck atached to it. If we cannot find it, it means scene 2 is not loaded yet. Hence, load it. Otherwise, do not. 
        if (initializeCheck == null)
        {
            //add an extra check if scene 2 has loaded, just in case. To avoid having objects being instantiated twice. 
            if (SceneManager.GetSceneByBuildIndex(2).isLoaded == false)
            {
                SceneManager.LoadScene(2, LoadSceneMode.Additive);
            }
        }
    }

    public void loadArScene()
    {
        //destroy any Mapbox Markers, we do not want to see these in the AR scene, only on the 2D mapbox map. 
        SpawnOnMap SpawnOnMap = FindObjectOfType<SpawnOnMap>();
        SpawnOnMap.destroyMarkers();

        SceneManager.UnloadSceneAsync(0);
        SceneManager.LoadScene(1, LoadSceneMode.Additive);
            
    }

    public void loadMapBoxScene()
    {
        //destroy any AR beams we created. we do not want to see these beams on the 2D mapbox map. 
        beamCreator beamCreator = FindObjectOfType<beamCreator>();
        beamCreator.destroyBeams();

        SceneManager.UnloadSceneAsync(1);
        SceneManager.LoadScene(0, LoadSceneMode.Additive);
    }
}