using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstructionPanelController : MonoBehaviour
{
    private GameObject               thePlayer               = null; // our player
    private Camera                   theMainCamera           = null; // main camera on player

    private GameObject               thePlayerPanel          = null; // the main gameplay panel with scores/lives/health
    private GameObject               theCreditsReplayPanel   = null; // the credits and replay option panel
    private GameObject               theHighScoresPanel      = null; // high scores display panel
    private HighScoreTableController theHighScoreScript      = null; // game controller script

    private GameObject               theGameController       = null; // game controller
    private GameplayController       theGameControllerScript = null; // game controller script

    // Start is called before the first frame update
    void Start()
    {
        // find the score/lives panel (must be active to be able to find, then immediately de-activate!
        // and same for credits replay panel & high scores one
        thePlayerPanel        = GameObject.Find("Score Lives Panel");
        theCreditsReplayPanel = GameObject.Find("Credits Replay Panel");
        theGameController     = GameObject.Find("GameplayController");
        theHighScoresPanel    = GameObject.Find("High Scores Panel");

        if (theGameController != null)
        {
            theGameControllerScript = theGameController.GetComponent<GameplayController>();

            if (theGameControllerScript == null)
            {
                Debug.Log("Couldn't find Game Controller Script from within Instruction Panel");
            }
        }

        thePlayer = GameObject.Find("Player");

        // turn off high scores panel
        if (theHighScoresPanel != null)
        {
            // found it, immediately deactivate as we are in Instructions Panel now
            // may need to call load high scores later if disabled too soon before Awake()
            // which loads in high scores automatically
            theHighScoreScript = theHighScoresPanel.GetComponentInChildren<HighScoreTableController>();

            if (theHighScoreScript == null)
            {
                Debug.Log("Couldn't find High Score Controller Script from within Instruction Panel");
            }
            else
            {
                // hide panel for now
                theHighScoreScript.ShowHighScoresPanel(false);
            }
        }
        else
        {
            Debug.Log("Can't find High Scores Panel from Instruction Panel start()");
        }

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
        else
        {
            Debug.Log("Can't find Player Score Panel from Instruction Panel start()");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S) && !thePlayer.GetComponent<PlayerController>().IsAnotherPanelInControl())
        {
            // start the game, hide demo characters, enable player score panel, disable this one
            ActivatePlayerPanel(); theGameControllerScript.StartGame(true);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            // enable credits panel, and disable this one
            ActivateCreditsReplayPanel();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            // enable high scores panel, and disable this one
            ActivateHighScorePanel();
        }
    }

    void ActivatePlayerPanel() 
    {
        // disable this panel and credits replay panel
        gameObject.SetActive(false);
        theCreditsReplayPanel.SetActive(false);

        // re-enable user input in Player controller first
        thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(false);

        // turn on player panel, activate player and camera
        thePlayerPanel.SetActive(true);
        thePlayer.SetActive(true);
        theMainCamera.gameObject.SetActive(true);
    }

    // Show the Credits Panel
    void ActivateCreditsReplayPanel()
    {
        // disable user input in Player controller
        thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(true);

        // disable this panel
        gameObject.SetActive(false);
        
        // turn off player, turn on credits replay panel  (and activate camera?)
        thePlayerPanel.SetActive(false);
        thePlayer.SetActive(false);
        
        theCreditsReplayPanel.SetActive(true);
    }

    // Show the High score panel
    void ActivateHighScorePanel()
    {
        // disable user input in Player controller
        thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(true);

        // disable instructions panel
        gameObject.SetActive(false);

        // turn off player panel & hide player, turn on high score panel
        thePlayerPanel.SetActive(false);
        thePlayer.SetActive(false);
        
        // show high scores
        theHighScoreScript.ShowHighScoresPanel(true);
        theHighScoreScript.TurnOnAsk();
    }
}
