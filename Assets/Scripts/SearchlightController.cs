using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;


public class SearchlightController : MonoBehaviour
{
    private GameObject          theGameController;          // game controller
    private GameObject          thePlayer;                  // game controller
    private PlayerController    thePlayerControllerScript;  // player controller
    private GameplayController  theGamecontrollerScript;    // game controller

    private List<SearchLight>   theSearchlights;            // list of spot light names/start & end angles/current sweep direction
    private List<Light>         theActualLights;            // light game objects

    private class SearchLight
    {
        public string name;            // name of searchlight to be rotated
        public float  startYAxisAngle; // starting position for rotation around Y-Axis
        public float  endYAxisAngle;   // end position for rotation around Y-Axis
        public bool   goingRight;      // direction of current travel (always travels from left to right)
    }

    // Start is called before the first frame update
    void Start()
    {
        // Find the required objects needed here
        theGameController = GameObject.Find("GameplayController");

        if (theGameController == null)
        {
            // not found
            UnityEngine.Debug.Log("Can't find Gameplay Controller in searchlight controller!");
        }
        else
        {
            // get game controller script
            theGamecontrollerScript = theGameController.GetComponent<GameplayController>();
        }

        thePlayer = GameObject.Find("Player");

        if (thePlayer == null)
        {
            // player not found
            UnityEngine.Debug.Log("Can't find Player in searchlight controller!");
        }
        else
        {
            // get game controller script
            thePlayerControllerScript = thePlayer.GetComponent<PlayerController>();
        }

        /* Each Searchlight is TAGGED "Search Spotlight" in the GUI, and has a child light under it whose "name" e.g. "HQ Spot 1" has been
         * stored in a List <SearchAngles> above for finding out its start & end positions and current direction
         * of travel when calculating movement direction
         * */

        // create the list of search lights (name, start rotation, end rotation, direction of travel for sweeps)
        // have to experiment with angles to get desired effect as seems rotation is based on quadrants
        // in Unity, so not simple to just say move from X degrees to Y degrees!
        theSearchlights = new List<SearchLight>()
                              {
                                 new SearchLight {name = "HQ Search 1", startYAxisAngle = 0f,  endYAxisAngle = 23f, goingRight = true},
                                 new SearchLight {name = "HQ Search 3", startYAxisAngle = 20f, endYAxisAngle = 7f,  goingRight = true}
                              };

        theActualLights = new List<Light>();

        // store the searchlights in an array as must be disabled during daytime
        foreach (SearchLight current in theSearchlights)
        {
            // find searchlight & add to list for enable/disable purposes
            Light theLight = GameObject.Find(current.name).GetComponentInChildren<Light>(); // find named Searchlight
            theActualLights.Add(theLight);
            theLight.gameObject.SetActive(false);
        }
    }

    // enable/ disable the displayed searchlights
    public void EnableSearchlights(bool bEnable)
    {
        foreach (Light current in theActualLights)
        {
            // find searchlight & add to list for enable/disable purposes
            current.gameObject.SetActive(bEnable);
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        // check if night mode is set to on in player controller & if
        // so then we must rotate the searchlights in the game

        if (thePlayerControllerScript.IsNightMode() && theGamecontrollerScript.HasGameStarted())
        {
            // only move searchlights when on at night, and game has started

            foreach (SearchLight current in theSearchlights)
            {
                // find & check each child Light's current Y-Axis angle, it's direction of travel, and rotate it until reaches
                // its end position, then just set it to reverse so it travels back towards the start on the next update here

                GameObject theLight = GameObject.Find(current.name);                 // find named Searchlight
                float currentYAngle = theLight.transform.rotation.eulerAngles.y; // get it's current rotation

                // N.B. ALL light sweeps start from left to right, and then reverse back

                float startAngle = current.startYAxisAngle;
                float endAngle   = current.endYAxisAngle;
                bool  direction  = current.goingRight;

                if (direction == true)
                {
                    // going right - so continue till end of sweep, then set reverse
                    if ( currentYAngle <= (180-endAngle) )
                    {
                        theLight.transform.RotateAround(
                                       new Vector3(theLight.transform.position.x, theLight.transform.position.y, theLight.transform.position.z),
                                                   Vector3.up,
                                                   -(endAngle - currentYAngle) * Time.deltaTime * 0.1f);
                        //UnityEngine.Debug.Log("current Angle = " + (currentYAngle) + ", End Angle = "+ endAngle);
                    }
                    else
                    {
                        // reverse direction
                        current.goingRight = false;
                       // UnityEngine.Debug.Log("RIGHT SWEEP HAS ENDED");
                    }
                }

                if (direction == false)
                {
                    // now going left (back) - so continue till end of sweep, then set reverse again
                    if ((90 - currentYAngle) < startAngle)
                    {
                        // rotate spotlight
                        theLight.transform.RotateAround(
                                      new Vector3(theLight.transform.position.x, theLight.transform.position.y, theLight.transform.position.z),
                                                  Vector3.up, 
                                                  -(180 - (currentYAngle - startAngle)) * Time.deltaTime * 0.1f);
                    }
                    else
                    {
                        // reverse direction again
                        current.goingRight = true;
                        //UnityEngine.Debug.Log("LEFT SWEEP HAS ENDED");
                    }

                   // UnityEngine.Debug.Log("Current Angle Reversing = " + currentYAngle + " Start Angle = " + startAngle);
                }
            }   
        }
    }
}
