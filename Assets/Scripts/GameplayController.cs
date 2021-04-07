using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.UI;

public class PowerUp
{
    // class used to store PowerUp object and creation time
    // for use in a List<>
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

    // all noises are setup by dragging into relevant fields in the gui editor
    public  AudioClip   countdownNoise;         // played when less than 60% (or whatever changed to) health
    public  AudioClip   criticalCountdownNoise; // played when 10% or under health 
    public  AudioClip   lifeLostNoise;          // life lost noise
    
    // add later
    //public AudioClip  cheeringNoiseLowScore;  // ordinary cheering at end
    //public AudioClip  cheeringNoiseHighScore; // cheering when highscore beaten

    private AudioSource theAudioSource;         // audio source component

    // all these text fields are associated by dragging field entries into gameplay controller entries in gui editor
    public TMP_Text     ScorePlayer;            // players score
    public TMP_Text     LivesPlayer;            // players lives
    public TMP_Text     PlayerHealth;           // players health
    public TMP_Text     StatusDisplay;          // status display messages box
    public TMP_Text     EnemyWaveNum;           // enemy wave number
    public TMP_Text     EnemiesRemaining;       // remaining enemies this wave
    public TMP_Text     EnemiesKilledTotal;     // total enemies killed in game

    private List<PowerUp> currentPowerups;      // current powerups on screen for grand finale destruction sequence
    private SpawnManager theSpawnManager;       // the spawn manager

    private UnityEngine.Vector3[] originalStartPosition; // position where our 'Warrior' objects were originally spawned (not used yet)
    
    // game control 
    public  bool bGameStarted     = false;  // has game started
    private bool bGameReStarted   = false;  // has game been restarted
    public  bool bGamePaused      = false;  // is game on pause
    public  bool bGameOver        = false;  // is game over
    public  bool playingCountdown = false;  // are we playing countdown noise

    // player variables
    private int maxPowerUps  = 100;         // maximum powerups on screen at a time
    private int playerLives  = 3;           // number of player lives
    public  int playerHealth = 100;         // initial full health
    private int playerScore  = 0;           // initial player score
    
    private int[] highScores;               // top ten high scores - probably read them from a serialised JSON file later on
    private int highScore;                  // put in a file later to keep

    // game stats stuff
    public  int enemyWaveNumber       = 0;  // wave number
    private int totalEnemiesKilled    = 0;  // total of all kills
    private int enemiesKilledThisWave = 0;  // how many killed on current wave
    private int maxEnemiesPerWave     = 30; // maximum per wave before starting next wave

    // sky box to use at start
    public Material theDaySkybox; // daytime sky box

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

    public void StartGame(bool bStart)
    {
        if (bStart == true)
        {
            // setting bGameStarted will start game spawning on next update() in SpawnController, and enable Player controls
            // in player controller
            bGameStarted = bStart; // start game
            enemyWaveNumber = 1; // first wave

            PauseGame(false); // turn off pause

            // update player display
            StartWaveNumber(enemyWaveNumber);
            EnemiesKilledTotal.SetText(totalEnemiesKilled.ToString());

            // FOR TESTING OF END GAME RESTART
            //playerHealth = 2;
            //playerLives = 1;

            // start routine to decay health periodically
            StartHealthCountdown();

            // set day box initially
            UnityEngine.RenderSettings.skybox = theDaySkybox;
        }
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
        }
        else
        {
            // Resume Game
            AudioListener.pause = false;

            string messageText = "";
            StatusDisplay.text = messageText.ToString();
            Time.timeScale = 1;
        }
    }

    public void SetGameOver()
    {
        // display restart option on screen, and tell player controller to accept input of "Y" to start again?
        bGameOver = true;

        GameObject[] theWarriors = GameObject.FindGameObjectsWithTag("Enemy Warrior Base Object");
        GameObject[] thePowerups = GameObject.FindGameObjectsWithTag("Power Up");
        GameObject[] theDrones   = GameObject.FindGameObjectsWithTag("Enemy Drone");

        // destroy warriors
        foreach (GameObject warrior in theWarriors)
        {
            // destroy them as game over
            Destroy(warrior);
        }

        // destroy powerups
        foreach (GameObject powerUp in thePowerups)
        {
            // destroy them as game over
            Destroy(powerUp);
        }

        // destroy drones
        foreach (GameObject drone in theDrones)
        {
            // destroy them as game over
            Destroy(drone);
        }

        string blank = "Game Over! Press 'S' to Restart!";
        StatusDisplay.text = blank.ToString();

        // stop animations and music
        Time.timeScale = 0f;
        AudioListener.pause = true;
        StatusDisplay.text = blank.ToString();
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
            // start next wave
            enemyWaveNumber++;
            StartWaveNumber(enemyWaveNumber);
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
        // destroy the warriors which may not have been killed due to animations not having completed - BUG FIX!!! AAGH!
        GameObject[] theWarriors = GameObject.FindGameObjectsWithTag("Enemy Warrior");

        // destroy warriors
        foreach (GameObject warrior in theWarriors)
        {
            // destroy them as game over
            Destroy(warrior);
        }

        enemiesKilledThisWave = 0;

        // display new wave number, and initial enemies remaining
        EnemyWaveNum.SetText(enemyWaveNumber.ToString());
        EnemiesRemaining.SetText(maxEnemiesPerWave.ToString());
        string dispString = "Starting Wave " + waveNum.ToString();
        StatusDisplay.SetText(dispString);
    }

    public void RestartGame()
    {
        // restarts game when finished and user wants to play again
        // reset score/wave and other variables
        enemyWaveNumber       = 1;
        totalEnemiesKilled    = 0;
        enemiesKilledThisWave = 0;
        bGameReStarted        = true; // for high score display later
        bGamePaused           = false;
        bGameOver             = false;
        playerHealth          = 100;
        playerScore           = 0;
        playerLives           = 3;

        // start animations and music again
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // reset contents of display fields
        EnemyWaveNum.SetText(enemyWaveNumber.ToString());
        EnemiesRemaining.SetText(maxEnemiesPerWave.ToString());
        EnemiesKilledTotal.SetText(totalEnemiesKilled.ToString());
        LivesPlayer.SetText(playerLives.ToString());
        PlayerHealth.SetText(playerHealth.ToString());
        ScorePlayer.SetText(playerScore.ToString());

        // initialise status message box again
        string blank = " ";
        StatusDisplay.text = blank.ToString();

        // turn off any playing sounds
        PlayCountdown(false);
        PlayCriticalCountdown(false);

        // re-enable smart bomb button
        thePlayer.GetComponent<PlayerController>().SmartBombReset(); // reset smart bomb availability
        bGameStarted = true; // causes start of spawning/re-enables player input/starts health countdown
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

    // Start is called before the first frame update
    void Start()
    {
        // do any setup here, initalise scores, display instructions, wait for start, then tell Spawnmanager to start   
        // when user requests
        //StartGame(true); // will cause SpawnManager to start spawning

        SetupHighScores(); // will read from a file at some point

        // get audio source component
        theAudioSource = GetComponent<AudioSource>();

        // initialise an empty Powerup list - for final destroy in grand finale (if I implement one)
        currentPowerups = new List<PowerUp>();

        // get player start position
        theStartPosition = thePlayer.transform;
    }

    public void StartHealthCountdown()
    {
        // called to start health countdown
        // now start health countdown so Player always needs to collect powerups!
        InvokeRepeating("DecayPlayerHealth", 3f, 3f); // in 3 secs time, start losing some health every 3 secs!
    }

    // Update is called once per frame
    void Update()
    {
        if (!bGameStarted)
        {
            // Start the game when user is ready
            if (Input.GetKeyDown(KeyCode.S))
            {
                // Start game
                StartGame(true);
            }
        }
    }

    public void UpdatePlayerScore(int scoreChange)
    {
        // update the score and display on the text panel
        playerScore += scoreChange;

        if (playerScore <0)
        {
            playerScore = 0;
        }

        ScorePlayer.text = playerScore.ToString();
    }


    void DecayPlayerHealth()
    {
        // just decays health by a point
        UpdatePlayerHealth(-1);
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
            TMP_Text statusDisp = GameObject.FindGameObjectWithTag("Status Display").GetComponent<TMP_Text>();
            
            string blank = "Health Critical!";

            statusDisp.text = blank.ToString();
        }

        if (playerHealth <= 10)
        {
            // imminent death state
            string blank = "Death Imminent - Get Powerups Now!";

            StatusDisplay.text = blank.ToString(); // display critical message
            PlayCriticalCountdown(true); // play critical noise
        }

        if (playerHealth <= 0)
        {
            // player has died (poor player!)
            playerHealth = 0;
            LoseALife();
        }

        // update gui
        PlayerHealth.text = playerHealth.ToString();
    }

    void PlayCountdown(bool playIt)
    {
        // play health countdown music
        if (playIt == true && !playingCountdown)
        {
            string blank = "Collect more Powerups!";
            StatusDisplay.SetText(blank.ToString());

            // not playing it, so play it
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

    void PlayCriticalCountdown(bool playIt)
    {
        // player is close to death, play critical countdown music

        PlayCountdown(false);

        if (playIt == true && !playingCountdown)
        {
            // play critical countdown noise
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
    
    void PlayLifeLost()
    {
        // play life lost noise
        theAudioSource.volume = 50;
        theAudioSource.time = 0f;
        theAudioSource.PlayOneShot(lifeLostNoise);
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

        string blank = "You Lost a Life!";

        // find bonus health field
        StatusDisplay.text = blank.ToString();

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
            PlayerHealth.SetText(playerHealth.ToString());

            thePlayer.GetComponent<PlayerController>().SmartBombReset(); // reset smart bomb availability

            // reset status display
            PlayCriticalCountdown(false); // turn off critical countdown sound
            PlayLifeLost();

            StartCoroutine("ClearStatusDisplay"); // clears after a short delay
        }
 
        LivesPlayer.text = playerLives.ToString(); // update number of lives
    }

    IEnumerator ClearStatusDisplay()
    {
        // waits 4 seconds without blocking before continuing execution
        yield return new WaitForSeconds(4.0f);
        string dispString = "";
        StatusDisplay.text = dispString.ToString();
    }

    public void PostStatusMessage(string sStatusMsg)
    {
        // post passed message
        StatusDisplay.text = sStatusMsg.ToString();
    }

    public int GetPlayerHealth()
    {
        // just return it
        return playerHealth;
    }

    void SetupHighScores()
    {
        // populate highscores and names list later
        highScores = new int[10];
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
}
