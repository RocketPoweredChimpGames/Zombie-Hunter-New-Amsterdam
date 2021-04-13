using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.AI;


public class SpawnManager : MonoBehaviour
{
    // Arrays of Game Objects to Spawn: Powerups, Warriors (Zombies), & Drones, all set up in GUI Editor
    public GameObject[] Drones;           // air support
    public GameObject[] Warriors;         // ground troops
    public GameObject[] PowerPills;       // current powerup pills

    // not used yet
    public GameObject[] PatrolLocations;  // array of objects set as patrol spots

    private Time[] powerupCreationTime;   // time each was first created
    
    private GameplayController theGameControllerScript;
    private GameplayController theGameManager;
    private GameObject thePlayer;

    // ALL TIMES BELOW allow for two spawn zones now, i.e. a drone will appear every 12s in zone you are in
    
    // 5th Oct 2020 ALL TIMES BELOW allow for FOUR spawn zones now, i.e. a drone will appear every 12s in zone (1 or 2 only)
    // and other objects spawn every 12-16 seconds in each of the 4 zones
    private float droneSpawnInterval     = 6.0f;  // spawn a drone every 6 seconds (or 12s in zone you're in now)
    private float warriorSpawnInterval   = 4.0f;  // spawn warrior every 4 seconds until maximum (1 every 16 secs in YOUR current zone)
    private float powerPillSpawnInterval = 3.5f;  // spawn a power up every 14 seconds (in the zone YOU are currently in)

    // used for waves of enemies
    public int maxDronesPerSpawn       = 3;
    public int maxWarriorsPerSpawn     = 3;
    public int maxWarriorsOnScreen     = 60;
    public int currentWarriorsPerSpawn = 1;       // starts at one, increases with wave numbers to max of maxPerSpawn
    public int nSpawnAreas             = 4;       // number of spawn zones on this level
    public bool startedSpawning        = false;   // have we started spawning

    // used to select next spawn zone, currently only 2 zones each, but could be more (& different) later
    // so leaving these individual variables in for now!
    private int   warriorSpawnZone = 0; // area for next enemy (zombie) spawn
    private int   droneArea        = 0; // area for next drone spawn ( 1 or 2 currently
    private int   powerUpSpawnZone = 0; // area for next powerup spawn

    private float timeLastSuperPowerup;   // time the last superpowerup was spawned
    private int   superPowerInterval = 2; // spawn one every alternate 2 mins for testing
    private bool  superPowerupGap    = false;

    // Sound stuff for Super Powerup
    private AudioSource theAudio;
    public  AudioClip theSuperClip;


    // Start is called before the first frame update
    void Start()
    {
        // find game controller for access later
        theGameManager = FindObjectOfType<GameplayController>();
        theGameControllerScript = theGameManager.GetComponent<GameplayController>();
        theAudio = GetComponent<AudioSource>();
        
        thePlayer = GameObject.FindGameObjectWithTag("Player"); // the player
        timeLastSuperPowerup = Time.realtimeSinceStartup;     // set creation time to current start time
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

            if (warriorSpawnZone > nSpawnAreas)
            {
                warriorSpawnZone = 1; // set to original
            }

            // game stats stuff JUST for reference from game controller while coding
            // enemiesKilledThisWave = 0;  // how many killed on current wave
            // maxEnemiesPerWave = 50; // maximum per wave before starting next wave

            // check if spawning these would go over the maximum allowed number of enemies on screen
            if (numberOfEnemiesOnScreenNow <= (maxPerWave - nToSpawnNow)) 
            {
                for (int iSpawn = 0; iSpawn < nToSpawnNow; iSpawn++)
                {
                    // spawn enemies at random places (Unity uses 10x10 scale for 1 display unit on screen)
                    // screen positions in Editor relate to the ORIGINAL plane size x SCALE FACTOR the plane was scaled up by.
                    float randomX = 0f;
                    float randomZ = 0f;

                    switch (warriorSpawnZone)
                    {
                        case 1:
                            {
                                // main area at startup
                                randomX = Random.Range(10f, 160f);
                                randomZ = Random.Range(0f, 175f);
                                break;
                            }

                        case 2:
                            {
                                // top left near HQ building / left side of central lake 
                                randomX = Random.Range(-175f, -145f);
                                randomZ = Random.Range(-150f, 190f);
                                break;
                            }

                        case 3:
                            {
                                // main zone, bottom park area
                                randomX = Random.Range(-130f, 190f);
                                randomZ = Random.Range(-140f, -195f);
                                break;
                            }

                        case 4:
                            {
                                // sky platform - central area
                                randomX = Random.Range(450f, 570f);
                                randomZ = Random.Range(-170f, 55f);
                                break;
                            }

                        default: break;
                    }

                    // spawn it
                    Vector3    randomSpawnPos = new Vector3(randomX, warriorToSpawn.transform.position.y, randomZ);
                    GameObject newWarrior;

                    newWarrior = Instantiate(warriorToSpawn, randomSpawnPos, Quaternion.identity);
                }
            }
        }
    }

    void SpawnDrone()
    {
        if (Random.Range(1,10) >=2)
        {
            // don't spawn all the time
            // Spawns Drones in the air
            GameObject droneToSpawn = Drones[0];  // originlly for testing only - now only one type spawned

            if (droneToSpawn != null)
            {
                // chose a random number of drones to spawn
                int nHowMany = Random.Range(1, maxDronesPerSpawn);

                droneArea++; // increment drone spawn area

                if (droneArea > nSpawnAreas)
                {
                    // only 2 zones at present
                    droneArea = 1;
                }

                for (int nDrone = 0; nDrone < nHowMany; nDrone++)
                {
                    float randomX = 0f;
                    float randomZ = 0f;

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
                                randomZ = Random.Range(175f, 190f);
                                break;
                            }
                    }

                    Vector3 randomSpawnPos = new Vector3(randomX, 8f + Random.Range(0f, 6f), 115f + Random.Range(1f, 10f));
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
        
    }

    
    void SpawnPowerPill()
    {
        // Spawn a Powerup
        GameObject pillToSpawn = PowerPills[0]; // Ordinary Powerup
        float      randomX     = 0f;            // spawn X Pos
        float      randomZ     = 0f;            // Spawn Z Pos

        if (pillToSpawn != null)
        {
            // change zone to next zone
            powerUpSpawnZone++;

            if (powerUpSpawnZone > nSpawnAreas)
            {
                powerUpSpawnZone = 1; // reset to start zone
            }

            for (int i = 0; i < nSpawnAreas; i++)
            {
                if (powerUpSpawnZone == 1)
                {
                    // new large area in front of church
                    randomX = Random.Range(10f, 160f);
                    randomZ = Random.Range(0f, 175f);
                }

                if (powerUpSpawnZone == 2)
                {
                    // top left near Harland HQ / centre lake
                    randomX = Random.Range(-40f, -160f);
                    randomZ = Random.Range( 15f, 200f);
                }

                if (powerUpSpawnZone == 3)
                {
                    // main zone, bottom park area
                    randomX = Random.Range(-130f, 190f);
                    randomZ = Random.Range(-140f, -195f);
                }

                if (powerUpSpawnZone == 4)
                {
                    // sky platform - central area
                    randomX = Random.Range(450f, 570f);
                    randomZ = Random.Range(-170f, 55f);
                }

                Vector3 randomSpawnPos = new Vector3(randomX, 2, randomZ);
                GameObject newPowerup;
                float      timeSpawned = Time.realtimeSinceStartup;

                newPowerup = Instantiate(pillToSpawn, randomSpawnPos, Quaternion.identity); // spawn it
                theGameManager.SetPowerUpEntry(newPowerup, timeSpawned); // store object & time of creation in game manager
            }

            // Spawn a super powerup also if time for next spawn is due (but allow it to be a little bit random
            // so not in same zones all the time (max 15 second gap, so could spawn in a different zone now (may work)
            float randomTime = Random.Range(0f, 0.25f);

            if ( (Time.realtimeSinceStartup - timeLastSuperPowerup) / 60f >= (superPowerInterval + randomTime))
            {
                // interval has expired
                if (superPowerupGap == false)
                {
                    // spawn the super powerup in current spawn zone a little away from Powerup just spawned
                    GameObject superPowerup = Instantiate(PowerPills[1], new Vector3(randomX + 2f, 0.1f, randomZ - 2f), Quaternion.identity); // the Super Powerup Container object
                    theGameControllerScript.PostImportantStatusMessage("Super Powerup in Zone " + powerUpSpawnZone + " , " + " 4 mins to Collect!");
                    timeLastSuperPowerup = Time.realtimeSinceStartup;
                    superPowerupGap = true; // prevent spawn at next interval
                    
                    // play beep
                    theAudio.clip = theSuperClip;
                    theAudio.volume = 100;
                    theAudio.Play();
                }
                else
                {
                    // leave a gap till next spawn
                    timeLastSuperPowerup = Time.realtimeSinceStartup; // fake it
                    superPowerupGap = false; // allow it next time it expires
                }
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

