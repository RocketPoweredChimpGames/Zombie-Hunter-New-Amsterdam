using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class GameExitController : MonoBehaviour
{
    // game objects
    private GameObject         thePlayer;            // our Player
    private GameObject         theGameController;    // game controller
    private GameplayController theGameScript;        // game controller script
    private Camera             theMainCamera = null; // main camera

    // audio components etc
    private AudioSource        theAudioSource;       // audio source component
    private AudioMixer         theMixer;             // the audio mixer to output sound from listener to
    public  AudioClip          theGoodbye;           // goodbye voice clip
    private string             _outputMixer;         // holds mixer struct
    private bool               bAppQuitNow = false;  // weird bug where won't quit, so do on next update() instead

    // Start is called before the first frame update
    void Start()
    {
        // find game controller / script
        theGameController = GameObject.FindGameObjectWithTag("GameController");
        theGameScript = theGameController.GetComponent<GameplayController>();

        // find the player
        thePlayer = theGameScript.thePlayer;

        if (thePlayer == null)
        {
            Debug.Log("Can't find the Player from the Game Exit Dialog");
        }
        else
        {
            // find camera to be enabled again later
            theMainCamera = thePlayer.GetComponentInChildren<Camera>();

            if (theMainCamera == null)
            {
                Debug.Log("Couldn't find Main Camera from within High Scores Panel");
            }
        }

        // set audio source component & mixer
        theAudioSource = GetComponent<AudioSource>();
        theMixer = Resources.Load("Music") as AudioMixer; // from "Resources/Music/..." folder in heirarchy
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // pause game if running (may need to delay to allow "you wanna go" voice to play)
            if (!theGameScript.IsGameOver())
            {
                if (theGameScript.bGameStarted)
                {
                    // pause game and continue
                    theGameScript.PauseGame(true);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            // "No" Quit selected, continue game if was running at point of Escape key use
            if (!theGameScript.IsGameOver() && theGameScript.bGameStarted)
            {
                theGameScript.PauseGame(false);
                AudioListener.pause = false; // enable sound again
            }

            // enable inputs again in Player controller
            thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(false);
            gameObject.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            // ok we are going to quit
            /*_outputMixer = "Voice Up 5db"; // increase volume of clip 10db above max volume using mixer
            GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];
            theAudioSource.clip = theGoodbye;
            AudioListener.pause = false; // enable sound
            theAudioSource.PlayOneShot(theAudioSource.clip, 1f);

            Invoke("GameFinished", theGoodbye.length); // delay quit until sound finished */

            theMainCamera.gameObject.SetActive(true);       // turn on main camera
            Application.Quit();
        }

        if (bAppQuitNow)
        {
            theMainCamera.gameObject.SetActive(true);       // turn on main camera
            Application.Quit();
            gameObject.SetActive(false);
        }
    }

    // Game should now exit fully    
    private void GameFinished()
    {
        Debug.Log("Quit Application Called in GameFinished()");
        theGameScript.PauseGame(false);
        thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(false);
        bAppQuitNow = true;
        theMainCamera.gameObject.SetActive(true);       // turn on main camera
        Application.Quit();
    }
}
