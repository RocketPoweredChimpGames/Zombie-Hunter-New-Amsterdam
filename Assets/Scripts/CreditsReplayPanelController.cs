using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreditsReplayPanelController : MonoBehaviour
{
    private GameObject thePlayer = null;
    private Camera     theMainCamera       = null; // main camera
    private GameObject theInstructionPanel = null; // the instructions panel
    private GameObject theGameExitPanel    = null; // game exit control panel



    // Start is called before the first frame update
    void Start()
    {
        // find the Player as we need the camera child object
        thePlayer        = GameObject.Find("Player");
        theGameExitPanel = GameObject.Find("Game Exit Panel"); // need this as we can press Escape in here too

        theInstructionPanel = GameObject.Find("Instructions Panel");
        
        if (theInstructionPanel == null)
        {
            // couldn't find Instruction panel from Credits Replay Panel
            Debug.Log("couldn't find Instruction panel from Credits Replay Panel... Disabled too quickly elsewhere?");
        }
        
        if (!theGameExitPanel)
        {
            Debug.Log("Can't find Game Exit Panel from Credits Replay Panel start()");
        }

        if (thePlayer == null)
        {
            Debug.Log("Couldn't find player from within Credits Replay Panel - disabled already?");
        }
        else
        {
            // find camera to be enabled again later
            theMainCamera = gameObject.GetComponentInChildren<Camera>();

            if (theMainCamera == null)
            {
                Debug.Log("Couldn't find Main Camera from within Instruction Panel");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // check for input here to return to Instructions Panel
        if (Input.GetKeyDown(KeyCode.R))
        {
            // enable credits panel, and disable this one
            ActivateInstructionsPanel();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // activate Game exit panel, and disable this one
            Debug.Log("Escape called in Credits Replay Panel");
            ActivateGameExitPanel();
        }
    }

    void ActivateInstructionsPanel()
    {
        // turn off this panel, activate instruction panel and start camera
        
        // re-enable user input in Player controller
        thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(false);

        gameObject.SetActive(false);
        theInstructionPanel.SetActive(true);
        theMainCamera.gameObject.SetActive(true);
    }

    void ActivateGameExitPanel()
    {
        // Turn on Game exit panel & disable user input in Player controller for now
        //thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(true);

        // disable instruction panel
        //gameObject.SetActive(false);

        // turn off player character, turn on credits replay panel  (and activate camera?)
        //thePlayerPanel.SetActive(false);
        //thePlayer.SetActive(false);
        
        theGameExitPanel.SetActive(true);
    }
}
