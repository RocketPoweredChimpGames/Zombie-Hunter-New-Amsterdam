using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Analytics;
using UnityEngine.UI;

public class PowerUp
{
    // class used to store PowerUp object and creation time
    // for use in a List<> here in gameplay controller (in later dev)
    public PowerUp( GameObject gObj, float tTime)
    {
        thePowerUp = gObj;       // the game object on screen
        timeOfCreation = tTime;  // time first created on screen
        currentlySought = false; // is this object being currently sought by an enemy
    }

    public GameObject thePowerUp { get; set; }
    public float timeOfCreation { get; set; }
    public bool currentlySought { get; set; }
}

public class GameplayController : MonoBehaviour
{
    // Public Variables
    public GameObject[] patrolPositions;        // patrol objects in scene which our warriors patrol to  - change to a list sometime?
    public GameObject[] theObstacles;           // dynamic array of obstacles (not used now) - delete later
    public GameObject   thePlayer;              // our player
    public Transform    theStartPosition;       // for repositioning player on restart (if i get it working)

    public GameObject   theScoringPanel;        // displays player scores, player start/re-start game
    public GameObject   theInstructionPanel;    // displayed prior to game start with instructions & animations of alien and player
    public GameObject   theCreditsReplayPanel;  // credits and replay panel at end of game
    public GameObject   theHighScoresPanel;     // top ten high scores panel
    private GameObject  theGameExitPanel = null;// game exit control panel
    private GameObject  theCrosshairs;          // target for flame / aiming

    private HighScoreTableController   theHighScoresControllerScript; // high scores controller script
    //private InstructionPanelController theInstructionPanelScript;     // instruction panel controller script

    // all noises are setup by dragging into relevant fields in the Unity editor
    public  AudioClip   countdownNoise;         // played when less than 60% (or whatever changed to) health
    public  AudioClip   criticalCountdownNoise; // played when 10% or under health 
    public  AudioClip   lifeLostNoise;          // life lost noise
    public  AudioClip   youReallyWannaGo;       // quit game ask
    public  AudioClip   goodbye;                // quit game confirmed
    public  AudioClip   zoneChanged;            // zone changed sound

    // spoken audio clips
    public AudioClip    gameOver;               // 'game over' voice
    public AudioClip    levelComplete;          // 'level completed' voice
    public AudioClip    winner;                 // 'winner' voice
    public AudioClip    hereWeGo;               // here we go game start voice
    public AudioClip    loseALifeVoice;         // 'lose a life' voice
    public AudioClip    thatsNotGonnaDoIt;      // not in high score or zero score voice
    public AudioClip    the321Voice;            // gun reload countdown at end of sequence

    // audio components etc
    private AudioSource theAudioSource;         // audio source component
    private AudioMixer  theMixer;               // the audio mixer to output sound from listener to
    private string      _outputMixer;           // holds mixer struct

    // all TMP text fields are associated by dragging field entries into gameplay controller entries in the Unity GUI editor
    public TMP_Text     ScorePlayer;            // players score
    public TMP_Text     LivesPlayer;            // players lives
    public TMP_Text     PlayerHealth;           // players health
    public TMP_Text     StatusDisplay;          // 'general' status display message box
    public TMP_Text     ImportantStatusDisplay; // 'IMPORTANT' status display message box (for Level Starts/Game Over/Super Powerup Msgs Only)
    public TMP_Text     PlayersCurrentZone;     // players current zone
    public TMP_Text     EnemyWaveNum;           // enemy wave number
    public TMP_Text     EnemiesRemaining;       // remaining enemies this wave
    public TMP_Text     EnemiesKilledTotal;     // total enemies killed in game
    public TMP_Text     HighScore;              // player high score
    public TMP_Text     ClipsLeft;              // number of ammo clips left
    public TMP_Text     ShotsLeftText;          // dual function displays "SHOTS" & "TIME" when all shots/clips shots used up
    public TMP_Text     CountReload;            // dual function shots left & time to reload when expired

    private List<PowerUp> currentPowerups;      // current powerups on screen for grand finale destruction sequence
    private SpawnManager theSpawnManager;       // the spawn manager

    private UnityEngine.Vector3[] originalStartPosition; // position where our 'Zombie' objects were originally spawned (not used yet)
    
    // game control 
    public  bool bGameStarted       = false;  // has game started
    private bool bGameReStarted     = false;  // has game been restarted (after end game)
    public  bool bGamePaused        = false;  // is game on pause
    public  bool bGameOver          = false;  // is game over (prevent further user inputs)
    public bool  bStartZoneUpdate   = false;  // prevent zone update till game starts or restarts
    public  bool playingCountdown   = false;  // are we playing countdown noise
    private int  enemySpeedSetting  = 2;      // set by Player during game to get enemies speed to go at (0- slow, 1-medium, 2-normal)
    
    // game stats stuff
    public int   enemyWaveNumber       = 0;   // current wave number
    private int  totalEnemiesKilled    = 0;   // total killed
    private int  enemiesKilledThisWave = 0;   // how many killed on current wave
    private int  maxEnemiesPerWave     = 50;  // maximum per wave before starting next wave

    // player variables
    private int  maxPowerUps         = 200;   // maximum powerups on screen at a time
    private int  playerLives         = 3;     // number of player lives
    public  int  playerHealth        = 100;   // initial full health
    private int  playerScore         = 0;     // initial player score
    private int  highScore           = 0;     // put in a file later to keep
    private int  playersCurrentZone  = 0;     // starting zone (always starts in zone 0) and and also is zone when last checked
    private int  currentZone         = 0;     // current zone player is in (at THIS check time)
    public bool  playerJustDied      = false; // set to true if player just died to avoid new hits for a few seconds

    // shot counters
    private int  startingClips       = 4;     // initial number of clips (may come from a file if player can buy stuff later in dev)
    private int  clipsLeft           = 4;     // clips remaining
    private int  shotsInAClip        = 25;    // total shots in a clip   (may vary if player buys bigger clips later in dev)
    private int  shotsLeftThisClip   = 25;    // shots left in current clip  

    // Super Powerup points / expiry
    private int  superPowerupPoints   = 500;  // points for collecting a super powerup
    private int  superPowerupInterval = 6;    // 6 minutes to next super powerup spawn

    // sky box used at start
    public Material theDaySkybox; // daytime sky box

    private void UpdatePlayerZonePosition()
    {
        // called every second to check players current zone for display update
        if (thePlayer !=null && !IsGameOver() || IsGamePaused())
        {
            // check position
            float currentX    = thePlayer.gameObject.transform.position.x;
            float currentZ    = thePlayer.gameObject.transform.position.z;

            if ( (currentX >= -210f && currentX <= 208f) && (currentZ <0f && currentZ >-140f))
            {
                // zone 0 - no spawning here
                currentZone = 0;
            }
            
            if ((currentX >= 0f && currentX <= 208f) && (currentZ >= 0f && currentZ <=210f))
            {
                // zone 1 - Main church area
                currentZone = 1;
            }
            
            if ((currentX <= 0.1f && currentX >= -210f) && (currentZ >= 0.05f && currentZ <= 210f))
            {
                // zone 2 - Harland HQ
                currentZone = 2;
            }

            if ((currentX >= -210f && currentX <= 205f) && (currentZ <= -140.1f && currentZ >= -210f))
            {
                // zone 3 - Bottom Lawn
                currentZone = 3;
            }
            
            if ((currentX >= 210f && currentX <= 650f) && (currentZ >= -260f && currentZ <= 200f))
            {
                // zone 4 - Sky Platform
                currentZone = 4;
            }

            if (currentZone != playersCurrentZone)
            {
                // reset zone and beep
                PlayersCurrentZone.SetText(currentZone.ToString());
                playersCurrentZone = currentZone;

                // play bing bong noise
                _outputMixer = "No Change"; // set to normal levels
                GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];

                theAudioSource.clip = zoneChanged;
                theAudioSource.volume = 0.4f;
                theAudioSource.time = 0f;
                theAudioSource.Play();
            }
        }
    }

    public int GetSuperPowerupPoints()
    {
        // returns points awarded for collecting a super powerup
        return superPowerupPoints;
    }
    public int GetSuperPowerupInterval() 
    {
        return superPowerupInterval;
    }

    public int GetStartingClips()
    {
        // returns start number of clips player has
        return startingClips;
    }

    public int GetNumberOfClipsLeft()
    {
        // returns number of clips player has left
        return clipsLeft;
    }

    public int GetShotsInMagazine()
    {
        // returns number of shots left in current clip
        return shotsLeftThisClip;
    }

    public void ShotFired()
    {
        // reduces number in current clip by one, which will reduce number of clips as required
        // which will then update display later on in dev
    }

    public bool HasPlayerJustDied()
    {
        // tell enemy controller that player just died, used to prevent adding damage to new player
        // for a little bit
        return playerJustDied;
    }

    IEnumerator PreventHitsTimer()
    {
        // delay for 5 seconds to give player time to escape
        yield return new WaitForSeconds(5f);
        playerJustDied = false;
    }

    public void ShowHighScores()
    {
        // get highscore controller to show high scores
        theHighScoresControllerScript.ShowHighScoresPanel(true);
    }
           
    public bool HasGameStarted()
    {
        // returns whether game has started or not
        return bGameStarted;
    }

    public bool HasGameRestarted()
    {
        // return whether game has restarted
        return bGameReStarted;
    }

    // StartGame() ALWAYS starts the countdown, the Coroutine is always running (and checks this flag), 
    // end game sets flag to stop countdown decaying health, so initial value must always be set to TRUE here!
    public bool bHealthCountdownPaused = true; // flag checked by HealthCountdown Coroutine periodically

    public void SetHealthCountdownPaused(bool bStart)
    {
        // flag checked by health decay coroutine every few seconds
        bHealthCountdownPaused = bStart;
    }

    public bool IsHealthCountdownPaused()
    {
        // returns status flag - true if it is currently paused
        return bHealthCountdownPaused;
    }

    public void StartGame(bool bStart)
    {
        if (bStart)
        {
            // starts Spawnmanager spawning on next update(), and also
            // enables Player controls in player controller
            
            // set with high score read from PlayerPrefs
            highScore = theHighScoresControllerScript.GetHighScore();
            HighScore.SetText(highScore.ToString());

            // reset to initial "25" shots & reset display text
            CountReload.SetText(shotsInAClip.ToString()); // reset shots left in clip display
            ShotsLeftText.SetText("SHOTS".ToString()); // dual purpose field ("SHOTS" and "TIME") display

            // set clips display object to initial "4"
            ClipsLeft.SetText(startingClips.ToString());

            // starts (or restarts) routine which periodically decays health (every 3s)
            if (IsHealthCountdownPaused())
            {
                // countdown currently paused (must always be paused at 1st start/ and at restart situation), so enable it again
                SetHealthCountdownPaused(false);
                thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(false);
            }

            // play 'here we go' game start voice
            AudioListener.pause  = false; // re-enable audio (disabled at end of a previous game)

            // increase clip vol by 5db
            _outputMixer  = "Voice Up 5db"; // group to output the audio listener to
            GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];
            theAudioSource.clip   = hereWeGo;
            theAudioSource.PlayOneShot(theAudioSource.clip, 1f); // play it

            // start Coroutine to reset volume to normal level
            StartCoroutine("ResetVolumeToNormal", hereWeGo);

            // reset clip start to 30s into main game background music clip
            thePlayer.GetComponentInChildren<Camera>().GetComponent<AudioSource>().time = 30f;

            PostImportantStatusMessage("GET READY TO HUNT! STARTING WAVE 1");

            // start updating player zone position
            bStartZoneUpdate = true;
            PlayersCurrentZone.SetText("0".ToString()); // dirty but will do!

        }
    }

    public IEnumerator ResetVolumeToNormal(AudioClip theClip)
    {
        // simply yield and then increase volume to previous level
        yield return new WaitForSeconds(theClip.length);

        _outputMixer = "No Change"; // reset audio to normal
        GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];
    }

    public bool IsGamePaused()
    {
        // return whether game paused or not
        return bGamePaused;
    }

    public void PauseGame( bool bPause)
    {
        // pause/unpause game
        bGamePaused = bPause;

        if (bPause)
        {
            // Pause Game
            AudioListener.pause = true;

            string messageText = "Game Paused!";
            StatusDisplay.text = messageText.ToString();
            Time.timeScale = 0;
            SetHealthCountdownPaused(true);
        }
        else
        {
            // Resume Game
            AudioListener.pause = false;

            string messageText = "";
            StatusDisplay.text = messageText.ToString();
            Time.timeScale = 1;
            SetHealthCountdownPaused(false);
        }
    }

    //private bool addedHighScore = false;

    public void SetGameOver()
    {
        // display game over on screen, accepts any high score entry before
        bGameOver = true;
        bStartZoneUpdate = false;

        // stop the decay health co-routine from doing anything
        SetHealthCountdownPaused(true); // flag checked inside coroutine

        GameObject[] theWarriors    = GameObject.FindGameObjectsWithTag("Enemy Warrior Base Object");
        GameObject[] theDrones = GameObject.FindGameObjectsWithTag("Enemy Drone");

        GameObject[] thePowerups    = GameObject.FindGameObjectsWithTag("Glowing Powerup");
        GameObject[] thePowerLights = GameObject.FindGameObjectsWithTag("Power Up");
        GameObject[] thePowerContainer = GameObject.FindGameObjectsWithTag("Powerup Container");
        
        GameObject[] theSuperPowers = GameObject.FindGameObjectsWithTag("Super Powerup Container");


        foreach (GameObject superPower in theSuperPowers)
        {
            // destroy them as game over
            Destroy(superPower);
        }

        // destroy warriors
        foreach (GameObject warrior in theWarriors)
        {
            // destroy them as game over
            Destroy(warrior);
        }

        // Destroy Powerup container (powerup & 6 lights each time)
        foreach (GameObject powerLight in thePowerContainer)
        {
            // destroy them as game over
            Destroy(powerLight);
        }

        // destroy drones
        foreach (GameObject drone in theDrones)
        {
            // destroy them as game over
            Destroy(drone);
        }

        // posts end game message in the bigger status message box used solely 
        // at start of levels /end game/ or super powerup availability
        PostImportantStatusMessage("GAME OVER!");
        PostStatusMessage(""); // clear other display

        // play game over voice
        _outputMixer = "Voice Up 10db"; // increase volume of clip 10db over max
        GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];
        theAudioSource.clip = gameOver;
        theAudioSource.PlayOneShot(theAudioSource.clip, 1f); // play it

        // wait for voice to finish before stopping audio
        StartCoroutine("WaitForEndGameVoice");
        
        // Show the highscores table
        ShowHighScores();
        
        // Disable Playercontroller inputs while in highscore table
        thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(true); // disable inputs in the player controller

        if (playerScore >0 && theHighScoresControllerScript.GoodEnoughForHighscores(playerScore))
        {
            //  Add score to highscore table
            theHighScoresControllerScript.AddHighscoreEntryWithName(playerScore);      // opens highscore name entry panel
            theHighScoresControllerScript.SetFocusToEntryField();
        }
        else 
        {
            // no score - high score panel will return us to Instruction panel when user says so
            if (GameObject.Find("Player").GetComponent<PlayerController>().IsNightMode())
            {
                // we are in night mode - so return to day mode as game over for now
                GameObject.Find("Player").GetComponent<PlayerController>().ToggleNightMode();
            }
        }
    }

    // Delay audio cutoff at game end until game over audio clip finishes
    IEnumerator WaitForEndGameVoice()
    {
        AudioSource aSource = GetComponent<AudioSource>();

        yield return new WaitForSeconds(aSource.clip.length); // wait for game over voice to finish

        if (playerScore > 0 && theHighScoresControllerScript.GoodEnoughForHighscores(playerScore))
        {
            // plays "Winner" voice as high enough for table
            _outputMixer = "Voice Up 5db"; // group to output this audio listener to

            GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];

            theAudioSource.clip = winner;
            theAudioSource.PlayOneShot(theAudioSource.clip, 1f); // play it
            yield return new WaitForSeconds(winner.length +1f);
            
            _outputMixer = "No Change"; // reset to normal
            GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];

            Time.timeScale      = 0f;   // stop animating
            AudioListener.pause = true; // stop sounds

            // can allow game over theme to play with an clip ignorepause or something - google it!
        }
        else
        {
            // play 'not good enough' voice, fixes low volume on this clip by using a Mixer set up in GUI
            // set on the AudioListener

            _outputMixer = "Voice Up 10db"; // group to output this audio listener to

            GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];

            theAudioSource.clip = thatsNotGonnaDoIt;
            theAudioSource.PlayOneShot(theAudioSource.clip, 1f); // play it

            yield return new WaitForSeconds(thatsNotGonnaDoIt.length);

            _outputMixer = "No Change"; // reset audio
            GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];

            Time.timeScale      = 0f;   // stop animating
            AudioListener.pause = true; // stop sound
        }
    }

    public bool IsGameOver()
    {
        return bGameOver;
    }

    // get enemy wave number
    public int GetWaveNumber()
    {
        // return enemy wave number
        return enemyWaveNumber;
    }

    public int GetMaxEnemiesPerWave()
    {
        // return maximum number of enemies per wave
        return maxEnemiesPerWave;
    }

    public void UpdateEnemiesKilled()
    {
        // increment number of enemies killed this wave
        totalEnemiesKilled++;
        enemiesKilledThisWave++;

        // update display or start next wave
        if (enemiesKilledThisWave >= maxEnemiesPerWave)
        {
            
            // play level completed voice
            theAudioSource.clip = levelComplete;
            theAudioSource.PlayOneShot(theAudioSource.clip, 1f); // play it

            // start next wave
            enemyWaveNumber++;
            StartWaveNumber(enemyWaveNumber);
            PostImportantStatusMessage("LEVEL COMPLETED! STARTING WAVE " + enemyWaveNumber);
        }
        else
        {
            // update enemies killed & remaining
            EnemiesRemaining.SetText((maxEnemiesPerWave - enemiesKilledThisWave).ToString());
            EnemiesKilledTotal.SetText(totalEnemiesKilled.ToString());
        }
    }

    public void StartWaveNumber( int waveNum)
    {

        enemiesKilledThisWave = 0;

        // display new wave number, and initial enemies remaining
        EnemyWaveNum.SetText(enemyWaveNumber.ToString());
        EnemiesRemaining.SetText(maxEnemiesPerWave.ToString());
    }

    public void SetGameDefaults()
    {
        // Set score/wave and other variables, resets day box, etc.
        enemyWaveNumber       = 1;     // set to initial wave
        totalEnemiesKilled    = 0;     // no enemies killed
        enemiesKilledThisWave = 0;     // nothing killed this wave
        bGameStarted          = true;  // allows Player controller to call StartGame again
        bGameOver             = false; // allow player controller inputs again
        bGamePaused           = false; // game not paused
        playerHealth          = 100;   // reset health
        playerScore           = 0;     // reset score
        playerLives           = 3;     // reset lives
        playerJustDied        = false; // reset to allow player to take damage again
        playersCurrentZone    = 0;     // reset players zone id to start zone
        currentZone           = 0;     // reset players checked zone to start
        clipsLeft             = startingClips;  // initial number of clips
        shotsLeftThisClip     = shotsInAClip;   // initial shots in a clip
        
        // reset contents of display fields
        EnemyWaveNum.SetText(      enemyWaveNumber.ToString());     // wave number
        EnemiesRemaining.SetText(  maxEnemiesPerWave.ToString());   // enemies left this wave
        EnemiesKilledTotal.SetText(totalEnemiesKilled.ToString());  // total enemies killed
        LivesPlayer.SetText(       playerLives.ToString());         // lives left
        PlayerHealth.SetText(      playerHealth.ToString());        // health
        ScorePlayer.SetText(       playerScore.ToString());         // score
        ClipsLeft.SetText(         clipsLeft.ToString());           // initial ammo clips left (set above)
        CountReload.SetText(       shotsLeftThisClip.ToString());   // initial shots in a clip (set above)

        // initialise status message box
        string blank = " ";
        StatusDisplay.text = blank.ToString();

        // turn off any possibly playing sounds (as could be from a restart now)
        PlayCountdown(false);
        PlayCriticalCountdown(false);

        // re-enable smart bomb button
        thePlayer.GetComponent<PlayerController>().SmartBombReset(); // reset smart bomb availability

        // re-enable crosshair target in case showing highscore entry field disabled it
        if (theCrosshairs != null)
        {
            theCrosshairs.SetActive(true);
        }

        // reposition player to start position (work on rotation at some point... rigidbody?)
        theStartPosition.position = new UnityEngine.Vector3(36f, 0.1f, -75f);
        transform.Translate(theStartPosition.position);

        // Hide highscores panel (as we may have come from end game before)
        theHighScoresControllerScript.ShowHighScoresPanel(false); // Do not place this after StartGame()
        
        // allow input in player controller
        thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(false);

        // destroy any (potential) leftover objects from last game
        GameObject[] theWarriors       = GameObject.FindGameObjectsWithTag("Enemy Warrior Base Object");
        GameObject[] thePowerups       = GameObject.FindGameObjectsWithTag("Glowing Powerup");
        GameObject[] thePowerLights    = GameObject.FindGameObjectsWithTag("Power Up");
        GameObject[] thePowerContainer = GameObject.FindGameObjectsWithTag("Powerup Container");
        GameObject[] theDrones         = GameObject.FindGameObjectsWithTag("Enemy Drone");
        GameObject[] theMissiles       = GameObject.FindGameObjectsWithTag("Missile");

        // destroy warriors
        foreach (GameObject warrior in theWarriors)
        {
            // destroy them as game over
            Destroy(warrior);
        }

        // Destroy Powerup container (powerup & 6 lights each time)
        foreach (GameObject powerLight in thePowerContainer)
        {
            // destroy them as game over
            Destroy(powerLight);
        }

        // destroy drones
        foreach (GameObject drone in theDrones)
        {
            // destroy them as game over
            Destroy(drone);
        }

        // destroy missiles
        foreach (GameObject missile in theMissiles)
        {
            // destroy them as game over
            Destroy(missile);
        }

        // Start everything
        AudioListener.pause = false; // enable sounds
        Time.timeScale = 1f;         // reset time to normal time

        // set to day Skybox initially
        UnityEngine.RenderSettings.skybox = theDaySkybox;
    }

    // no longer needed as enemies no longer do searches for energy
    public void DestroyPowerUp(GameObject toKill)
    {
        // find and destroy a particular powerup
        if (toKill != null)
        {
            if (currentPowerups.Exists(x => x.thePowerUp == toKill))
            {
                // it exists, remove and destroy
                Debug.Log("Gameplay Controller found the PowerUp to destroy!");
            }
        }
    }

    // only needed when previously WAS going to get Zombies to search for energy too on a navmesh!
    public GameObject GetPowerUpObject() 
    {
        // returns the location of a PowerUp which isn't currently being searched for,
        // we will need to check periodically with the Gameplay controller it hasnt been eaten by player 
        // in EnemyController Update()
        
        PowerUp found = null; 

        foreach (PowerUp currPower in currentPowerups)
        {
            if (!currPower.currentlySought)
            {
                // this one is not being sought so search for this one
                currPower.currentlySought = true;
                found = currPower;
                break;
            }
        }

        if (found != null)
        {
            // return the screen position
            return found.thePowerUp;
        }
        else return null;
    }

    private bool restartReposition = false; // reposition on game restart

    public bool PlayerRepositionForStart()
    {
        // return whether should be repositioned on restart
        return restartReposition;
    }

    public void ResetEnemySpeeds(int newSpeed)
    {
        // Set speed for enemies - used by player controller
        enemySpeedSetting = newSpeed;
    }

    public int GetEnemySpeed()
    {
        // returns enemy speed setting to enemy objects (and set from player controller)
        return enemySpeedSetting;
    }

    // Start is called before the first frame update
    void Start()
    {
        // do any setup here, initalise scores, display instructions, wait for start, then tell Spawnmanager to start   
        // spawning objects when user starts game from Playercontroller
        bGameOver = false;

        // set pause flag & start health countdown routine as even if not in game should be running
        SetHealthCountdownPaused(true);
        InvokeRepeating("DecayPlayerHealth", 3f, 3f);

        // start zone check every second - will only do a check when game is running
        InvokeRepeating("UpdatePlayerZonePosition", 1f, 1f);

        // set audio source component & mixer
        theAudioSource = GetComponent<AudioSource>();
        theMixer       = Resources.Load("Music") as AudioMixer; // from created "Resources/Music/..." folder in heirarchy
        _outputMixer   = "";

        // initialise an empty Powerup list - for final destroy in grand finale (if I implement one)
        currentPowerups = new List<PowerUp>();

        // set player start position
        theStartPosition = thePlayer.transform;
    }

   void Awake()
   {
        // get high score panel script
        theHighScoresControllerScript = theHighScoresPanel.GetComponentInChildren<HighScoreTableController>();

        if (theHighScoresControllerScript == null)
        {
            Debug.Log("Couldn't find Highscore script from within Gameplay controller");
        }

        // find crosshairs
        theCrosshairs = GameObject.FindGameObjectWithTag("Crosshair Target");

        // get game exit panel
        theGameExitPanel = GameObject.Find("Game Exit Panel"); // needed as we can press Escape in here too
                                                               // check if we found it - but don't disable here as Instruction panel does this
        if (!theGameExitPanel)
        {
            Debug.Log("Can't find Game Exit Panel from Player Controller Awake()");
        }
    }

    void ActivateGameExitPanel()
    {
        // Turn on Game exit panel and disable user input in Player controller for now
        thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(true);
        theGameExitPanel.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (!bGameStarted)
        {
            // Start the game when user is ready and another control panel (high scores/credits/instructions) isn't in use by player
            if (Input.GetKeyUp(KeyCode.S) && !thePlayer.GetComponent<PlayerController>().IsAnotherPanelInControl())
            {
                // Hide highscores panel
                theHighScoresControllerScript.ShowHighScoresPanel(false); // Do not place this after StartGame()

                SetGameDefaults(); // resets all game defaults inc time & sound to normal
                StartGame(true); // start game
               
                PostImportantStatusMessage("GET READY TO HUNT! STARTING WAVE 1");

                if (!restartReposition)
                {
                    // reset to original position from GUI editor
                    theStartPosition.position = new UnityEngine.Vector3(36f, 0.1f, -75f);
                    transform.rotation = UnityEngine.Quaternion.Euler(0f,0f,0f);
                    transform.Rotate(transform.rotation.eulerAngles);
                    transform.Translate(theStartPosition.position);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // allow game exit at any time
            // pressing escape will first just pause game, and if confirmed, will then exit
            // and play exit voice, or maybe an Advert in future!

            if (Input.GetKeyDown(KeyCode.Escape) && !thePlayer.GetComponent<PlayerController>().IsAnotherPanelInControl())
            {
                // fix low volume on this clip by using a Mixer set up in gui
                AudioMixer mixer = Resources.Load("Music") as AudioMixer;
                string _OutputMixer = "Voice Up 10db"; // group to output this audio listener to

                GetComponent<AudioSource>().outputAudioMixerGroup = mixer.FindMatchingGroups(_OutputMixer)[0];
                theAudioSource.clip = youReallyWannaGo;
                theAudioSource.PlayOneShot(theAudioSource.clip, 1f);

                // prevent any inputs in Player controller while we respond
                thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(true);

                Debug.Log("Escape pressed in Gameplay controller");

                // start routine to get confirmation
                StartCoroutine("GameQuit");
            }
        }
    }
    
    // game quit sequence
    IEnumerator GameQuit()
    {
        // show game over dialog
        yield return new WaitForSeconds(theAudioSource.clip.length); // delay till clip over

        if (bGameStarted && !bGameOver)
        {
            // pause game for now
            PauseGame(true);
        }

        ActivateGameExitPanel();
    }


    public void UpdatePlayerScore(int scoreChange)
    {
        // update the score and display on the text panel
        playerScore += scoreChange;

        if (playerScore <= 0)
        {
            playerScore = 0;
        }

        ScorePlayer.text = playerScore.ToString();

        if (playerScore > highScore)
        {
            string high = playerScore.ToString();
            HighScore.SetText(high.ToString());
        }
    }


    void DecayPlayerHealth()
    {
        // only decay health if NOT game over / NOT paused as otherwise
        // highscore entry dialog could reappear at game end
        if (!bGameOver || !bGamePaused)
        {
            if (!IsHealthCountdownPaused())
            {
                // only decays health if NOT paused or game over
                UpdatePlayerHealth(-1);
            }
        }
    }

    public void UpdatePlayerHealth(int healthPoints)
    {
        // healthPoints can be negative to indicate damage from an attack, bomb damage,
        // or positive from collecting a powerup
        playerHealth += healthPoints;

        CheckHealthState();
    }

    public void CheckHealthState() 
    { 
        // checks and update scores, and turns off sounds as necessary
        if (playerHealth >= 100)
        {
            // set to full health
            playerHealth = 100;
        }

        if (playerHealth <= 60)
        {
            // play health countdown music
            PlayCountdown(true);
        }

        if (playerHealth > 60)
        {
            // turn off countdown music
            PlayCountdown(false);
        }
         
        if (playerHealth <=25 && playerHealth >10)
        {
            // display critical health message
            PostStatusMessage("HEALTH CRITICAL!");
        }

        if (playerHealth <= 10)
        {
            // imminent death state
            PostStatusMessage("DEATH IMMINENT - GET POWERUPS NOW!");
            PlayCriticalCountdown(true); // play critical noise
        }

        if (playerHealth <= 0 && !bGameOver)
        {
            // player has died (poor player!)
            playerHealth   = 0;
            playerJustDied = true;
            LoseALife();
        }

        // update gui
        PlayerHealth.text = playerHealth.ToString();
    }

    public void PlayCountdown(bool playIt)
    {
        // play health countdown music
        if (playIt == true && !playingCountdown)
        {
            PostStatusMessage("COLLECT POWERUPS!");
            
            // not playing it, so play it
            _outputMixer = "No Change"; // set to normal levels
            GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];
            theAudioSource.clip = countdownNoise;
            theAudioSource.time = 125.06f;
            theAudioSource.volume = 25;
            theAudioSource.Play();
            playingCountdown = true;
        }

        if (!playIt && playingCountdown)
        {
            // playing it, so stop playing
            theAudioSource.Stop();
            playingCountdown = false;
        }
    }

    public void PlayCriticalCountdown(bool playIt)
    {
        // player is close to death, play critical countdown music

        PlayCountdown(false);

        if (playIt == true && !playingCountdown)
        {
            // play critical countdown noise
            _outputMixer = "No Change"; // group to output the audio listener to
            GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];
            theAudioSource.clip = criticalCountdownNoise;
            theAudioSource.volume = 40;
            theAudioSource.time = 0f;
            theAudioSource.Play();
        }
        else
        {
            // stop playing it
            theAudioSource.Stop();
            playingCountdown = false;
        }
    }

    AudioSource theSource;
    void PlayLifeLost()
    {
        // temporarily reduce game volume (the source is attached to Players Main Camera)
        
        theSource      = thePlayer.GetComponentInChildren<Camera>().GetComponent<AudioSource>();
        float myVolume = theSource.volume;

        theSource.volume = 0;
        StartCoroutine("IncreaseVolumeToPrevious");

        // play life lost noise
        theAudioSource.PlayOneShot(lifeLostNoise, 0.3f);

        // play life lost voice
        _outputMixer = "Voice Up 10db"; // increase volume of clip 10db above max volume using mixer
        GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];

        theAudioSource.clip = loseALifeVoice;
        theAudioSource.PlayOneShot(theAudioSource.clip, 1f); // play it
    }

    IEnumerator IncreaseVolumeToPrevious()
    {
        // simply yield and then increase volume to previous level
        yield return new WaitForSeconds(lifeLostNoise.length+ loseALifeVoice.length);
        
        _outputMixer = "No Change"; // reset audio to normal
        GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];

        // now return it to normal level
        theSource.volume = .26f;
    }

    private void LoseALife()
    {
        // player loses a life
        UpdatePlayerLives(-1);
    }

    private void UpdatePlayerLives(int livesLost)
    {
        // decrease player lives, reset health to full if some left, turn off countdown noise
        playerLives += livesLost;

        if (!IsGameOver())
        {
            string blank = "YOU LOST A LIFE!";
            PostStatusMessage(blank);
        }

        if (playerLives <= 0)
        {
            playerLives = 0;

            // Set Game Over, Player controller will check isGameOver() and allow restart
            SetGameOver();
        }
        else
        {
            // reset for new life
            playerHealth = 100; // reset health

            // delay new hits from current enemies
            playerJustDied = true;
            PostImportantStatusMessage("RUN! 5 SECS BEFORE ATTACKS COUNT!");
            StartCoroutine("PreventHitsTimer");

            PlayerHealth.SetText(playerHealth.ToString());

            thePlayer.GetComponent<PlayerController>().SmartBombReset(); // reset smart bomb availability

            // reset status display
            PlayCriticalCountdown(false); // turn off critical countdown sound
            PlayLifeLost();
        }
 
        LivesPlayer.text = playerLives.ToString(); // update number of lives
    }

    // clears message box for general messages during gameplay
    IEnumerator ClearStatusDisplay()
    {
        // waits 4 seconds without blocking before continuing execution
        yield return new WaitForSeconds(4.0f);
        string dispString = "";
        StatusDisplay.text = dispString.ToString();
    }

    // clears message box for IMPORTANT game information during gameplay
    IEnumerator ClearImportantStatusDisplay()
    {
        // waits 7 seconds without blocking before continuing execution
        yield return new WaitForSeconds(7.0f);
        string dispString = "";
        ImportantStatusDisplay.text = dispString.ToString();
    }

    public void PostStatusMessage(string sStatusMsg)
    {
        // post a general message
        StatusDisplay.text = sStatusMsg.ToString();
        StartCoroutine("ClearStatusDisplay"); // clear 4 secs later
    }
    public void PostImportantStatusMessage(string sStatusMsg)
    {
        // post an important message
        ImportantStatusDisplay.text = sStatusMsg.ToString();
        StartCoroutine("ClearImportantStatusDisplay"); // clear 6 secs later
    }

    public int GetPlayerHealth()
    {
        // just return it
        return playerHealth;
    }

    public void SetPowerUpEntry(GameObject aPowerUp, float theTimeCreated)
    {
        // add a power up object to the array (set by spawn manager)
        if (aPowerUp != null && theTimeCreated >= 0.0f)
        {
            // add a new entry to the powerup list (defaults to not currently sought for by an enemy
            currentPowerups.Add(new PowerUp(aPowerUp, theTimeCreated));
        }
    }

    // Attempts to find a matching powerup object containing the gameObject passed in
    // so we can check its timestamp
    public PowerUp FindPowerUp( GameObject objToFind)
    {
        PowerUp found = null;

        // search our powerup list for a matching one
        foreach (PowerUp currPower in currentPowerups)
        {
            if (currPower.thePowerUp == objToFind)
            {
                // we found a match so return it
                found = currPower;
                break;
            }
        }
        // return either a match or null object
        return found;
    }

    // called by player controller to start weapon reload process
    public void StartWeaponReload()
    {
        // starts weapon reload sequence
        ShotsLeftText.SetText("TIME".ToString()); // set to reloading
        CountReload.SetText("15".ToString());
        PostStatusMessage("OUT OF FUEL - RELOADING!");
        StartCoroutine("WeaponReloadTimer");
    }

    private int timerCountdown = 15; // reload time
    private int elapsedSecs    = 0;

    IEnumerator WeaponReloadTimer()
    {
        while (elapsedSecs < timerCountdown)
        {
            yield return new WaitForSeconds(1f);
            elapsedSecs++;
     
            // update countdown display
            CountReload.SetText((timerCountdown - elapsedSecs).ToString());

            if ((timerCountdown - elapsedSecs) == 3)
            {
                // play 3-2-1 voice
                _outputMixer = "Voice Up 10db"; // group to output the audio listener to
                GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];
                theAudioSource.clip = the321Voice;
                theAudioSource.volume = 1f;
                theAudioSource.time = 0f;
                theAudioSource.Play();
                StartCoroutine("ResetVolumeToNormal",the321Voice);
            }
        }

        // timer exceeded - tell player controller gun reloaded
        ShotsLeftText.SetText("SHOTS".ToString());    // set to available
        CountReload.SetText(shotsInAClip.ToString()); // set to initial value
        elapsedSecs = 0; // reset counter
        thePlayer.GetComponent<PlayerController>().SetGunAvailable(); // re available
    }
}
