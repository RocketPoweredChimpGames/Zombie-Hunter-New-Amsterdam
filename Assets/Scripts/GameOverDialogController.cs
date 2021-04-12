using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

public class GameOverDialogController : MonoBehaviour
{
    private GameObject  thePlayer;            // our Player
    private GameObject  theGameController;    // game controller
    private GameplayController theGameScript; // game controller script
    
    // audio components etc
    private AudioSource theAudioSource;       // audio source component
    private AudioMixer  theMixer;             // the audio mixer to output sound from listener to
    public  AudioClip   theGoodbye;           // goodbye voice clip
    private string      _outputMixer;         // holds mixer struct


    private Rect windowRect = new Rect((Screen.width - 200) / 2, (Screen.height - 300) / 2, 400, 100); // 400x100 box at centre of screen
    private bool bShow = false; // show as required

    // Start is called before the first frame update
    void Start()
    {
        

        // find game controller / script
        theGameController = GameObject.FindGameObjectWithTag("GameController");
        theGameScript     = theGameController.GetComponent<GameplayController>();

        // find the player
        thePlayer = theGameScript.thePlayer;

        if (thePlayer == null)
        {
            Debug.Log("Can't find the Player from the End Game Dialog");
        }

        // set audio source component & mixer
        theAudioSource    = GetComponent<AudioSource>();
        theMixer          = Resources.Load("Music") as AudioMixer; // from created "Resources/Music/..." folder in heirarchy
    }

    void OnGUI()
    {
        // open dialog when needed
        if (bShow)
        {
            // pause game if running
            if (!theGameScript.IsGameOver())
            {
                if (theGameScript.bGameStarted)
                {
                    // pause game and continue
                    theGameScript.PauseGame(true);
                    //AudioListener.pause = false; // enable sound to allow voice to finish
                }

                AudioListener.pause = false; // enable sound to allow voice to finish
                windowRect = GUI.Window(0, windowRect, DialogWindow, "QUIT GAME"); // calls 'DialogWindow' function and shows it
            }
        }
    }

    // display this window
    void DialogWindow(int windowID)
    {
        float y = 20;
        GUI.Label(new Rect(5, y, windowRect.width, 20), "ARE YOU SURE?");

        // resume game
        if (GUI.Button(new Rect(5, y+40, windowRect.width - 10, 20), "NO"))
        {
            // Continue game if was running
            if (!theGameScript.IsGameOver() && theGameScript.bGameStarted)
            {
                theGameScript.PauseGame(false);
            }
            
            // enable inputs again in Player controller
            thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(false);
            bShow = false; // hide dialog
        }

        // quit game
        if (GUI.Button(new Rect(5, y+20, windowRect.width - 10, 20), "YES"))
        {
            // ok we are going to quit
            _outputMixer = "Voice Up 5db"; // increase volume of clip 10db above max volume using mixer
            GetComponent<AudioSource>().outputAudioMixerGroup = theMixer.FindMatchingGroups(_outputMixer)[0];
            theAudioSource.clip = theGoodbye;
            theAudioSource.PlayOneShot(theAudioSource.clip, 1f);

            StartCoroutine("GameFinished");
            //bShow = false;
        }
    }

    // To open the dialogue from outside of the script.
    public void Open()
    {
        bShow = true;
    }

    // final termination of game
    IEnumerator GameFinished()
    {
        yield return new WaitForSeconds(theAudioSource.clip.length); // wait for end of goodbye voice
        theGameScript.PauseGame(false); // otherwise App quit below won't work if in game Mode
        Application.Quit();
    }
}
