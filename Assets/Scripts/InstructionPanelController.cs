using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstructionPanelController : MonoBehaviour
{
    private GameObject thePlayer = null;
    private Camera theMainCamera = null;
    private GameObject thePlayerPanel = null;
    private GameObject theGameController = null;
    private GameplayController theGameControllerScript = null;

    // Start is called before the first frame update
    void Start()
    {
        // find the score/lives panel (must be active to be able to find, then immediately de-activate!
        thePlayerPanel = GameObject.Find("Score Lives Panel");

        theGameController = GameObject.Find("GameplayController");

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
        if (thePlayerPanel)
        {
            // found it, immediately deactivate as we are in Instructions Demo Panel now
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
    }

    void ActivatePlayerPanel() 
    {
        // disable this panel
        gameObject.SetActive(false);

        // turn on player, player panel and activate camera
        thePlayerPanel.SetActive(true);
        thePlayer.SetActive(true);
        theMainCamera.gameObject.SetActive(true);
    }
}
