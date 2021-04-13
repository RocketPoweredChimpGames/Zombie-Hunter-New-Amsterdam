using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Audio;
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
    private float droneSpawnInterval     = 9.0f;  // spawn a drone every 9 seconds (or 18s in zone you're in now)
    private float warriorSpawnInterval   = 4.0f;  // spawn warrior every 16 seconds until maximum in YOUR current zone
    private float powerPillSpawnInterval = 4.0f;  // spawn a power up every 16 seconds in your current zone

    // used for waves of enemies
    public int maxDronesPerSpawn       = 3;
    public int maxWarriorsPerSpawn     = 3;
    public int maxWarriorsOnScreen     = 60;
    public int currentWarriorsPerSpawn = 1;       // starts at one, increases with wave numbers to max of maxPerSpawn
    public int nSpawnAreas             = 4;       // number of spawn zones on this level
    public bool startedSpawning        = false;   // have we started spawning

    // used to select next spawn zone, currently only 2 zones each, but could be more (& different) later
    // so leaving these individual variables in for now!
    private int   warriorSpawnZone = 0;           // area for next enemy (zombie) spawn
    private int   droneArea        = 0;           // area for next drone spawn ( 1 or 2 currently
    private int   powerUpSpawnZone = 0;           // area for next powerup spawn

    private float timeLastSuperPowerup;           // time the last superpowerup was spawned
    private int   superPowerInterval   = 0;       // time interval to next super powerup spawn
    private int   superPowerExpiryTime = 2;       // time we have to collect it (mins)

    // Sound stuff for Super Powerup Announcment
    private AudioSource theAudio;                 // the audio source
    public AudioClip    theSuperClip;             // clip to play
    private AudioMixer  theMixer;                 // the audio mixer to output sound from listener to
    private string      _outputMixer;             // holds mixer struct
    

    public void ResetSuperSpawnTime( float expiryCollectTime)
    {
        // resets time of last superpowerup spawn
        //
        // the time of the LAST spawn is reset by the SuperSpawnController to ensure even time spacing between
        // spawns and greatly reduces complexity here
        timeLastSuperPowerup = expiryCollectTime;
    }

    // Start is called before the first frame update
    void Start()
    {
        // find game controller for access later
        theGameManager          = FindObjectOfType<GameplayController>();
        theGameControllerScript = theGameManager.GetComponent<GameplayController>();
        superPowerInterval      = theGameControllerScript.GetSuperPowerupInterval(); // time after which we spawn another Super Powerup

        // find audio components needed
        theAudio                = GetComponent<AudioSource>();
        theMixer                = Resources.Load("Music") as AudioMixer; // from created "Resources/Music/..." folder in heirarchy
        _outputMixer            = ""; // holds mixer struct

        thePlayer               = GameObject.FindGameObjectWithTag("Player"); // the player
        timeLastSuperPowerup    = Time.realtimeSinceStartup;    // set creation time to current start time
    }

    // Update is called once per frame
    void Update()
    {
        // Start routine to spawn objects if gameManager says game has started
        if (theGameManager.HasGameStarted() && !startedSpawning)
        {
            startedSpawning = true;

            // start to spawn enemies, drones and power ups at regular intervals
            // (SpawnPowerpill also does super powerups)
            InvokeRepeating("SpawnWarrior",   1.0f, warriorSpawnInterval);
            InvokeRepeating("SpawnDrone",     1.0f, droneSpawnInterval);
            InvokeRepeating("SpawnPowerPill", 1.0f, powerPillSpawnInterval);
        }
    }

    // Start Spawning
    void SpawnWarrior()
    {
        GameObject warriorToSpawn = Warriors[0];  // only one warrior type for now
        
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
            // don't spawn all the time, and always spawns Drones in the air
            GameObject droneToSpawn = Drones[0];  // only one type spawned for now

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
        // Spawn a Powerup (and regularly a Super Powerup)

        GameObject pillToSpawn = PowerPills[0]; // an ordinary Powerup
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
                    randomX = Random.Range(13f, 155f);
                    randomZ = Random.Range(0f,  175f);
                }

                if (powerUpSpawnZone == 2)
                {
                    // top left near Harland HQ / centre lake
                    randomX = Random.Range(-45f, -155f);
                    randomZ = Random.Range( 15f, 200f);
                }

                if (powerUpSpawnZone == 3)
                {
                    // main zone, bottom park area
                    randomX = Random.Range(-130f, 190f);
                    randomZ = Random.Range(-143f, -195f);
                }

                if (powerUpSpawnZone == 4)
                {
                    // sky platform - central area
                    randomX = Random.Range(450f, 570f);
                    randomZ = Random.Range(-210f,130f);
                }

                Vector3 randomSpawnPos = new Vector3(randomX, 2, randomZ);
                GameObject newPowerup;
                float      timeSpawned = Time.realtimeSinceStartup;

                newPowerup = Instantiate(pillToSpawn, randomSpawnPos, Quaternion.identity); // spawn it
                theGameManager.SetPowerUpEntry(newPowerup, timeSpawned); // store object & time of creation in game manager
            }

            // Spawn a super powerup if time for next spawn (but allow it to be a little bit random
            // so not in same zones all the time (max 15 second gap, so could spawn in a different zone now (may work)

            float randomTime = Random.Range(0f, 0.25f);

            if ( (Time.realtimeSinceStartup - timeLastSuperPowerup) / 60f >= (superPowerInterval + randomTime) && !theGameControllerScript.IsGamePaused())
            {
                // Spawn the Super Powerup in current spawn zone a little bit away from Powerup just spawned
                GameObject superPowerup = Instantiate(PowerPills[1], new Vector3(randomX + 2f, 0.1f, randomZ - 2f), Quaternion.identity); // the Super Powerup Container object
                theGameControllerScript.PostImportantStatusMessage("SUPER POWERUP IN ZONE " + powerUpSpawnZone + ", YOU HAVE " + superPowerExpiryTime + " MINS TO COLLECT IT!");
                timeLastSuperPowerup = Time.realtimeSinceStartup;

                // play fanfare noise
                StartCoroutine("PlaySuperPowerupFanfare");
            }
        }
    }
    
    IEnumerator PlaySuperPowerupFanfare()
    {
        // play fanfare noise
        _outputMixer = "No Change"; // set to normal levels
        GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];

        theAudio.clip   = theSuperClip;
        theAudio.volume = 0.8f;
        theAudio.Play();

        yield return new WaitForSeconds(theSuperClip.length);
    }


    // Returns a currently unallocated patrol location
    UnityEngine.Vector3 getPatrolLocation()
    {
        // search patrol array to find a free one
        return new Vector3(0f, 0f, 0f); // test
    }
}

