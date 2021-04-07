using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DroneController : MonoBehaviour
{
    public GameObject thePlayer; // our player
    private GameObject theGameController;  // game play controller
    private PlayerController thePlayerControllerScript;
    private GameplayController theGameControllerScript;

    private float droneSpeed = 0.25f;
    private float zBoundary = -110f; // bottom boundary of play area
    
    private bool bGameStarted = false; // game started or not
    public bool missileLaunched = false;

    public GameObject missileToLaunch; // missile object to launch

    // Start is called before the first frame update
    void Start()
    {
        thePlayer = GameObject.FindGameObjectWithTag("Player");         // player
        theGameController = GameObject.FindGameObjectWithTag("GameController"); // game controller 

        // get class scripts
        thePlayerControllerScript = thePlayer.GetComponent<PlayerController>();           // find the player controller
        theGameControllerScript = theGameController.GetComponent<GameplayController>(); // find the gameplay controller
        missileLaunched = false;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 currentPos = transform.position;

        if (currentPos.z <= zBoundary)
        {
            // destroy drone
            Destroy(gameObject);
        }
        else
        {
            // move drone on flight path, needs to bomb player if within a certain range of flight path
            Vector3 droneFlightPath = new Vector3(currentPos.x, currentPos.y, -115f);
            Vector3 direction = droneFlightPath - transform.position;
            transform.Translate(direction * Time.deltaTime * droneSpeed);

            // drop a bomb (falls under gravity)
            if (!missileLaunched)
            {
                StartCoroutine(DropABomb());
                missileLaunched = true; // destroys on hitting ground or barriers
            }
        }
    }
    IEnumerator DropABomb()
    {
        yield return new WaitForSeconds(1.5f + Random.Range(0f,1.5f));
            
        // after 1.5s to 3 secs, we start to drop another bomb
        float xPos = transform.position.x;
        float zPos = transform.position.z;

        Vector3 spawnPos = new Vector3(xPos, transform.position.y -0.5f, zPos);

        // set its forward velocity 
        GameObject theMissile = Instantiate(missileToLaunch, spawnPos, Quaternion.identity);
        Rigidbody missileRb = theMissile.GetComponent<Rigidbody>();
        missileRb.velocity = transform.TransformDirection(Vector3.back * 4);
    }
}