using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreditsReplayPanelController : MonoBehaviour
{
    private GameObject thePlayer = null;
    private Camera     theMainCamera       = null; // main camera
    private GameObject theInstructionPanel = null; // the instructions panel



    // Start is called before the first frame update
    void Start()
    {
        // find the Player as we need the camera child object
        thePlayer = GameObject.Find("Player");

        theInstructionPanel = GameObject.Find("Instructions Panel");
        if (theInstructionPanel == null)
        {
            // couldn't find Instruction panel from Credits Replay Panel
            Debug.Log("couldn't find Instruction panel from Credits Replay Panel... Disabled too quickly elsewhere?");
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
    }

    void ActivateInstructionsPanel()
    {
        // turn off this panel, activate instruction panel and start camera
        gameObject.SetActive(false);
        theInstructionPanel.SetActive(true);
        theMainCamera.gameObject.SetActive(true);
    }
}
