using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.AI;


public class SpawnManager : MonoBehaviour
{
    // Arrays of Game Objects to Spawn, Power Pills, Warriors, Drones etc
    public GameObject[] Drones;           // air support
    public GameObject[] Warriors;         // ground troops
    public GameObject[] PowerPills;       // current powerup pills
    public GameObject[] PatrolLocations;  // array of objects set as patrol spots
    private Time[] powerupCreationTime;   // time each was first created
    private GameplayController theGameControllerScript;
    private GameplayController theGameManager;
    private GameObject thePlayer;

    // testing only - private vars later
    private float droneSpawnInterval = 12.0f; // spawn a drone every 30 seconds
    private float warriorSpawnInterval = 10.0f; // spawn a warrior every 10 seconds until we have a maximum number
    private float powerPillSpawnInterval = 8.0f; // spawn a power up pill every 8 seconds

    // used for waves of enemies
    public int maxDronesPerSpawn = 3;
    public int maxWarriorsPerSpawn = 3;
    public int maxWarriorsOnScreen = 20;
    public int currentWarriorsPerSpawn = 1; // starts at one, increases with wave numbers to max of maxPerSpawn

    public bool startedSpawning = false;

    // Start is called before the first frame update
    void Start()
    {
        // find game controller for access later
        theGameManager = FindObjectOfType<GameplayController>();
        theGameControllerScript = theGameManager.GetComponent<GameplayController>();

        thePlayer = GameObject.FindGameObjectWithTag("Player");         // player
    }

   
    // Update is called once per frame
    void Update()
    {
        // Start routine to spawn objects if gameManager says game has started
        if (theGameManager.HasGameStarted() && !startedSpawning)
        {
            startedSpawning = true;

            // start to spawn enemies, drones and power ups at regular intervals
            InvokeRepeating("SpawnWarrior", 1.0f, warriorSpawnInterval);
            InvokeRepeating("SpawnDrone", 1.0f, droneSpawnInterval);
            InvokeRepeating("SpawnPowerPill", 1.0f, powerPillSpawnInterval);
        }
    }


    // Start Spawning
    void SpawnWarrior()
    {
        GameObject warriorToSpawn = Warriors[0];  // for testing only
        
        int nWaveNumber = theGameControllerScript.GetWaveNumber(); // find out wave number

        if (warriorToSpawn != null)
        {
            // calculate a different number to spawn depending on wave number using MOD function
            int nToSpawnNow = nWaveNumber % maxWarriorsPerSpawn; // gets remainder (between 0 and maxWarriorsPerSpawn-1)

            Debug.Log("Spawning " + nToSpawnNow + " zombies PER spawn function call on level " + nWaveNumber + ".");

            if (nToSpawnNow ==0)
            {
                // set to one
                nToSpawnNow = 1;
            }

            int maxPerWave = theGameControllerScript.GetMaxEnemiesPerWave();
            int numberOfEnemiesOnScreenNow = GameObject.FindGameObjectsWithTag("Enemy Warrior").Length;

            // check if spawning these would go over the maximum allowed number of enemies on screen
            if (numberOfEnemiesOnScreenNow <= maxPerWave - nToSpawnNow)
            {
                for (int iSpawn = 0; iSpawn < nToSpawnNow; iSpawn++)
                {
                    // start to spawn enemies, power ups at regular intervals
                    float randomX = Random.Range(-15f, 15f);
                    float randomZ = Random.Range(-10f, 80f);
                    Vector3 randomSpawnPos = new Vector3(randomX, 0.0f, randomZ);

                    GameObject newWarrior;

                    newWarrior = Instantiate(warriorToSpawn, randomSpawnPos, Quaternion.identity);
                }
            }
        }
    }


    void SpawnDrone()
    {
        // Spawn a drone in the air
        GameObject droneToSpawn = Drones[0];  // for testing only

        if (droneToSpawn != null)
        {
            // chose a random number of drones to spawn
            int nHowMany = Random.Range(1, maxDronesPerSpawn);

            for (int nDrone =0; nDrone <nHowMany; nDrone++)
            {
                float randomX = Random.Range(-30f, 30f);
                Vector3 randomSpawnPos = new Vector3(randomX, 5f + Random.Range(0f,6f), 115f +Random.Range(1f,10f));
                GameObject newDrone;
                // create it at a random height and position
                newDrone = Instantiate(droneToSpawn, randomSpawnPos, Quaternion.identity);

                // rotate drone to face right direction - doesn't work for some reason,
                // tried rotating the animators transform and the objects transform! bah!
                //newDrone.GetComponent<Animator>().GetComponent<Rigidbody>().transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }
        }
    }


    void SpawnPowerPill()
    {
        // Spawn a Powerup
        GameObject pillToSpawn = PowerPills[0];  // for testing only

        if (pillToSpawn != null)
        {
            for (int i = 0; i < 2; i++)
            {
                float randomX = Random.Range(-30f, 30f);
                float randomZ = Random.Range(-60f, 90f);
                Vector3 randomSpawnPos = new Vector3(randomX, 2, randomZ);
                GameObject newPowerup;
                float timeSpawned = Time.realtimeSinceStartup;

                newPowerup = Instantiate(pillToSpawn, randomSpawnPos, Quaternion.identity); // spawn it
                theGameManager.SetPowerUpEntry(newPowerup, timeSpawned); // store object & time of creation in game manager
            }
        }

    }


    // Returns a currently unallocated patrol location
    UnityEngine.Vector3 getPatrolLocation()
    {
        // search patrol array to find a free one
        return new Vector3(0f, 0f, 0f); // test
    }
}

