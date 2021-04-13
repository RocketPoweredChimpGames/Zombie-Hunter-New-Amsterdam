using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstructionPanelController : MonoBehaviour
{
    private GameObject               thePlayer               = null; // our player character
    public  Camera                   theMainCamera           = null; // main camera on player (child of player) - public for audio listener access elsewhere

    private GameObject               thePlayerPanel          = null; // the main gameplay panel with scores/lives/health
    private GameObject               theCreditsReplayPanel   = null; // the credits / replay option panel
    private GameObject               theGameExitPanel        = null; // game exit control panel
    private GameObject               theHighScoresPanel      = null; // high scores display panel
    private HighScoreTableController theHighScoreScript      = null; // high score controller script

    private GameObject               theGameController       = null; // game controller
    private GameplayController       theGameControllerScript = null; // game controller script

    // Start is called before the first frame update
    void Start()
    {
        // find the score/lives panel (must be active to be able to find, then immediately de-activate!
        // and do same for credits replay panel, high scores panel, game exit panels
        thePlayerPanel        = GameObject.Find("Score Lives Panel");
        theCreditsReplayPanel = GameObject.Find("Credits Replay Panel");
        theGameController     = GameObject.Find("GameplayController");
        theHighScoresPanel    = GameObject.Find("High Scores Panel");
        theGameExitPanel      = GameObject.Find("Game Exit Panel");

        if (theGameController != null)
        {
            theGameControllerScript = theGameController.GetComponent<GameplayController>();

            if (theGameControllerScript == null)
            {
                Debug.Log("Couldn't find Game Controller Script from within Instruction Panel");
            }
        }

        // find the player character
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
                Debug.Log("Couldn't find High Score Controller Script from Instruction Panel Start()");
            }
            else
            {
                // hide panel for now
                theHighScoreScript.ShowHighScoresPanel(false);
            }
        }
        else
        {
            Debug.Log("Can't find High Scores Panel from Instruction Panel Start()");
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
                Debug.Log("Couldn't find Players Main Camera from within Instruction Panel Start()");
            }
            else
            {
                // now we can hide the player as we have the camera 
                thePlayer.SetActive(false);
            }
        }

        if (theGameExitPanel)
        {
            // found game exit panel, immediately deactivate as we are in Instructions panel
            theGameExitPanel.SetActive(false);
        }
        else
        {
            Debug.Log("Can't find Game Exit Panel from Instruction Panel start()");
        }

        if (theCreditsReplayPanel)
        {
            // found credits panel, immediately deactivate as we are in Instructions Demo Panel now
            theCreditsReplayPanel.SetActive(false);
        }
        else
        {
            Debug.Log("Can't find Credits Replay Panel from Instruction Panel Start()");
        }

        // NEVER CHANGE THIS DISABLING ORDER - THIS MUST ALWAYS BE LAST
        //
        // Only NOW do we turn off the player panel as we have got all the objects we need for reactivating later
        if (thePlayerPanel)
        {
            // found it, now deactivate as we are currently in Instructions Panel (main control panel)
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
            // activate panels, set defaults and start game spawning/running
            ActivatePlayerPanel();
            theGameControllerScript.SetGameDefaults(); // reset ALL relevant variables to defaults
            theGameControllerScript.StartGame(true); // starts spawning and game running on next update() in other controllers
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

        if (Input.GetKeyDown(KeyCode.Escape) && !thePlayer.GetComponent<PlayerController>().IsAnotherPanelInControl())
        {
            // activate Game exit panel, and disable this one
            Debug.Log("Escape called in Instruction Panel");
            ActivateGameExitPanel();
        }
    }

    void ActivatePlayerPanel() 
    {
        // disable instructions panel, credits replay panel, and high score panel
        gameObject.SetActive(false);
        theCreditsReplayPanel.SetActive(false);
        theHighScoreScript.ShowHighScoresPanel(false);
        
        // turn on player panel, activate player
        thePlayerPanel.SetActive(true);
        thePlayer.SetActive(true);
        theMainCamera.gameObject.SetActive(true);

        // re-enable user input in Player controller first
        thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(false);
        
        
    }

    // Show the Credits Panel
    void ActivateCreditsReplayPanel()
    {
        // disable user input in Player controller
        thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(true);

        // disable this panel
        gameObject.SetActive(false);
        theMainCamera.GetComponent<AudioListener>().enabled = false;

        // turn off player, turn on credits replay panel
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

    void ActivateGameExitPanel()
    {
        // Turn on Game exit panel (overlay on top of Instruction panel), & disable user input in Player controller for now
        thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(true);

        // disable instruction panel
        //gameObject.SetActive(false);

        // turn off player character, turn on credits replay panel  (and activate camera?)
        thePlayerPanel.SetActive(false);
        thePlayer.SetActive(false);

        theGameExitPanel.SetActive(true);
    }
}
