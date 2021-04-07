using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using UnityEngine;

public class MissileController : MonoBehaviour
{
    private GameObject theDroneController;
    private GameObject theGameController;
    
    private DroneController theDroneControllerScript;
    private GameplayController theGameControllerScript;
    
    private Rigidbody missileRb;
    private Vector3 realGravity;
    private float gravityModifier = 1.0f;
    private float speed = 1.0f;

    public AudioClip missileExplosion;

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

        theDroneControllerScript = theDroneController.GetComponent<DroneController>();
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
            collision.gameObject.CompareTag("Boundary Top") ||
            collision.gameObject.CompareTag("Boundary Bottom"))
        {
            // missile has landed - play bomb explosion at current position
            if (transform.position.z > -80)
            {
                // ok to spawn another missile, so reset flag
                theDroneControllerScript.missileLaunched = false;
            }

            StartCoroutine(PlayingExplosion());
            gameObject.GetComponent<MeshRenderer>().enabled = false;

            // check if player within a certain range and if so popup hit by blast wave message and 
            // decrease points & 15% energy

        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            // collided with player
            theGameControllerScript.UpdatePlayerScore(-10);

            Debug.Log("BIG BOOM - YOU'RE DEAD!");
            
            // display direct hit message and knock off 35% energy of player

            // do some animation or effect on player position here
            StartCoroutine(PlayingExplosion());
        }
    }

    public GameObject theBombFlames;
    
    IEnumerator PlayingExplosion()
    {
        // play the sound and wait for it to finish
        GetComponent<AudioSource>().clip = missileExplosion;
        GetComponent<AudioSource>().playOnAwake = true;
        GetComponent<AudioSource>().volume = 0.3f;
        GetComponent<AudioSource>().Play(); // play explosion noise
        
        //  play particle effect
        if (theBombFlames != null)
        {
            theBombFlames.GetComponentInChildren<ParticleSystem>().Play();
        }
        
        // suspend deletiom for a bit
        yield return new WaitForSeconds(0.01f);

        //  set it dead if not destroyed already (added due to timing issues)
        if (theBombFlames != null)
        {
            Destroy(gameObject, theBombFlames.GetComponentInChildren<ParticleSystem>().duration);
        }
        else
        {
            Destroy(gameObject, 2f);
        }
    }
}
