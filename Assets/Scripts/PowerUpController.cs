using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class PowerUpController : MonoBehaviour
{
    private GameObject         theGameController;       // GameplayController object in scene
    private GameplayController theGameControllerScript; // the script attached to this object
    private PlayerController   thePlayerControlScript;  // player script for checking night mode
    private Light[]            theLightHolder;          // array of light gameobjects with Light Components within for night mode
    private Component[]        theLightComponent;       // need these to be able to re-activate them (how crap)
    
    private float lastUpdateTime;              // time was last updated
    private float startTime;                   // time first appeared on screen
    private float decayPeriod         = 10.0f; // decays a bit every 10 secs
    private int   decayCount          = 0;     // number of times powerup has decayed (deleted on points reaching 0)
    private int   powerUpPoints       = 5;     // score value - every power starts with 5 points
    private int   powerUpHealthPoints = 3;     // health points given for collecting this powerup

    private bool  hitByPlayer        = false;

    public TMP_Text  statusDisplayField;
    public AudioClip powerBoing;               // audio clip to play on "collecting" powerup
    
    // Start is called before the first frame update
    void Start()
    {
        theGameController = GameObject.FindGameObjectWithTag("GameController"); // game controller 

        if (theGameController != null)
        {
            theGameControllerScript = theGameController.GetComponent<GameplayController>(); // find the gameplay controller
        }
        else
        {
            UnityEngine.Debug.LogError("Didn't find object tagged GameController, check tag in Unity");
        }

        // find player script
        thePlayerControlScript = GameObject.Find("Player").GetComponent<PlayerController>();

        if (thePlayerControlScript == null)
        {
            // cant find it
            UnityEngine.Debug.Log("Can't find Player script from within Powerup controller");
        }

        lastUpdateTime = Time.realtimeSinceStartup; // start time we will increment later in update()
        startTime      = lastUpdateTime;

        // setup audio clip
        GetComponent<AudioSource>().playOnAwake = false;
        GetComponent<AudioSource>().clip        = powerBoing;

        string blank = "";

        statusDisplayField = GameObject.FindGameObjectWithTag("Status Display").GetComponent<TMP_Text>();

        // reset field
        statusDisplayField.text = blank.ToString();

        /////////// Prefab Name "Glowing Powerup Container" ///////////
        //                                                           //
        //                   |                                       //
        //             "Glowing Powerup" <- has this script          //
        //             "Light"                                       //
        //                |_> light    <-    'component' within      //
        //             "Light 1"                                     //
        //             "Light 2"                                     //
        //             "Light 3"                                     //
        //             "Light 4"                                     //
        //             "Light 5"                                     //
        //                                                           // 
        ///////////////////////////////////////////////////////////////

        // find the Light GameObjects in the prefab (from root transform's game object down)
        GameObject rootObject = transform.root.gameObject; // the glowing powerup container

        theLightHolder    = new Light[5];
        theLightComponent = new Component[5];

        theLightHolder = rootObject.GetComponentsInChildren<Light>();
        
        // save them & then check if we should illuminate the powerup
        // - we save them as Unity can't find inactive objects!
        if (thePlayerControlScript)
        {
            // illuminate (or not) all the 'light components' inside
            for (int i =0; i< theLightHolder.Length; i++)
            {
                // find the Light object from each gameObject
                theLightComponent[i] = theLightHolder[i].GetComponentInChildren<Light>();
            }

            // now see if we need to illuminate them
            foreach (Light current in theLightComponent)
            {
                // illuminate if night mode, switch off otherwise
                if (thePlayerControlScript.IsNightMode() == true)
                {
                    current.intensity = 28f;                }
                else
                {
                    current.intensity = 0f;
                }
            }
        }
    }

    public void SetPowerupGlowing(bool bGlow)
    {
        // illuminate (or not) all the light gameobjects inside
 
        foreach (Light current in theLightComponent)
        {
            // must be a light - illuminate if night mode, switch off otherwise
            if (current != null)
            {
                if (thePlayerControlScript.IsNightMode() == true)
                {
                    current.intensity = 28f;
                }
                else
                {
                    current.intensity = 0f;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // only cycle powerup states if not paused or game over
        if (!theGameControllerScript.IsGamePaused())
        {
            if (!theGameControllerScript.IsGameOver())
            {
                // do it periodically
                if (timeHasPassed())
                {
                    // change colour of powerup, and reduce points for collecting
                    decayCount++;
                    CycleToExtinction();
                }
            }
        }
    }

    void CycleToExtinction()
    {
        // change colour of power pill, delete it if fully decayed
        MeshRenderer theRenderer = gameObject.GetComponent<MeshRenderer>();
        Material theMaterial = theRenderer.material;

        switch (decayCount)
        {
            // all powerups start having 5 point value, and coloured bright green
            case 1:
                {
                    // update decay colour
                    theMaterial.color = new Color32(70, 106, 23, 255); // dark green
                    powerUpPoints--; // reduce points from 5 to 4
                    break;
                }
            case 2:
                {
                    // update decay colour
                    theMaterial.color = new Color32(255, 163, 0, 255); // orange
                    powerUpPoints--; // reduce points from 4 to 3
                    break;
                }
            case 3:
                {
                    // update decay colour
                    theMaterial.color = new Color32(255, 105, 105, 255); // light red
                    powerUpPoints--; // reduce points from 3 to 2
                    break;
                }
            case 4:
                {
                    // update decay colour
                    theMaterial.color = new Color32(168, 4, 4, 255); // dark red
                    powerUpPoints--; // reduce to one point
                    break;
                }

            case 5:
                {
                    // destroy lights around it
                    foreach (Light current in theLightHolder)
                    {
                        // check it's not the powerup
                        if (!current.CompareTag("Glowing Powerup"))
                        {
                            Destroy(current);
                        }
                    }

                    // destroy powerup
                    Destroy(gameObject);
                    break;
                }
        }

        if (decayCount == 5) decayCount = 0;
    }

    bool timeHasPassed()
    {
        // flag that a complete time period has passed for decaying powerup
        if (Time.realtimeSinceStartup >= lastUpdateTime + decayPeriod)
        {
            lastUpdateTime = Time.realtimeSinceStartup;
            return true;
        }
        else return false;
    }

    private bool bHitFirstTime = true; // prevent multiple collisions adding extra points

    private void OnTriggerEnter(Collider other)
    {
        // check who triggered this - if it's the player tell game manager        
        // to update score

        if (other.gameObject.CompareTag("Player") && !hitByPlayer)
        {
            // prevent random re-collisions giving more points
            hitByPlayer = true;

            // get collider and turn off
            gameObject.GetComponent<Collider>().enabled = false;

            GetComponent<AudioSource>().Play();

            // update score in game manager
            theGameControllerScript.UpdatePlayerScore(powerUpPoints);

            string points = powerUpPoints.ToString() + (powerUpPoints == 1 ? " POINT SCORED!" : " POINTS SCORED!");

            statusDisplayField.text = points;
            int bonusHealth = 0;

            if (bHitFirstTime)
            {
                // prevent multiple health points addition
                // randomly give Player a random bonus
                bHitFirstTime = false;

                if (Random.Range(1f, 20f) >= 17f)
                {
                    bonusHealth = Random.Range(10, 20);
                }

                if (bonusHealth > 0)
                {
                    // player got a bonus
                    string bonus = bonusHealth.ToString();
                    string blank = "YOU'RE LUCKY! BONUS HEALTH " + bonus + "%";

                    // find bonus health field
                    statusDisplayField.text = blank.ToString();
                }

                // update player health by remaining powerup health points and any bonus
                theGameControllerScript.UpdatePlayerHealth(powerUpHealthPoints + bonusHealth);
            }

            // destroy lights around it
            foreach (Light current in theLightHolder)
            {
                // check it's not the powerup
                if (!current.CompareTag("Glowing Powerup"))
                {
                    Destroy(current);
                }
            }
            
            // now destroy powerup
            Destroy(gameObject, 0.35f);
        }
    }
}
