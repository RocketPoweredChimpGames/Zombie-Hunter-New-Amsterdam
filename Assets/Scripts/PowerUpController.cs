using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class PowerUpController : MonoBehaviour
{
    private GameObject theGameController;               // GameplayController object in scene
    private GameplayController theGameControllerScript; // the script attached to this object

    private float lastUpdateTime;     // time this was last updated
    private float startTime;          // time this first appeared on screen
    private float decayPeriod = 10.0f; // decays a bit every 10 secs
    private int decayCount = 0;       // number of times energy pill has decayed (deleted on maxDecay)
    private int powerUpPoints = 5;    // every power up has 5 points initially
    private int powerUpHealthPoints = 3;

    public TMP_Text statusDisplayField;
    public AudioClip powerBoing;
    
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

        lastUpdateTime = Time.realtimeSinceStartup; // start time we will increment later in update()
        startTime = lastUpdateTime;

        // set audio clip
        GetComponent<AudioSource>().playOnAwake = false;
        GetComponent<AudioSource>().clip = powerBoing;

        string blank = "";

        statusDisplayField = GameObject.FindGameObjectWithTag("Status Display").GetComponent<TMP_Text>();

        // find bonus health field
        statusDisplayField.text = blank.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        // only cycle powerup states if not paused
        if (!theGameControllerScript.IsGamePaused() || !theGameControllerScript.IsGameOver())
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
                    // destroy power pill
                    Destroy(gameObject);
                    break;
                }
        }

        if (decayCount == 5) decayCount = 0;
    }

    /*  private float lastUpdateTime;     // time this was last updated
        private float startTime;          // time this first appeared on screen
        private float decayPeriod = 5.0f; // decays a bit every 5 secs
        private int decayCount = 0;       // number of times energy pill has decayed (deleted on maxDecay)
        private int maxDecay = 5;         // maximum number of time periods*/

    bool timeHasPassed()
    {
        /*if (Time.realtimeSinceStartup >= lastUpdateTime + decayPeriod)
        {
            PowerUp matching = theGameControllerScript.FindPowerUp(gameObject);

            // if found it, check time from this object
            if (matching != null)
            {
                // found the object
                if (Time.realtimeSinceStartup >= matching.timeOfCreation)
                {
                    return true;
                }
            }
            lastUpdateTime = Time.realtimeSinceStartup;
            return true;
        }
        else
        {

            return false;
        }*/

        if (Time.realtimeSinceStartup >= lastUpdateTime + decayPeriod)
        {
            lastUpdateTime = Time.realtimeSinceStartup;
            return true;
        }
        else return false;
    }

    
    private void OnCollisionEnter(Collision collision)
    {
        // check who collided with us - if player update game manager with score        
        if (collision.gameObject.CompareTag("Player"))
        {
            GetComponent<AudioSource>().Play();

            // update score in game manager
            theGameControllerScript.UpdatePlayerScore(powerUpPoints);

            string points = powerUpPoints.ToString() + (powerUpPoints ==1 ? " Point!" : " Points!");

            statusDisplayField.text = points;
            int bonusHealth = 0;

            if (Random.Range(1f,20f) >= 17f)
            {
                // Player got a random bonus, at random
                bonusHealth = Random.Range(10, 20);
            }

            if (bonusHealth > 0)
            {
                // you got a bonus
                string bonus = bonusHealth.ToString();
                string blank = "Bonus Health Points! " + bonus +"%";
                
                // find bonus health field
                statusDisplayField.text = blank.ToString();
            }

            theGameControllerScript.UpdatePlayerHealth(powerUpHealthPoints + bonusHealth);
            bonusHealth = 0;
            Destroy(gameObject, 0.35f);
        }
    }
}
