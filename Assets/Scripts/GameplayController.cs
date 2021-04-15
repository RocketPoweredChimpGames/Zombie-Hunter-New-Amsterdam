﻿using System;
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

// Powerup class not used yet as Enemies don't search for energy at moment
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

// Main game controller
public class GameplayController : MonoBehaviour
{
    // Public Variables
    public GameObject[] patrolPositions;        // patrol objects in scene which our warriors patrol to  - change to a list sometime?
    public GameObject[] theObstacles;           // dynamic array of obstacles (not used now) - delete later
    public GameObject thePlayer;              // our player (set up in the GUI editor - not in program)
    public Transform theStartPosition;       // for repositioning player on restart (if i get it working)

    public GameObject theSpawnManager;        // the spawn manager
    public GameObject theScoringPanel;        // displays player scores, player start/re-start game
    public GameObject theInstructionPanel;    // displayed prior to game start with instructions & animations of alien and player
    public GameObject theCreditsReplayPanel;  // credits and replay panel at end of game
    public GameObject theHighScoresPanel;     // top ten high scores panel
    private GameObject theGameExitPanel = null;// game exit control panel
    private GameObject theCrosshairs;          // target for flame / aiming

    private HighScoreTableController theHighScoresControllerScript; // high scores controller script
    private PlayerController thePlayerScript;               // player controller script

    // all noises are setup by dragging into relevant fields in the Unity editor
    public AudioClip backgroundMusic;        // background music always playing
    public AudioClip countdownNoise;         // played when less than 35% (or whatever changed to) health
    public AudioClip criticalCountdownNoise; // played when 10% or under health 
    public AudioClip lifeLostNoise;          // life lost noise
    public AudioClip youReallyWannaGo;       // quit game ask voice
    public AudioClip goodbye;                // quit game confirmed voice
    public AudioClip zoneChanged;            // zone changed sound
    public AudioClip nextMessageVoice;       // click used to notify of next Important message

    // spoken audio clips
    public AudioClip gameOver;               // 'game over' voice
    public AudioClip levelComplete;          // 'level completed' voice
    public AudioClip winner;                 // 'winner' voice
    public AudioClip hereWeGo;               // 'here we go' game start voice
    public AudioClip loseALifeVoice;         // 'lose a life' voice
    public AudioClip thatsNotGonnaDoIt;      // not going in high score table, or zero score voice
    public AudioClip the321Voice;            // for end of gun reload countdown

    // audio sources / components etc
    private AudioSource theAudioSource;       // audio source (on main camera) for zaps / explosions etc - limit to 10 oneshots
    public  AudioSource backgroundSource;     // audio source for continuous background music
    public  AudioSource countdownSource;      // audio source for countdown music
    public  AudioSource criticalCDSource;     // audio source for critical countdown music

    private AudioMixer  theMixer;             // audio mixer used to mix output for our general audio listener
    private string     _outputMixer;          // holds mixer struct used for all sources as necessary (may change later)


    // DISPLAY FIELDS
    //
    // all TMP text fields are associated by dragging corresponding entries into gameplay controller field entries in the Unity editor
    //
    public TMP_Text ScorePlayer;            // players score
    public TMP_Text LivesPlayer;            // players lives
    public TMP_Text PlayerHealth;           // players health
    public TMP_Text StatusDisplay;          // 'general' status display message box
    public TMP_Text ImportantStatusDisplay; // 'IMPORTANT' status display message box (for Level Starts/Game Over/Super Powerup Msgs Only)
    public TMP_Text PlayersCurrentZone;     // players current zone (always starts in zone 0)
    public TMP_Text EnemyWaveNum;           // enemy wave number
    public TMP_Text EnemiesRemaining;       // remaining enemies this wave
    public TMP_Text EnemiesKilledTotal;     // total enemies killed in game
    public TMP_Text HighScore;              // player high score
    public TMP_Text ClipsLeft;              // number of ammo clips left
    public TMP_Text ShotsLeftText;          // dual function display: shows "SHOTS" & "TIME" (when all shots/clips shots used up)
    public TMP_Text CountReload;            // dual function display: shopws 'shots left' & time to reload

    private List<PowerUp> currentPowerups;  // current powerups on screen for grand finale destruction sequence
    private SpawnManager theSpawnScript;    // spawn manager script

    private UnityEngine.Vector3[] originalStartPosition; // position where our 'Zombie' objects were originally spawned (not used yet)

    // Game Control variables
    public bool bGameStarted      = false;  // has game started
    private bool bGameReStarted   = false;  // has game been restarted (after end game)
    private bool bGamePaused      = false;  // is game on pause
    private bool bGameOver        = false;  // is game over (prevent further user inputs in controllers)
    private bool bStartZoneUpdate = false;  // prevents any zone update until game starts (or restarts)
    private bool playingCountdown = false;  // are we playing countdown noise
    private int  enemySpeedSetting = 2;     // can be changed by Player during game to change enemy speed (0- slow, 1-medium, 2-normal)

    private bool playingCriticalCountdown = false; // are we playing critical countdown
    private bool bHealthCountdownPaused   = true; // flag checked by HealthCountdown Coroutine periodically

    // Game Stats / Difficulty / Progress stuff etc
    private int enemyWaveNumber       = 0;   // current wave (level) number
    private int totalEnemiesKilled    = 0;   // total of all enemies killed since game start
    private int enemiesKilledThisWave = 0;   // how many killed on current wave
    private int currentZombiesPerWave = 30;  // starting number per wave
    private int maxEnemiesPerWave     = 60;  // maximum enemies per wave before starting next wave (can change to make harder later)
    private int incrementZombies      = 5;   // how many increases by per wave
    private int startHitsToKill       = 3;   // starting number of hits before an enemy is killed
    private int hitsToKillEnemy       = 0;   // how many hits from a player before enemy will die (set in start() - dummy value here)
    private int maxHitsToKillEnemy    = 6;   // maximum hits to kill an enemy (ever)

    // Player variables
    private int playerLives           = 3;   // number of player lives (can increase with bonus lives)
    private int playerHealth          = 100; // initial full health
    private int playerScore           = 0;   // initial player score
    private int highScore             = 0;   // retrieved from highscore table at start (from registry)
    private int playersCurrentZone    = 0;   // starting zone (always starts in zone 0) and and also is zone when "last checked" for zone controller
    private int currentZone           = 0;   // current zone player is in (at THIS check time)
    private bool playerJustDied       = false; // set to true if player just died to avoid new hits for a few seconds
    private int currentAttackers      = 0;   // number of current attackers (used to prevent audio overload)
    private int AttackMaxForAudio     = 3;   // we only have 3 different attack audio clips anyway - so restrict to prevent audio dac overload

    // Health warning levels (Percentages)
    private int warnCollectPowerups   = 30;  // starts warning to collect powerups
    private int warnCriticalPowerups  = 20;  // starts warning of critical health condition
    private int warnImminentDeath     = 10;  // starts warning of imminent death

    // Ammunition counters & allowances
    private int startingClips         = 4;   // initial number of clips (may come from a file if player can buy stuff later in dev)
    private int clipsLeft             = 4;   // fuel (ammo) clips remaining
    private int shotsInAClip          = 25;  // total shots in a clip   (may vary if player buys bigger clips later in dev)
    private int shotsLeftThisClip     = 25;  // shots left in current clip  (not used yet - just subtracts shotsfired from this total at minute)

    // these could change if player allowed to pay (or earn points towards by watching adverts) to change these later on
    private int maximumClips         = 15;   // maximum clips allowed either to be carried or available at fuel dumps (total of all active or available)
    private float ammoRefillInterval = 87f;  // ammo is respawned every 87s (avoids conflict with Super Powerup text display & sound - not imp. now)
    private int maxAmmoPerSpawn      = 3;    // how many ammo clips (refills) are allowed at any ONE spawning

    // Smart bomb awarding
    private int smartBombsMax        = 5;     // maximum number of smart bombs allowed (could sell more)
    private int currentSmartBombs    = 1;     // start off with 1 smartbomb
    private int smartBombsAwarded    = 0;     // count of number awarded so far

    private int smartBombPoints      = 5000;  // gain a smart bomb every 5k points (but not more than maxSmartBombs)
    private int nextBombScoreCheck   = 5000;  // ALWAYS SET to same as smartBombPoints to start with, this checkpoint increments during play     

    // 'Super Powerup' Points / time to next one etc
    private int superPowerupPoints   = 500;   // points for collecting a super powerup

    private int superPowerupInterval = 6;     // 6 minutes to next super powerup spawn
    //private int  maxPowerUps       = 200;   // maximum powerups on screen at a time (not used yet)

    // Bonus Lives Awarding
    private int extraLivesMax        = 2;     // maximum of five lives can be held (player starts with 3 lives) in any full game
    private int extraLivesPoints     = 7500;  // get another life every 7.5k points
    private int nextLifeScoreCheck   = 7500;  // ALWAYS SET to same as extraLivesPoints to start with, as this checkpoint increments during play     
    private int extraLivesAwarded    = 0;     // number of extra lives awarded so far

    // Spawn Zone Info
    private int numSpawnZones        = 4;    // zones start at zone zero (so actually plus 1)

    // 'Important Message' Display Variables
    private List<string> ImportantMessageList;   // our list of displayed 'Important' messages
    private float impMsgMaxTime      = 7f;   // max time an important message is displayed (if no other messages)
    private float impMsgMaxDelay     = 2f;   // maximum we delay a message before next message (really +1sec as called by InvokeRepeating every 1s)
    private float timeLastImpMsg     = 0f;   // time last message (if any) was posted in this field

    // Sky box to be used at start of game
    public Material theDaySkybox;    // daytime sky box
    public Material theZombieSkybox; // zombies title page

    public int GetAttackersMax()
    {
        // return maximum allowed before AUDIO suffers
        return AttackMaxForAudio;
    }

    public int GetNumberAttackers()
    {
        // return total number currently attacking player
        return currentAttackers;
    }

    public void DecreaseAttackers()
    {
        // decrease number of current attackers
        currentAttackers--;
    }
    public void IncreaseAttackers()
    {
        // increment number of current attackers
        currentAttackers++;
    }

    public int GetHitsToKillEnemy()
    {
        // used by others to find out how many hits (currently) before an enemy dies
        return hitsToKillEnemy;
    }

    public void SmartBombUsed()
    {
        // decrement - smart bomb just used by player
        currentSmartBombs--;
    }

    public int GetCurrentSmartBombs()
    {
        // returns number of smart bombs held
        return currentSmartBombs;
    }

    public bool MoreSmartBombsAvailable()
    {
        // are more bombs available this game
        if (smartBombsAwarded < smartBombsMax - 1)
        {
            return true;
        }

        // no more available
        return false;
    }

    public int HitsToEnemyDeath()
    {
        // get number of hits from player before an enemy will die
        return hitsToKillEnemy;
    }

    public int GetNumberOfSpawnZones()
    {
        // returns total number of spawning zones in game level
        return numSpawnZones;
    }

    public int GetMaximumClips()
    {
        // maximum clips to be held at any point
        return maximumClips;
    }

    public int GetMaxAmmoPerSpawn()
    {
        // maximum ammo refills allowed at any spawn time
        return maxAmmoPerSpawn;
    }

    public float GetAmmoRefillInterval()
    {
        // return time for a respawn of fuel for a clip
        return ammoRefillInterval;
    }

    private void UpdatePlayerZonePosition()
    {
        // Invoked function called every second to check players current zone for 'zone' display update
        if (thePlayer != null && !IsGameOver() || IsGamePaused())
        {
            // check position
            float currentX = thePlayer.gameObject.transform.position.x;
            float currentZ = thePlayer.gameObject.transform.position.z;

            if ((currentX >= -210f && currentX <= 208f) && (currentZ < 0f && currentZ > -140f))
            {
                // zone 0 - no spawning here
                currentZone = 0;
            }

            if ((currentX >= 0f && currentX <= 208f) && (currentZ >= 0f && currentZ <= 210f))
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
                // zone 4 - Sky Shopping Platform
                currentZone = 4;
            }

            // airport check will go in here later on
            //
            // zone 5!

            if (currentZone != playersCurrentZone)
            {
                // reset zone and beep
                PlayersCurrentZone.SetText(currentZone.ToString());
                playersCurrentZone = currentZone;

                // play bing bong noise
                theAudioSource.PlayOneShot(zoneChanged, 0.85f);
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
        // interval to next spawn
        return superPowerupInterval;
    }

    public int GetStartingClips()
    {
        // returns start number of clips player has to start with
        return startingClips;
    }

    public int GetNumberOfClipsLeft()
    {
        // returns number of clips player has left
        return clipsLeft;
    }

    public void SetAmmoClipUsed()
    {
        // reduce remaining clips by one
        clipsLeft--;
    }

    public void SetAmmoCollected()
    {
        if (clipsLeft < maximumClips)
        {
            clipsLeft++; // increment clips held
            ClipsLeft.SetText(clipsLeft.ToString()); // update TM_UI textmesh display field
            thePlayerScript.UpdateClipsDisplay(); // tell player controller we have collected some ammo
        }
    }

    public int GetShotsInMagazine()
    {
        // returns number of shots in current clip
        return shotsLeftThisClip;
    }

    public void ShotFired()
    {
        // reduces number in current clip by one, which will reduce number of clips as required
        // which will then update display later on in dev - done in player controller at minute
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

    public void ShowHighScores(bool fromEndGame = false)
    {
        // get highscore controller to show high scores
        theHighScoresControllerScript.ShowHighScoresPanel(true);

        if (fromEndGame)
        {
            // turn off Zombie Hunter Title Field
            Debug.Log("highscore display from end game!");
        }
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

    // StartGame() ALWAYS starts the countdown, the Health decay Coroutine is always running (and checks this flag), 
    // end game sets flag to stop countdown decaying health, so initially the value must always be set to TRUE!
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

            // Re-enable audio
            AudioListener.pause = false; // re-enable audio (disabled at end of a previous game)

            // set with high score read from PlayerPrefs
            highScore = theHighScoresControllerScript.GetHighScore();
            HighScore.SetText(highScore.ToString());

            // reset to initial "25" shots (or whatever this changes to) & resets display text
            CountReload.SetText(shotsInAClip.ToString()); // reset shots left in clip display
            ShotsLeftText.SetText("SHOTS".ToString());    // dual purpose field ("SHOTS" and "TIME") display

            // set clips display object to initial "4"
            ClipsLeft.SetText(startingClips.ToString());

            // clear important message field
            ImportantStatusDisplay.SetText("".ToString());

            // starts (or restarts) routine which periodically decays health (every 3s)
            if (IsHealthCountdownPaused())
            {
                // countdown currently paused (must ***ALWAYS*** be paused at 1st start/ and at restart situation), so enable it again
                SetHealthCountdownPaused(false);
                thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(false);
            }

            // increase 'hereWeGo' clip vol by 5db
            _outputMixer = "Voice Up 5db"; // group to output the audio listener to
            GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];
            //theAudioSource.clip = hereWeGo;
            //theAudioSource.time = 0f;
            theAudioSource.PlayOneShot(hereWeGo, 1f);

            // start Coroutine to reset volume to normal level
            StartCoroutine("ResetVolumeToNormal", hereWeGo);

            // reset clip start to 30s into main game background music clip
            //thePlayer.GetComponentInChildren<Camera>().GetComponent<AudioSource>().time = 30f;

            PostImportantStatusMessage("   "); // need this entry when initial list to start it correctly (bug fix for now!)
            PostImportantStatusMessage("HAPPY HUNTING PLAYER! WAVE 1");
            PostStatusMessage(hitsToKillEnemy.ToString() + " HITS TO KILL AN ENEMY!");

            // start updating player zone position 
            bStartZoneUpdate = true;
            PlayersCurrentZone.SetText("0".ToString()); // set initial zone display (Zone 0)
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

    public void PauseGame(bool bPause)
    {
        // pause/unpause game
        bGamePaused = bPause;

        if (bPause)
        {
            // Pause Game
            AudioListener.pause = true;

            string messageText = "GAME PAUSED!";
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
        backgroundSource.Stop(); // stop background music

        bGameOver = true;
        bStartZoneUpdate = false;

        // stop the decay health co-routine from doing anything
        SetHealthCountdownPaused(true); // flag checked inside coroutine

        GameObject[] theWarriors = GameObject.FindGameObjectsWithTag("Enemy Warrior Base Object");
        GameObject[] theDrones = GameObject.FindGameObjectsWithTag("Enemy Drone");
        GameObject[] thePowerups = GameObject.FindGameObjectsWithTag("Glowing Powerup");
        GameObject[] thePowerLights = GameObject.FindGameObjectsWithTag("Power Up");
        GameObject[] thePowerContainer = GameObject.FindGameObjectsWithTag("Powerup Container");
        GameObject[] theSuperPowers = GameObject.FindGameObjectsWithTag("Super Powerup Container");
        GameObject[] theAmmo = GameObject.FindGameObjectsWithTag("Petrol Can");

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

        // destroy any ammo left
        foreach (GameObject ammo in theAmmo)
        {
            // destroy them as game over
            if (ammo != null)
            {
                Destroy(ammo);
            }
        }

        // not an important message as game now not running so wouldn't display
        ImportantStatusDisplay.SetText("GAME OVER!".ToString());

        // play game over voice
        _outputMixer = "Voice Up 10db"; // increase volume of clip 10db over max
        GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];
        theAudioSource.PlayOneShot(gameOver, 1f); // play it

        // wait for voice to finish before stopping audio
        StartCoroutine("WaitForEndGameVoice");

        // Show the highscores table
        ShowHighScores(true);

        // Disable Playercontroller inputs while in highscore table
        thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(true); // disable inputs in the player controller

        if (playerScore > 0 && theHighScoresControllerScript.GoodEnoughForHighscores(playerScore))
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

        // set to Zombie Skybox
        //UnityEngine.RenderSettings.skybox = theZombieSkybox; // maybe sometime later
    }

    // Delay audio cutoff at game end until game over audio clip finishes
    IEnumerator WaitForEndGameVoice()
    {
        yield return new WaitForSeconds(gameOver.length); // wait for game over voice to finish

        if (playerScore > 0 && theHighScoresControllerScript.GoodEnoughForHighscores(playerScore))
        {
            // plays "Winner" voice as high enough for table
            _outputMixer = "Voice Up 5db"; // group to output this audio listener to

            GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];

            theAudioSource.PlayOneShot(winner, 1f); // play it
            yield return new WaitForSeconds(winner.length);

            _outputMixer = "No Change"; // reset to normal
            GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];

            Time.timeScale = 0f;   // stop animating
            AudioListener.pause = true; // stop sounds
        }
        else
        {
            // play 'not good enough' voice, fixes low volume on this clip by using a Mixer set up in GUI
            // set on the AudioListener

            _outputMixer = "Voice Up 10db"; // group to output this audio listener to

            GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];

            theAudioSource.PlayOneShot(thatsNotGonnaDoIt, 1f); // play it

            yield return new WaitForSeconds(thatsNotGonnaDoIt.length);

            _outputMixer = "No Change"; // reset audio
            GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];

            yield return new WaitForSeconds(2f);

            Time.timeScale = 0f;   // stop animating
            AudioListener.pause = true; // stop sound
        }
    }

    public bool IsGameOver()
    {
        return bGameOver; // return game over flag
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
        if (enemiesKilledThisWave >= currentZombiesPerWave)
        {
            // level completed, play level completed voice
            _outputMixer = "Voice Up 5db"; // group to output this audio listener to
            GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];
            theAudioSource.PlayOneShot(levelComplete, 1f); // play it

            StartCoroutine("ResetVolumeToNormal", levelComplete); // change volume to normal again

            // start the next wave
            enemyWaveNumber++; // increment wave number
            StartWaveNumber(enemyWaveNumber);
        }
        else
        {
            // update enemies remaining
            EnemiesRemaining.SetText((currentZombiesPerWave - enemiesKilledThisWave).ToString());
        }

        EnemiesKilledTotal.SetText(totalEnemiesKilled.ToString()); // update enemies killed
    }

    private void ResetHitsToKillEnemy()
    {
        // find all enemies in play, and increase their resilience to hits
        // as we have now started a new level
        GameObject[] theEnemies = GameObject.FindGameObjectsWithTag("Enemy Warrior Base Object");

        foreach (GameObject enemy in theEnemies)
        {
            // increase maximum hits allowed before they die
            if (enemy != null)
            {
                enemy.GetComponentInChildren<EnemyController>().IncreaseHitsToKill();
            }
        }
    }

    public void StartWaveNumber(int waveNum)
    {
        enemiesKilledThisWave = 0; // reset for new wave

        // don't exceed maximum hits to kill
        if (hitsToKillEnemy < maxHitsToKillEnemy)
        {
            hitsToKillEnemy++;         // increment as started a new wave
            ResetHitsToKillEnemy(); // reset to new number of hits to kill for any existing enemies on screen
        }

        currentZombiesPerWave += incrementZombies; // increase to new number of zombies for next level

        // give player wave info
        PostImportantStatusMessage("  "); // bodge for now
        PostImportantStatusMessage("LEVEL COMPLETED! STARTING WAVE " + waveNum + "\nKILL " +
                                    currentZombiesPerWave.ToString() + " ZOMBIES, " +
                                    hitsToKillEnemy.ToString() + " HITS TO KILL EACH!");

        Debug.Log(hitsToKillEnemy.ToString() + " HITS TO KILL ENEMIES ON WAVE " + waveNum.ToString());

        // update enemies remaining
        EnemiesRemaining.SetText((currentZombiesPerWave - enemiesKilledThisWave).ToString());

        // don't allow it to exceed maximum allowed
        if (currentZombiesPerWave >= maxEnemiesPerWave)
        {
            currentZombiesPerWave = maxEnemiesPerWave;
        }

        // display new wave number, and initial enemies remaining
        EnemyWaveNum.SetText(enemyWaveNumber.ToString());
        EnemiesRemaining.SetText(currentZombiesPerWave.ToString());
    }

    public void SetGameDefaults()
    {
        // Reset all player/score/wave and any other variables, resets day box

        enemyWaveNumber       = 1;                 // set to initial wave
        totalEnemiesKilled    = 0;                 // no enemies killed
        enemiesKilledThisWave = 0;                 // nothing killed this wave
        bGameStarted          = true;              // allows Player controller to call StartGame again
        bGameOver             = false;             // allow player controller inputs again
        bGamePaused           = false;             // game not paused
        playerHealth          = 100;               // reset health
        playerScore           = 0;                 // reset score
        playerLives           = 3;                 // reset lives
        playerJustDied        = false;             // reset to allow player to take damage again
        playersCurrentZone    = 0;                 // reset players zone id to start zone
        currentZone           = 0;                 // reset players checked zone to start
        clipsLeft             = startingClips;     // initial number of clips
        shotsLeftThisClip     = shotsInAClip;      // initial shots in a clip
        currentSmartBombs     = 1;                 // start off with 1 smartbomb
        smartBombsAwarded     = 0;                 // count of number awarded so far
        nextBombScoreCheck    = smartBombPoints;   // reset to first check level
        nextLifeScoreCheck    = extraLivesPoints;  // ALWAYS same as extraLivesPoints to start with, as check score increments during play     
        extraLivesAwarded     = 0;                 // reset count of extra lives awarded so far in game
        
        // current attackers is checked by enemy controller script and smartbomb() use
        currentAttackers      = 0;                 // nobody currently attacking player (this is ONLY ever used for preventing AUDIO DAC overload)

        // turn off any possibly playing sounds (as could be after a 'game over')
        PlayCountdown(false);
        PlayCriticalCountdown(false);

        // reset contents of display fields
        EnemyWaveNum.SetText(enemyWaveNumber.ToString());           // first wave
        EnemiesRemaining.SetText(currentZombiesPerWave.ToString()); // reset to starting number of enemies to kill
        EnemiesKilledTotal.SetText(totalEnemiesKilled.ToString());  // no enemies killed
        LivesPlayer.SetText(playerLives.ToString());                // initial lives left
        PlayerHealth.SetText(playerHealth.ToString());              // full health
        ScorePlayer.SetText(playerScore.ToString());                // zero score
        ClipsLeft.SetText(clipsLeft.ToString());                    // initial ammo clips left (set above)
        CountReload.SetText(shotsLeftThisClip.ToString());          // initial shots in a clip (set above)

        // initialise status message box
        string blank = " ";
        StatusDisplay.text = blank.ToString();

        // re-enable smart bomb button (checks with us here, and resets text on button)
        thePlayer.GetComponent<PlayerController>().SmartBombReset(); // reset smart bomb availability

        // re-enable crosshair target in case showing highscore entry field disabled it
        if (theCrosshairs != null)
        {
            theCrosshairs.SetActive(true);
        }

        // reposition player to start position (work on rotation at some point... rigidbody?)
        theStartPosition.position = new UnityEngine.Vector3(36f, 0.1f, -75f);
        transform.Translate(theStartPosition.position);

        // Hide highscores panel (as we may have come from "game over")
        theHighScoresControllerScript.ShowHighScoresPanel(false); // Do NOT EVER place this after StartGame()

        // ok to allow input in player controller now
        thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(false);

        // destroy any (potential) leftover objects from last game caused by repeating invokes after game end
        GameObject[] theWarriors = GameObject.FindGameObjectsWithTag("Enemy Warrior Base Object");
        GameObject[] theSuperPower = GameObject.FindGameObjectsWithTag("Super Powerup Container");
        GameObject[] thePowerContainer = GameObject.FindGameObjectsWithTag("Powerup Container");
        GameObject[] theDrones = GameObject.FindGameObjectsWithTag("Enemy Drone");
        GameObject[] theMissiles = GameObject.FindGameObjectsWithTag("Missile");
        GameObject[] theAmmo = GameObject.FindGameObjectsWithTag("Petrol Can");

        // destroy warriors
        foreach (GameObject warrior in theWarriors)
        {
            // destroy them as game over
            if (warrior != null)
            {
                Destroy(warrior);
            }
        }

        // Destroy any Super Powerup
        foreach (GameObject superPowerups in theSuperPower)
        {
            // destroy them as game over
            if (superPowerups != null)
            {
                Destroy(superPowerups);
            }
        }

        // Destroy Powerup container (powerup & 6 lights each time)
        foreach (GameObject powerups in thePowerContainer)
        {
            // destroy them as game over
            if (powerups != null)
            {
                Destroy(powerups);
            }
        }

        // destroy drones
        foreach (GameObject drone in theDrones)
        {
            // destroy them as game over
            if (drone != null)
            {
                Destroy(drone);
            }
        }

        // destroy missiles
        foreach (GameObject missile in theMissiles)
        {
            // destroy them as game over
            if (missile != null)
            {
                Destroy(missile);
            }

        }

        // destroy any ammo left
        foreach (GameObject ammo in theAmmo)
        {
            // destroy them as game over
            if (ammo != null)
            {
                Destroy(ammo);
            }
        }

        // Start everything going!
        AudioListener.pause = false; // enable playing sounds
        Time.timeScale = 1f;    // reset time to 'normal' time

        // set to day Skybox initially
        UnityEngine.RenderSettings.skybox = theDaySkybox;
    }

    // nt currently needed as enemies no longer do searches for energy too
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

    // only needed when previously WAS going to get Zombies to search for energy too
    public GameObject GetPowerUpObject()
    {
        // returns the location of a PowerUp which isn't currently being searched for,
        // we will need to check periodically with the Gameplay controller it hasn't been collected already by the player 
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

        ImportantMessageList = new List<string>();           // create an empty 'important' message list
        InvokeRepeating("DisplayImportantMessages", 1f, 1f); // checks list every second while game running for new messages

        // set pause flag & start health countdown routine as even if not in game should be running
        SetHealthCountdownPaused(true);
        InvokeRepeating("DecayPlayerHealth", 3f, 3f);

        // start zone check every second - will only do a check when game is running
        InvokeRepeating("UpdatePlayerZonePosition", 1f, 1f);

        // set audio source component & mixer
        theAudioSource = GetComponent<AudioSource>();
        theMixer = Resources.Load("Music") as AudioMixer; // from created "Resources/Music/..." folder in heirarchy
        _outputMixer = "";

        // looping sources
        backgroundSource.loop   = true;
        criticalCDSource.loop   = true;
        countdownSource.loop    = true;

        // non looping / oneshots
        theAudioSource.loop     = false; // on players main camera

        // set volumes
        backgroundSource.volume = 0.4f;
        criticalCDSource.volume = 1f;
        countdownSource.volume  = 0.5f;
        theAudioSource.volume   = 1f;

        // initialise an empty Powerup list - for final destroy in grand finale (if I implement one)
        currentPowerups = new List<PowerUp>();

        // set player start position
        theStartPosition = thePlayer.transform;

        hitsToKillEnemy = startHitsToKill; // set default hits to kill an enemy
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


        if (thePlayer != null)
        {
            // get the player controller script
            thePlayerScript = thePlayer.GetComponent<PlayerController>();

            if (thePlayerScript == null)
            {
                Debug.Log("Couldn't find Player controller script from within Gameplay controller Awake()");
            }
        }
        else
        {
            Debug.Log("Player not set up in GUI in Gameplay controller Awake()");
        }

        theSpawnManager = GameObject.FindGameObjectWithTag("SpawnManager");

        if (theSpawnManager != null)
        {
            // get the spawn controller script
            theSpawnScript = theSpawnManager.GetComponent<SpawnManager>();

            if (theSpawnScript == null)
            {
                Debug.Log("Couldn't find Spawn Manager controller script from within Gameplay controller Awake()");
            }
        }
        else
        {
            Debug.Log("No SpawnManager object set up in GUI Scene - in Gameplay controller Awake()");
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

                // start background music from beginning
                backgroundSource.time = 0f;
                backgroundSource.volume = 0.45f;
                backgroundSource.loop = true;
                backgroundSource.Play();
                
                PostImportantStatusMessage("GET READY TO HUNT! STARTING WAVE 1");

                if (!restartReposition)
                {
                    // reset to original position from GUI editor
                    theStartPosition.position = new UnityEngine.Vector3(36f, 0.1f, -75f);
                    transform.rotation = UnityEngine.Quaternion.Euler(0f, 0f, 0f);
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

        // give player an extra smart bomb if point score level met/exceeded
        if ((playerScore >= nextBombScoreCheck) && (smartBombsAwarded < smartBombsMax - 1) && (currentSmartBombs < smartBombsMax))
        {
            // level exceeded, award a smart bomb if not maximum awarded or held
            // ok to award one, we always start with one so check level minus one here!
            smartBombsAwarded++;
            currentSmartBombs++;
            nextBombScoreCheck += smartBombPoints; // increment check to next level

            // inform when next one is awarded or no more
            PostImportantStatusMessage("EXTRA SMART BOMB! " + (smartBombsAwarded < smartBombsMax - 1 ? " NEXT AT " + nextBombScoreCheck.ToString() + " POINTS!" :
                "NO MORE BONUS BOMBS LEFT!"));

            thePlayerScript.SmartBombReset(); // always call as may have disabled if only had one bomb last time used
        }

        // Give player an extra life 
        if (playerScore >= nextLifeScoreCheck && extraLivesAwarded < extraLivesMax)
        {
            // ok to give player another life
            playerLives++;
            extraLivesAwarded++;
            nextLifeScoreCheck += extraLivesPoints; // increment to next point level
            LivesPlayer.SetText(playerLives.ToString()); // update display
            PostImportantStatusMessage("EXTRA LIFE AWARDED! " + (extraLivesAwarded < extraLivesMax ? "NEXT AT " + nextLifeScoreCheck.ToString() + " POINTS!" :
                "NO MORE EXTRA LIVES AVAILABLE!"));
        }
    }

    // function called every few seconds (started with InvokeRepeating elsewhere)
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

    /* 
     * List<string>  ImportantMessageList; // our list of displayed messages
       private float impMsgMaxTime = 7f;   // max time an important message is displayed
       private float impMsgMaxDelay = 4f;   // max time we delay a message before clearing current one
       private float timeLastImpMsg = 0f;   // time last message (if any) was posted */


    // started in Start() and only does checks when game running or not paused, clears list at end game
    // checks / updates every second for new messages
    void DisplayImportantMessages()
    {
        if (!bGameOver || !bGamePaused)
        {
            // display / update the messages if not paused or game over
            if ((ImportantMessageList.Count() == 0) && (Time.time >= timeLastImpMsg + impMsgMaxTime) && (timeLastImpMsg != 0f))
            {
                // remove any currently displayed entry
                ImportantStatusDisplay.text = "".ToString(); // clear it
                timeLastImpMsg = 0f;
            }

            if ((ImportantMessageList.Count == 1) && timeLastImpMsg == 0f)
            {
                // first time any message displayed
                ImportantStatusDisplay.text = ImportantMessageList[0].ToString(); // display it
                ImportantMessageList.Clear();
                timeLastImpMsg = Time.time;
            }

            if ((ImportantMessageList.Count == 1) && Time.time >= timeLastImpMsg + impMsgMaxTime)
            {
                // time for display has expired
                ImportantStatusDisplay.text = "".ToString(); // clear it
                ImportantMessageList.Clear(); // empty the list
                timeLastImpMsg = 0f;
            }

            if (ImportantMessageList.Count() > 1)
            {
                if (Time.time >= timeLastImpMsg + impMsgMaxDelay)
                {
                    // ok we have more messages, so reduce the current one's display time
                    // and display the next if the reduced display period has passed
                    // by copying the list to a new list minus the first element, and reseting

                    List<string> tempList = new List<string>();
                    tempList = ImportantMessageList.GetRange(1, ImportantMessageList.Count() - 1); // copy leaving out first message
                    ImportantMessageList.Clear(); // clear old out
                    ImportantMessageList = tempList; // reset to new list
                    timeLastImpMsg = Time.time;
                    ImportantStatusDisplay.text = ImportantMessageList[0].ToString(); // display next message

                    // play a beep on changing to new message
                    theAudioSource.PlayOneShot(nextMessageVoice, 1f);
                }
            }
        }

        if (bGameOver)
        {
            // just remove all entries & clear display
            ImportantMessageList.Clear();
            timeLastImpMsg = 0f;
        }
    }


    public void UpdatePlayerHealth(int healthPoints)
    {
        // healthPoints can be negative to indicate damage from an attack, bomb damage,
        // or positive after  collecting a powerup / superpowerup
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

        if (playerHealth <= warnCollectPowerups)
        {
            // play health countdown music
            PostStatusMessage("GET POWERUPS!");
            PlayCountdown(true);
        }

        if (playerHealth > warnCollectPowerups)
        {
            // turn off countdown music
            PlayCountdown(false);
        }

        if (playerHealth > warnImminentDeath)
        {
            // turn off critical countdown music
            PlayCriticalCountdown(false);
        }

        if (playerHealth <= warnCriticalPowerups && playerHealth > warnImminentDeath)
        {
            // display critical health message
            PostStatusMessage("HEALTH CRITICAL! GET POWERUPS NOW!");
        }

        if (playerHealth <= warnImminentDeath)
        {
            // warn of imminent death state
            PostStatusMessage("DEATH IMMINENT - GET POWERUPS NOW!");
            PlayCountdown(false);
            PlayCriticalCountdown(true); // play critical noise
        }

        if (playerHealth <= 0 && !bGameOver)
        {
            // player has died (poor player!)
            playerHealth = 0;
            playerJustDied = true;
            PlayCountdown(false);
            PlayCriticalCountdown(false);
            LoseALife();
        }

        // update gui
        PlayerHealth.text = playerHealth.ToString();
    }

    // so much simpler now using separate audio sources!
    // all sources here set to loop & volumes set in Start()
    public void PlayCountdown(bool playIt)
    {
        // play health countdown music (non critical)
        if (playIt)
        {
            // we want to play it - so just start it
            if (!playingCountdown)
            {
                countdownSource.Play(); // either plays from start position, or resumes from where it was
                playingCountdown = true;
            }
        }    

        if (!playIt)
        {
            // pause till next time
            countdownSource.Pause();
            playingCountdown = false;
        }
    }

    public void PlayCriticalCountdown(bool playIt)
    {
        // player is close to death, play critical countdown music
        if (playIt)
        {
            // we want to play it - so just start it
            if (!playingCriticalCountdown)
            {
                criticalCDSource.Play(); // either plays from start position, or resumes from where it was
                playingCriticalCountdown = true;
            }
        }

        if (!playIt)
        {
            // pause till next time
            criticalCDSource.Pause();
            playingCriticalCountdown = false;
        }
    }
    
    void PlayLifeLost()
    {
        // game volume (the source is attached to Players Main Camera)
        //theAudioSource = thePlayer.GetComponentInChildren<Camera>().GetComponent<AudioSource>();

        // play life lost noise
        theAudioSource.clip = lifeLostNoise;
        theAudioSource.volume = 1f;
        theAudioSource.Play();
        //theAudioSource.PlayOneShot(lifeLostNoise, 1f);

        // play life lost voice
        _outputMixer = "Voice Up 10db"; // increase volume of clip 10db above max volume using mixer
        GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];

        theAudioSource.clip = loseALifeVoice;
        theAudioSource.time = 0f;
        theAudioSource.PlayOneShot(loseALifeVoice, 1f); // play it
        //theAudioSource.Play();

        StartCoroutine("SetVolumeNormal");
    }

    IEnumerator SetVolumeNormal()
    {
        // simply yields and then changes volume to previous level
        yield return new WaitForSeconds(lifeLostNoise.length+ loseALifeVoice.length);

        _outputMixer = "No Change"; // reset audio to normal
        GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];

        // now return it to normal level
        theAudioSource.volume = 0.45f;
    }

    private void LoseALife()
    {
        // player loses a life
        PlayCriticalCountdown(false); // switch off critical countdown
        UpdatePlayerLives(-1);
    }

    private void UpdatePlayerLives(int livesLost)
    {
        // decrease player lives, reset health to full if some left, turn off countdown noise
        playerLives += livesLost;

        if (!IsGameOver())
        {
            if (ImportantMessageList.Count == 0)
            {
                // fudge for problem with empty list at moment
                PostImportantStatusMessage("   ");
            }
        }

        if (playerLives <= 0)
        {
            playerLives = 0;
            PostImportantStatusMessage("GAME OVER!");

            // Set Game Over, Player controller will check isGameOver() and allow restart
            SetGameOver();
        }
        else
        {
            // reset for new life
            playerHealth = 100; // reset health

            // delay new hits from current enemies
            playerJustDied = true;
            PostImportantStatusMessage("YOU LOST A LIFE - NO MORE ATTACKS FOR FIVE SECONDS!");
            StartCoroutine("PreventHitsTimer");

            PlayerHealth.SetText(playerHealth.ToString());

            thePlayer.GetComponent<PlayerController>().SmartBombReset(); // reset smart bomb availability

            // reset status display
            PlayCriticalCountdown(false); // turn off critical countdown sound
            PlayLifeLost();
        }
 
        LivesPlayer.text = playerLives.ToString(); // update number of lives

        if (currentSmartBombs < smartBombsMax)
        {
            // give another smart bomb
            currentSmartBombs++;
            thePlayerScript.SmartBombReset();
        }
    }

    // clears message box for general messages during gameplay
    IEnumerator ClearStatusDisplay()
    {
        // waits 4 seconds without blocking before continuing execution
        yield return new WaitForSeconds(4.0f);
        string dispString = "";
        StatusDisplay.text = dispString.ToString();
    }

    public void PostStatusMessage(string sStatusMsg)
    {
        // post a general message
        StatusDisplay.text = sStatusMsg.ToString();
        StartCoroutine("ClearStatusDisplay"); // clear 4 secs later
    }
    
    public void PostImportantStatusMessage(string sStatusMsg)
    {
        // post an important message (add it to the list of messages being displayed)
        // just add it to our list of displayed messages
        ImportantMessageList.Add(sStatusMsg);
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
        CountReload.SetText("10".ToString());
        PostImportantStatusMessage("OUT OF AMMO - RELOADING!");
        StartCoroutine("WeaponReloadTimer");
    }

    private int timerCountdown = 10; // reload time
    private int elapsedSecs    = 0;

    IEnumerator WeaponReloadTimer()
    {
        while ((elapsedSecs < timerCountdown) && !bGameOver)
        {
            // only do this while game running
            yield return new WaitForSeconds(1f);
            elapsedSecs++;
     
            // update countdown display
            CountReload.SetText((timerCountdown - elapsedSecs).ToString());

            if ((timerCountdown - elapsedSecs) == 3)
            {
                // increase clip vol by 10db
                _outputMixer = "Voice Up 10db"; // group to output the audio listener to
                GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];

                // play 3-2-1 voice
                theAudioSource.volume = 1;
                theAudioSource.PlayOneShot(the321Voice, 1f);

                // start Coroutine to reset volume to normal level
                StartCoroutine("ResetVolumeToNormal", the321Voice);
            }
        }

        // timer exceeded - tell player controller gun reloaded
        ShotsLeftText.SetText("SHOTS".ToString());    // set to available
        CountReload.SetText(shotsInAClip.ToString()); // set to initial value
        elapsedSecs = 0; // reset timer counter
        thePlayer.GetComponent<PlayerController>().SetGunAvailable(); // gun is nowavailable

        // Allow ammo spawning again
        theSpawnScript.SetAmmoSpawnAllowed(); // resets flags in spawn manager
    }
}
