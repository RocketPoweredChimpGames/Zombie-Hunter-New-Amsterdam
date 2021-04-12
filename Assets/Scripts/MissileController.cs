using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using UnityEngine;

public class MissileController : MonoBehaviour
{
    // game objects
    private GameObject         theDroneController;
    private GameObject         theGameController;
    public GameObject          theBombFlames;

    // scripts
    private DroneController    theDroneControllerScript;
    private GameplayController theGameControllerScript;
    
    private Rigidbody missileRb; // missile Rb
    private Vector3  realGravity;
    private float    gravityModifier = 1.0f;

    public AudioClip missileExplosion; // explosion sound

    // Start is called before the first frame update
    void Start()
    {
        // get players rigidbody component
        missileRb = GetComponent<Rigidbody>();

        // Set gravity up in Physics system
        realGravity = new UnityEngine.Vector3(0f, -9.8f, 0f); // downwards force 9.8m/s2
        Physics.gravity = realGravity * gravityModifier;
    
        theDroneController = GameObject.FindGameObjectWithTag("Enemy Drone");
        theGameController = GameObject.FindGameObjectWithTag("GameController");

        if (theDroneControllerScript)
        {
            theDroneControllerScript = theDroneController.GetComponent<DroneController>();
        }
        
        theGameControllerScript = theGameController.GetComponent<GameplayController>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 theGround = new Vector3(transform.position.x, 0.0f, -30f);
        
        if (Vector3.Distance(theGround, transform.position) > 0f)
        {
            // need to move towards the ground
            Vector3 direction = (theGround - transform.position).normalized;
            transform.Translate(direction * Time.deltaTime * 0.01f);  // *2f);
        }

        if (transform.position.y < -1)
        {
            // we overshot and missed somehow so tidyup
            Destroy(this.gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") ||
            collision.gameObject.CompareTag("Patio") ||
            collision.gameObject.CompareTag("Boundary Top") ||
            collision.gameObject.CompareTag("Boundary Bottom") ||
            collision.gameObject.CompareTag("Power Up") ||
            collision.gameObject.CompareTag("Enemy Warrior") ||
            collision.gameObject.CompareTag("Beut Tree") ||
            collision.gameObject.CompareTag("DanpungMix Tree") ||
            collision.gameObject.CompareTag("Jumok Tree") ||
            collision.gameObject.CompareTag("Newngsowha Tree") ||
            collision.gameObject.CompareTag("Neuti Tree") ||
            collision.gameObject.CompareTag("Sonamoo Tree") ||
            collision.gameObject.CompareTag("Black Car") ||
            collision.gameObject.CompareTag("Tocus") ||
            collision.gameObject.CompareTag("FOCE08") ||
            collision.gameObject.CompareTag("Table") ||
            collision.gameObject.CompareTag("Umbrella") ||
            collision.gameObject.CompareTag("Concrete") ||
            collision.gameObject.CompareTag("Ponds") ||
            collision.gameObject.CompareTag("Grassy Areas") ||
            collision.gameObject.CompareTag("Building") ||
            collision.gameObject.CompareTag("Skyscraper") ||
            collision.gameObject.CompareTag("Stone Fence") ||
            collision.gameObject.CompareTag("Dumpster") ||
            collision.gameObject.CompareTag("Ground Pole") ||
            collision.gameObject.CompareTag("Stop Sign") ||
            collision.gameObject.CompareTag("Mailbox") ||
            collision.gameObject.CompareTag("Fire Hydrant") ||
            collision.gameObject.CompareTag("Kiosk") ||
            collision.gameObject.CompareTag("Patio"))
        {
            // missile has landed (or hit end barriers/ landed on a powerup/ trees etc,
            // or even bounced on a zombies head - hahaha), so play bomb explosion at current position
            // and allow another bomb launch if not too far down screen

            if (transform.position.z > -110f)
            {
                // ok to spawn another missile, so reset flag
                if (theDroneControllerScript != null)
                {
                    theDroneControllerScript.missileLaunched = false;
                }
            }

            // play explosion effect
            StartCoroutine(PlayingExplosion());

            // disable renderer to hide object while playing explosion
            gameObject.GetComponent<MeshRenderer>().enabled = false;
        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            // collided with player - knock off points
            theGameControllerScript.UpdatePlayerScore(-20);

            // display direct hit message
            theGameControllerScript.PostStatusMessage("DIRECT HIT! LOSE 20 POINTS!");

            // do explosion animation
            StartCoroutine(PlayingExplosion());
        }
    }

    IEnumerator PlayingExplosion()
    {
        // play the explosion sound and wait for it to finish
        GetComponent<AudioSource>().clip        = missileExplosion;
        GetComponent<AudioSource>().playOnAwake = true;
        GetComponent<AudioSource>().volume      = 0.25f;
        GetComponent<AudioSource>().Play(); 
        
        //  play explosion particle effect
        if (theBombFlames != null)
        {
            theBombFlames.GetComponentInChildren<ParticleSystem>().Play();
        }
        
        GameObject thePlayer = theGameControllerScript.thePlayer;

        // Give Player damage if too close to explosion
        if (Vector3.Distance(thePlayer.transform.position, transform.position) < 15f)
        {
            // player within range to take blast wave damage
            theGameControllerScript.UpdatePlayerScore(-10);
            theGameControllerScript.PostStatusMessage("BLAST WAVE DAMAGE! LOSE 10 POINTS!");
        }

        // suspend deletiom for a bit
        yield return new WaitForSeconds(0.01f);

        //  set it dead if not destroyed already (added due to timing issues) at end of animation clip
        if (theBombFlames != null)
        {
            Destroy(gameObject, theBombFlames.GetComponentInChildren<ParticleSystem>().main.duration);
        }
        else
        {
            // ensure dead
            Destroy(gameObject, 2f);
        }
    }
}
