using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SuperPowerupController : MonoBehaviour
{
    private GameObject         theGameController;         // GameplayController object in scene
    private GameplayController theGameControllerScript;   // the script attached to this object
    private PlayerController   thePlayerControlScript;    // player script for checking night mode
    private SpawnManager       theSpawnManagerScript;     // needed to reset time of last super powerup spawn there
    private Light[]            theLightHolder;            // array of light gameobjects with Light Components within for night mode
    private Component[]        theLightComponent;         // need these to be able to re-activate them after turning off

    private float              startTime;                 // time first appeared on screen
    private int                powerupPoints     = 0;     // get real value from game controller
    private int                powerupExpiryTime = 2;     // how long before this expires (MINUTES)
    private bool               hitByPlayer       = false; // only allow points to be received once when hit
    private bool               bHitFirstTime     = true;  // prevent multiple collisions adding extra points

    public  AudioClip          superPowerBoing;           // audio clip to play on "collecting" powerup

    private void Awake()
    {
        // find game controller
        theGameController = GameObject.FindGameObjectWithTag("GameController"); // game controller 
    }
    // Start is called before the first frame update
    void Start()
    {
        if (theGameController != null)
        {
            theGameControllerScript = theGameController.GetComponent<GameplayController>(); // find the gameplay controller
            powerupPoints           = theGameControllerScript.GetSuperPowerupPoints();
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
            UnityEngine.Debug.Log("Can't find Player script from within Super Powerup controller");
        }

        theSpawnManagerScript = GameObject.FindGameObjectWithTag("SpawnManager").GetComponent<SpawnManager>();

        // start time needed - as will disappear after a couple of mins
        startTime = Time.realtimeSinceStartup; // start time we will increment later in update()

        // setup audio clip
        GetComponent<AudioSource>().playOnAwake = false;
        GetComponent<AudioSource>().clip        = superPowerBoing;

        /////////// Prefab Name "Super Powerup Container" /////////////
        //                 "has"                                     //
        //                   |                                       //
        //             "Super Powerup"        <- has this script     //
        //             "Light1"                                      //
        //                |_> light           <- a component' within //
        //             "Light 2"                 the "Light" object  //
        //                                                           // 
        ///////////////////////////////////////////////////////////////


        // find the Light GameObjects in the prefab (from root transform's game object down)
        GameObject rootObject = transform.root.gameObject; // the glowing powerup container

        theLightHolder    = new Light[2];
        theLightComponent = new Component[2];

        theLightHolder = rootObject.GetComponentsInChildren<Light>();

        // save them & then check if we should illuminate the powerup
        // - we save them as Unity can't find inactive objects! (maybe not needed as we now change intensity!

        if (thePlayerControlScript)
        {
            // illuminate (or not) all the 'light components' inside
            for (int i = 0; i < theLightHolder.Length; i++)
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
                    current.intensity = 400f;
                }
                else
                {
                    current.intensity = 0f;
                }
            }
        }
    }

    // remove bGlow parameter at some point as not needed now!
    public void SetPowerupGlowing(bool bGlow)
    {
        // illuminate (or not) all the light gameobjects inside the super powerups

        foreach (Light current in theLightComponent)
        {
            // must be a light - illuminate if night mode, switch off otherwise
            if (current != null)
            {
                if (thePlayerControlScript.IsNightMode() == true)
                {
                    current.intensity = 400f;
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
                // check if passed expiry time (set in game controller)
                if ((Time.realtimeSinceStartup - startTime) /60 >= powerupExpiryTime)
                {
                    // has met/exceeded expiry time, so get rid of it
                    PowerupExpired();
                    theGameControllerScript.PostImportantStatusMessage("SUPER POWERUP JUST EXPIRED!");
                }    
            }
        }
    }

    private void PowerupExpired()
    {
        // expired - destroy the Super Powerup Container & contents & reset time in spawn manager
        if (theSpawnManagerScript != null)
        {
            Debug.Log("Resetting Spawn time in SuperPowerupController");
            theSpawnManagerScript.ResetSuperSpawnTime(Time.realtimeSinceStartup);
        }

        Destroy(transform.parent.gameObject, 0.35f);
    }

    private void OnTriggerEnter(Collider other)
    {
        // check who triggered this - if the player update score & health in game manager        
        if (other.gameObject.CompareTag("Player") && !hitByPlayer)
        {
            // prevent re-collisions giving more points
            hitByPlayer = true;

            // turn collider off too for extra security
            gameObject.GetComponent<Collider>().enabled = false;

            GetComponent<AudioSource>().Play(); // play collected sound

            // update score & health in game manager
            theGameControllerScript.UpdatePlayerScore(powerupPoints);
            theGameControllerScript.UpdatePlayerHealth(100);

            string points = "WELL DONE! YOU GOT FULL HEALTH & " + powerupPoints.ToString() + " POINTS! ";
            
            if (bHitFirstTime)
            {
                theGameControllerScript.PostImportantStatusMessage(points); // display it
            }
            
            // update player health to full
            theGameControllerScript.UpdatePlayerHealth(100);
            theGameControllerScript.PlayCriticalCountdown(false); // may be running

            // Do tidy up - this resets spawn time in spawn controller
            PowerupExpired();
        }
    }
}
