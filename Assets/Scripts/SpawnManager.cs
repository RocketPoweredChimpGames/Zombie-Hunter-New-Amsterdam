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
    private float droneSpawnInterval     = 15.0f; // spawn a drone every 15 seconds
    private float warriorSpawnInterval   = 10.0f; // spawn a warrior every 10 seconds until we have a maximum number
    private float powerPillSpawnInterval = 8.0f;  // spawn a power up pill every 8 seconds

    // used for waves of enemies
    public int maxDronesPerSpawn       = 2;
    public int maxWarriorsPerSpawn     = 3;
    public int maxWarriorsOnScreen     = 50;
    public int currentWarriorsPerSpawn = 1; // starts at one, increases with wave numbers to max of maxPerSpawn

    public bool startedSpawning = false;

    // Start is called before the first frame update
    void Start()
    {
        // find game controller for access later
        theGameManager = FindObjectOfType<GameplayController>();
        theGameControllerScript = theGameManager.GetComponent<GameplayController>();

        thePlayer = GameObject.FindGameObjectWithTag("Player");         // player
        // old tried this in a container didnt work thePlayer!! = GameObject.FindGameObjectWithTag("Player Base Object"); // player
    }

   
    // Update is called once per frame
    void Update()
    {
        // Start routine to spawn objects if gameManager says game has started
        if (theGameManager.HasGameStarted() && !startedSpawning)
        {
            startedSpawning = true;

            // start to spawn enemies, drones and power ups at regular intervals
            InvokeRepeating("SpawnWarrior",   1.0f, warriorSpawnInterval);
            InvokeRepeating("SpawnDrone",     1.0f, droneSpawnInterval);
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

            //Debug.Log("Spawning " + nToSpawnNow + " zombies PER spawn function call on level " + nWaveNumber + ".");

            if (nToSpawnNow ==0)
            {
                // set to one
                nToSpawnNow = 1;
            }

            int maxPerWave = theGameControllerScript.GetMaxEnemiesPerWave();
            int numberOfEnemiesOnScreenNow = GameObject.FindGameObjectsWithTag("Enemy Warrior Base Object").Length;

            // check if spawning these would go over the maximum allowed number of enemies on screen
            if (numberOfEnemiesOnScreenNow <= maxPerWave - nToSpawnNow)
            {
                for (int iSpawn = 0; iSpawn < nToSpawnNow; iSpawn++)
                {
                    // start to spawn enemies, power ups at regular intervals
                    // spawn in different places too

                    int   spawnZone = Random.Range(0, 3);
                    float randomX   = 0f;
                    float randomZ   = 0f;

                    spawnZone = 1; // for test

                    if (spawnZone == 1)
                    {
                        // original spawn zone playfield
                        //randomX = Random.Range(  8f, 50f);
                        //randomZ = Random.Range(-58f, 55f);

                        // new big area
                        randomX = Random.Range(10f, 160f);
                        randomZ = Random.Range(0f, 175f);
                        Debug.Log("Spawning Warrior in Zone 1");
                    }
                    
                    if (spawnZone == 2)
                    {
                        // top left near smaller big building
                        randomX = Random.Range(7f, 30f);
                        randomZ = Random.Range(3f, 25f);
                        Debug.Log("Spawning Warrior in Zone 2");
                    }

                    if (spawnZone == 3)
                    {
                        // original spawn zone
                        randomX = Random.Range(-15f, 15f);
                        randomZ = Random.Range(-10f, 80f);
                        Debug.Log("Spawning Warrior in Zone 3");
                    }

                    if (spawnZone == 4)
                    {
                        // original spawn zone
                        randomX = Random.Range(-15f, 15f);
                        randomZ = Random.Range(-10f, 80f);
                        Debug.Log("Spawning Warrior in Zone 4");
                    }

                    Vector3    randomSpawnPos = new Vector3(randomX, warriorToSpawn.transform.position.y, randomZ);
                    GameObject newWarrior;

                    newWarrior = Instantiate(warriorToSpawn, randomSpawnPos, Quaternion.identity);
                }
            }
        }
    }


    void SpawnDrone()
    {
        // Spawns Drones in the air
        GameObject droneToSpawn = Drones[0];  // for testing only

        if (droneToSpawn != null)
        {
            // chose a random number of drones to spawn
            int nHowMany = Random.Range(1, maxDronesPerSpawn);

            for (int nDrone = 0; nDrone <nHowMany; nDrone++)
            {
                int spawnZone = Random.Range(1, 4);
                float randomX = 0f;
                float randomZ = 0f;

                spawnZone = 1; // for test

                switch (spawnZone)
                {
                    case 1:
                        {
                            // original spawn zone
                            randomX = Random.Range(10f, 160f);
                            randomZ = Random.Range(185f, 175f);
                            break;
                        }

                    case 2:
                        {
                            // original spawn zone
                            randomX = Random.Range(-15f, 15f);
                            randomZ = Random.Range(-10f, 80f);
                            break;
                        }

                    case 3:
                        {
                            // original spawn zone
                            randomX = Random.Range(-15f, 15f);
                            randomZ = Random.Range(-10f, 80f);
                            break;
                        }
                    case 4:
                        {
                            // original spawn zone
                            randomX = Random.Range(-15f, 15f);
                            randomZ = Random.Range(-10f, 80f);
                            break;
                        }
                }

                Vector3 randomSpawnPos = new Vector3(randomX, 8f + Random.Range(0f,6f), 115f +Random.Range(1f,10f));
                GameObject newDrone;

                // spawn it at the random height and position
                newDrone = Instantiate(droneToSpawn, randomSpawnPos, Quaternion.identity);

                // rotate drone to face right direction - doesn't work for some reason,
                // tried rotating the animators transform and the objects transform! bah!
                newDrone.GetComponent<Animator>().GetComponent<Rigidbody>().transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                // try this roate here?
                newDrone.transform.Rotate(Vector3.up, 180f);
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
                // weird positioning as doesn't relate to building positioning?
                float randomX = Random.Range(5f, 175f);
                float randomZ = Random.Range(-175f, 175f);
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

