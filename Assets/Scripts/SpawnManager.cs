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
    public GameObject[] PetrolCans;       // ammo refills

    // not used yet
    public GameObject[] PatrolLocations;  // array of objects set as patrol spots

    private Time[] powerupCreationTime;   // time each was first created

    private GameplayController theGameControllerScript;
    private GameplayController theGameManager;
    private GameObject thePlayer;

    // The times BELOW allow for FOUR spawn zones now (for enemies & powerups), drones will appear every 12s in zones 1 or 2 only
    // and other objects spawn every 12-16 seconds in each of the 4 zones
    private float droneSpawnInterval     = 9.0f;  // spawn a drone every 9 seconds (or 18s in zone you're in now)
    private float warriorSpawnInterval   = 4.0f;  // spawn warrior every 16 seconds (until maximum reached) in YOUR current zone
    private float powerPillSpawnInterval = 4.0f;  // spawn a powerup up every 16 seconds in your current zone

    // used for waves of enemies
    public int maxDronesPerSpawn       = 3;      // flying drones
    public int maxWarriorsPerSpawn     = 3;      // maximum generated at each spawn
    public int maxWarriorsOnScreen     = 60;     // max on screen
    public int currentWarriorsPerSpawn = 1;      // starts at one, increases with wave numbers to max of maxPerSpawn
    public int nSpawnAreas             = 4;      // number of spawn zones on this level
    public bool startedSpawning        = false;  // have we started spawning yet

    // used to select next spawn zone
    private int warriorSpawnZone       = 0;     // area for next enemy (zombie) spawn
    private int droneArea              = 0;     // area for next drone spawn (zone 1 or 2)
    private int powerUpSpawnZone       = 0;     // area for next powerup spawn

    private float ammoRefillInterval   = 0f;    // this is set by the game controller NOT this value here!
    private float timeLastSuperPowerup = 0f;    // time the last superpowerup was spawned
    private int   superPowerInterval   = 0;     // time interval to next super powerup spawn
    private int   superPowerExpiryTime = 2;     // time in minutes we have to collect it
    private int   maximumClips         = 0;     // total of ALL clips allowed (either HELD by Player or Spawned and not yet collected) at ANY time

    private bool bSpawnAmmoAllowed     = true;  // are we allowed to spawn ammo (set FALSE when maxAmmo held/spawned are in scene)
    private bool bFirstTimeMessage     = true;  // only display "no more ammo" message once spawn empty cycle, set false once shown
    private int  spawnedSoFar          = 0;     // number spawned (we only spawn upto max allowed MINUS clips held MINUS already spawned & not collected)

    // Sound stuff for Announcments
    private AudioSource theAudio;               // the audio source
    public AudioClip    theSuperClip;           // clip to play
    public AudioClip    messageClip;            // message audio (should move this to important status message routine)
    private AudioMixer  theMixer;               // the audio mixer to output sound from listener to
    private string      _outputMixer;           // holds mixer struct

    public void ResetSuperSpawnTime(float expiryCollectTime)
    {
        // reset time of last superpowerup spawn
        //
        // the time of the LAST spawn is reset by the SuperSpawnController to ensure even time spacing between
        // spawns and greatly reduces complexity here (resets it on collisionenter() and expiry)
        timeLastSuperPowerup = expiryCollectTime;
    }

    // Start is called before the first frame update
    void Start()
    {
        // find game controller for access later
        theGameManager = FindObjectOfType<GameplayController>();
        theGameControllerScript = theGameManager.GetComponent<GameplayController>();
        superPowerInterval = theGameControllerScript.GetSuperPowerupInterval(); // time after which we spawn another Super Powerup

        ammoRefillInterval = theGameControllerScript.GetAmmoRefillInterval();   // get time after which we spawn (or not) more ammo in scene
        maximumClips = theGameControllerScript.GetMaximumClips();         // TOTAL of all clips (held or can be spawned) ALLOWED in the scene

        // find audio components needed
        theAudio     = GetComponent<AudioSource>();
        theMixer     = Resources.Load("Music") as AudioMixer; // from created "Resources/Music/..." folder in heirarchy
        _outputMixer = ""; // holds mixer struct

        thePlayer    = GameObject.FindGameObjectWithTag("Player"); // the player character
        timeLastSuperPowerup = Time.realtimeSinceStartup;          // set creation time to current start time
        spawnedSoFar = theGameControllerScript.GetStartingClips(); // always include the starting ones held by Player
    }

    // Update is called once per frame
    void Update()
    {
        // Start routine to spawn objects if gameManager says game has started
        if (theGameManager.HasGameStarted() && !startedSpawning)
        {
            startedSpawning = true;

            // start to spawn enemies, drones and power ups at regular intervals
            // (SpawnPowerpill also does super powerups) & ammo refills
            InvokeRepeating("SpawnWarrior", 1.0f, warriorSpawnInterval);
            InvokeRepeating("SpawnDrone", 1.0f, droneSpawnInterval);
            InvokeRepeating("SpawnPowerPill", 1.0f, powerPillSpawnInterval);
            InvokeRepeating("SpawnAmmoRefill", ammoRefillInterval, ammoRefillInterval); // starting spawn in "refill interval" seconds from now, then repeat at refill interval
            
            //TESTING   InvokeRepeating("SpawnAmmoRefill", 5f, 30f); // in 30s start spawning and then every refill interval
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
                case 1: { nToSpawnNow = 1; break; }
                case 2: { nToSpawnNow = 2; break; }
                case 3: { nToSpawnNow = 3; break; }
                case 4: { nToSpawnNow = 4; break; }
                case 5: { nToSpawnNow = 5; break; }
                default: { nToSpawnNow = 5; break; }
            }

            int maxPerWave = theGameControllerScript.GetMaxEnemiesPerWave();
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
                                randomZ = Random.Range(-150f,  150f);
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
                    Vector3 randomSpawnPos = new Vector3(randomX, warriorToSpawn.transform.position.y, randomZ);
                    GameObject newWarrior;

                    newWarrior = Instantiate(warriorToSpawn, randomSpawnPos, Quaternion.identity);
                }
            }
        }
    }

    void SpawnDrone()
    {
        if (Random.Range(1, 10) >= 2)
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
        float randomX = 0f;            // spawn X Pos
        float randomZ = 0f;            // Spawn Z Pos

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
                    randomZ = Random.Range(0f, 175f);
                }

                if (powerUpSpawnZone == 2)
                {
                    // top left near Harland HQ / centre lake
                    randomX = Random.Range(-100f, -155f);
                    randomZ = Random.Range(  15f,  160f);
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
                    randomZ = Random.Range(-210f, 130f);
                }

                Vector3 randomSpawnPos = new Vector3(randomX, 2, randomZ);
                GameObject newPowerup;
                float timeSpawned = Time.realtimeSinceStartup;

                newPowerup = Instantiate(pillToSpawn, randomSpawnPos, Quaternion.identity); // spawn it
                theGameManager.SetPowerUpEntry(newPowerup, timeSpawned); // store object & time of creation in game manager
            }

            // Spawn a super powerup if time for next spawn (but allow it to be a little bit random
            // so not in same zones all the time (max 15 second gap, so could spawn in a different zone now (may work)

            float randomTime = Random.Range(0f, 0.25f);

            if ((Time.realtimeSinceStartup - timeLastSuperPowerup) / 60f >= (superPowerInterval + randomTime) && !theGameControllerScript.IsGamePaused())
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

        theAudio.clip = theSuperClip;
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

    public void SetAmmoSpawnAllowed()
    {
        // allow spawning of ammo to continue
        bSpawnAmmoAllowed = true; // ok to spawn
        bFirstTimeMessage = true; // display msg next time empty
        spawnedSoFar      = 1;    // one clip is spawned just after clip reload sequence
    }

    void SpawnAmmoRefill()
    {
        // Spawn an Ammo clip refill in the "FUEL" dump zone found in each zone (1-4 for now -not zero)
        if (bSpawnAmmoAllowed)
        {
            // Spawn a refill if not on pause
            if (!theGameControllerScript.IsGamePaused())
            {
                int currentClips  = theGameControllerScript.GetNumberOfClipsLeft(); // how many clips player currently holds
                int maxClips      = theGameControllerScript.GetMaximumClips();      // maximum number of clips that can be carried
                int possibleClips = maxClips - currentClips;                        // max we could potentially spawn this time
                int spawnZone     = Random.Range(0, theGameControllerScript.GetNumberOfSpawnZones()); // random spawn zone

                if (spawnedSoFar < maxClips)
                {
                    // we haven't yet spawned (or currently hold) the maximum allowed
                    
                    int toSpawn  = Random.Range(1, theGameControllerScript.GetMaxAmmoPerSpawn());
                   
                    // allow smaller spawn if near limit
                    if (toSpawn + spawnedSoFar > maxClips && spawnedSoFar < maxClips)
                    {
                        toSpawn = maxClips - spawnedSoFar;
                    }

                    if (toSpawn + spawnedSoFar <= maxClips)
                    {
                        // ok to spawn these, spawn the Ammo (Petrol Can(s)) in the "Fuel Zone" circle
                        // find the selected zone & its transform 
                        string zoneString = "Fuel Dump " + spawnZone.ToString();

                        GameObject dumpZone = GameObject.FindGameObjectWithTag(zoneString);
                        GameObject newFuel;
                        Vector3    spawnPos = dumpZone.transform.position;
                       
                        Debug.Log("Spawning " + toSpawn.ToString() + (toSpawn == 1 ? " refill" : " refills") + " in zone " + spawnZone + " at " + (Time.realtimeSinceStartup % 60) + " minutes(s) from startup.");

                        // position at centre of Fuel dump zone, and randomise a little to prevent (hopefully) new ones appearing on top
                        // if player doesn't collect them (Zone Width is approx 0.8 units in GUI), ammo has a rigidbody, so should just
                        // bump out of the way on screen (may fall on floor) if they do collide

                        for (int i=0; i < toSpawn; i++)
                        {
                            GameObject ammoRefill = PetrolCans[0]; // ammo "Petrol Can" object - may be more types later
                            float spawnX = spawnPos.x + ammoRefill.GetComponent<Renderer>().bounds.center.x + Random.Range(-3.5f, 3.5f);
                            float spawnY = 0f;
                            float spawnZ = spawnPos.z + ammoRefill.GetComponent<Renderer>().bounds.center.z + Random.Range(-3.5f, 3.5f);

                            // spawn it now
                            newFuel    = Instantiate(ammoRefill, new Vector3(spawnX, 0f, spawnZ), Quaternion.identity);
                        }

                        spawnedSoFar += toSpawn; // increment count

                        // play fanfare noise
                        StartCoroutine("PlayAmmoFanfare", toSpawn.ToString() + " x AMMO REFILL AVAILABLE IN ZONE " + spawnZone);
                    }
                    
                    if (spawnedSoFar >= maxClips)
                    {
                        // turn off spawning now as we have reached maximum - only reenable when we are empty of clips
                        bSpawnAmmoAllowed = false;

                        if (bFirstTimeMessage)
                        {
                         //   theGameControllerScript.PostImportantStatusMessage("NO MORE AMMO SPAWNED UNTIL ALL CURRENT IS USED!");
                            bFirstTimeMessage = false;

                            // play fanfare noise
                            StartCoroutine("PlayAmmoFanfare", "NO MORE AMMO NOW, UNTIL ALL HELD IS USED!");
                        }
                    }
                }
            }
        }
    }

    IEnumerator PlayAmmoFanfare(string theMessage)
    {
        // play fanfare noise
        _outputMixer = "No Change"; // set to normal levels
        GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];

        theAudio.clip = messageClip;
        theAudio.volume = 0.8f;
        theAudio.Play();

        yield return new WaitForSeconds(messageClip.length);

        // post an important message
        theGameControllerScript.PostImportantStatusMessage(theMessage);
    }
}

