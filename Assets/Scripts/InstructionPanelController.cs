using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstructionPanelController : MonoBehaviour
{
    private GameObject         thePlayer               = null; // our player
    private Camera             theMainCamera           = null; // main camera
    private GameObject         thePlayerPanel          = null; // the main gameplay panel
    private GameObject         theCreditsReplayPanel   = null; // the credits and replay option panel

    private GameObject         theGameController       = null; // game controller
    private GameplayController theGameControllerScript = null; // game controller script

    // Start is called before the first frame update
    void Start()
    {
        // find the score/lives panel (must be active to be able to find, then immediately de-activate!
        // and same for credits replay panel
        thePlayerPanel        = GameObject.Find("Score Lives Panel");
        theCreditsReplayPanel = GameObject.Find("Credits Replay Panel");
        theGameController     = GameObject.Find("GameplayController");

        if (theGameController != null)
        {
            theGameControllerScript = theGameController.GetComponent<GameplayController>();

            if (theGameControllerScript == null)
            {
                Debug.Log("Couldn't find Game Controller from within Instruction Panel");
            }
        }

        thePlayer = GameObject.Find("Player");

        if (thePlayer == null)
        {
            Debug.Log("Couldn't find Player from within Instruction Panel");
        }
        else
        {
            // find camera to be enabled again later
            theMainCamera = thePlayer.GetComponentInChildren<Camera>();

            if (theMainCamera == null)
            {
                Debug.Log("Couldn't find Players Main Camera from within Instruction Panel");
            }
            else
            {
                // now we can hide the player as we have the camera 
                thePlayer.SetActive(false);
            }
        }

        // only now do we turn off player panel as we have got all the objects we need to reactivate later

        if (theCreditsReplayPanel)
        {
            // found it, immediately deactivate as we are in Instructions Demo Panel now
            theCreditsReplayPanel.SetActive(false);
        }
        else
        {
            Debug.Log("Can't find Credits Replay Panel from Instruction Panel start()");
        }

        // turn off player score panel
        if (thePlayerPanel)
        {
            // found it, immediately deactivate as we are in Instructions Panel now
            thePlayerPanel.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            // start the game, hide demo characters, enable player score panel, disable this one
            ActivatePlayerPanel();
            theGameControllerScript.StartGame(true);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            // enable credits panel, and disable this one
            ActivateCreditsReplayPanel();
        }
    }

    void ActivatePlayerPanel() 
    {
        // disable this panel and credits replay panel
        gameObject.SetActive(false);
        theCreditsReplayPanel.SetActive(false);

        // turn on player panel, activate player and camera
        thePlayerPanel.SetActive(true);
        thePlayer.SetActive(true);
        theMainCamera.gameObject.SetActive(true);
    }
    void ActivateCreditsReplayPanel()
    {
        // disable this panel
        gameObject.SetActive(false);

        // turn off player, turn on credits replay panel  (and activate camera?)
        thePlayerPanel.SetActive(false);
        thePlayer.SetActive(false);
        
        theCreditsReplayPanel.SetActive(true);
        //theMainCamera.gameObject.SetActive(true); // maybe????
    }
}
