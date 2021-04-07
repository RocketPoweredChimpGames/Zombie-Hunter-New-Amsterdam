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

    // testing only - private vars later             ALL TIMES BELOW allow for two zones now!
    private float droneSpawnInterval     = 6.0f;  // spawn a drone every 6 seconds (or 12s in zone you're in now)
    private float warriorSpawnInterval   = 6.0f;  // spawn a warrior every 6 seconds until we have a maximum number
    private float powerPillSpawnInterval = 5.0f;  // spawn a power up pill every 10 seconds

    // used for waves of enemies
    public int maxDronesPerSpawn       = 3;
    public int maxWarriorsPerSpawn     = 3;
    public int maxWarriorsOnScreen     = 60;
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
            InvokeRepeating("SpawnWarrior",   1.0f, warriorSpawnInterval);
            InvokeRepeating("SpawnDrone",     1.0f, droneSpawnInterval);
            InvokeRepeating("SpawnPowerPill", 1.0f, powerPillSpawnInterval);
        }
    }

    int warriorSpawnZone = 0;
    
    // Start Spawning
    void SpawnWarrior()
    {
        GameObject warriorToSpawn = Warriors[0];  // for testing only
        
        int nWaveNumber = theGameControllerScript.GetWaveNumber(); // find out wave number

        if (warriorToSpawn != null)
        {
            // calculate a different number to spawn depending on wave number using MOD function
            int nToSpawnNow = nWaveNumber;

            //Debug.Log("Spawning " + nToSpawnNow + " zombies PER spawn function call on level " + nWaveNumber + ".");
            
            switch (nWaveNumber)
            {
                case 1:  { nToSpawnNow =1; break;}
                case 2:  { nToSpawnNow =2; break;}
                case 3:  { nToSpawnNow =3; break;}
                case 4:  { nToSpawnNow =4; break;}
                case 5:  { nToSpawnNow =5; break;}
                default: { nToSpawnNow =5; break;}
            }

            int maxPerWave                 = theGameControllerScript.GetMaxEnemiesPerWave();
            int numberOfEnemiesOnScreenNow = GameObject.FindGameObjectsWithTag("Enemy Warrior Base Object").Length;

            warriorSpawnZone++; // increment zone

            if (warriorSpawnZone > 2)
            {
                warriorSpawnZone = 1; // set to original
            }

            // check if spawning these would go over the maximum allowed number of enemies on screen
            if (numberOfEnemiesOnScreenNow <= maxPerWave - nToSpawnNow)
            {
                for (int iSpawn = 0; iSpawn < nToSpawnNow; iSpawn++)
                {
                    // start to spawn enemies, power ups at regular intervals
                    // spawn in different places too

                    float randomX   = 0f;
                    float randomZ   = 0f;

                    if (warriorSpawnZone == 1)
                    {
                        // original spawn zone playfield
                        // randomX = Random.Range(  8f, 50f);
                        // randomZ = Random.Range(-58f, 55f);

                        // new bigger area by original player site
                        randomX = Random.Range(10f, 160f);
                        randomZ = Random.Range(0f,  175f);
                        Debug.Log("Spawning Warrior in Zone 1");
                    }
                    
                    if (warriorSpawnZone == 2)
                    {
                        // top left near smaller posh big building / down side of lake 
                        // Don't forget Unity uses 10x10 scale for 1 unit!
                        // so screen positions in Editor relate to original x SCALE FACTOR used on original 10x10 unit plane.
                        randomX = Random.Range(-175f, -145f);
                        randomZ = Random.Range(-150f,  190f);

                        //Debug.Log("Spawning Warrior in Zone 2");
                    }

                    Vector3    randomSpawnPos = new Vector3(randomX, warriorToSpawn.transform.position.y, randomZ);
                    GameObject newWarrior;

                    newWarrior = Instantiate(warriorToSpawn, randomSpawnPos, Quaternion.identity);
                }
            }
        }
    }

    int droneArea = 0; // area for next drone spawn

    void SpawnDrone()
    {
        // Spawns Drones in the air
        GameObject droneToSpawn = Drones[0];  // for testing only

        if (droneToSpawn != null)
        {
            // chose a random number of drones to spawn
            int nHowMany = Random.Range(1, maxDronesPerSpawn);

            droneArea++; // increment drone spawn area

            for (int nDrone = 0; nDrone <nHowMany; nDrone++)
            {
                float randomX = 0f;
                float randomZ = 0f;

                if (droneArea >2)
                {
                    // only 2 zones at present
                    droneArea = 1;
                }

                switch (droneArea)
                {
                    case 1:
                    {
                        // original spawn zone
                        randomX = Random.Range(10f, 160f);
                        randomZ = Random.Range(175f, 190f);
                        break;
                    }

                    case 2:
                    {
                        // second spawn zone - whole area on left side
                        randomX = Random.Range(-40f, -160f);
                        randomZ = Random.Range( 175f, 190f);
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

    int powerUpSpawnZone = 0; // area for next powerup spawn
    void SpawnPowerPill()
    {
        // Spawn a Powerup
        GameObject pillToSpawn = PowerPills[0];  // for testing only

        if (pillToSpawn != null)
        {
            powerUpSpawnZone++;

            if (powerUpSpawnZone > 2)
            {
                powerUpSpawnZone = 1; // reset to original zone
            }

            float randomX = 0f;
            float randomZ = 0f;

            for (int i = 0; i < 2; i++)
            {
                if (powerUpSpawnZone == 1)
                {
                    // original spawn zone playfield
                    //randomX = Random.Range(  8f, 50f);
                    //randomZ = Random.Range(-58f, 55f);

                    // new bigger area by original player site
                    randomX = Random.Range(10f, 160f);
                    randomZ = Random.Range(0f, 175f);
                }
                if (powerUpSpawnZone == 2)
                {
                    // top left near smaller posh big building / lake
                    randomX = Random.Range(-40f, -160f);
                    randomZ = Random.Range( 15f, 200f);
                }
                
                Vector3    randomSpawnPos = new Vector3(randomX, 2, randomZ);
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

