using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.Audio; // for audio components

public class PetrolCanController : MonoBehaviour
{
    private GameObject         theGameController;         // gameplayController object in scene
    private GameObject         thePlayer;                 // player object in scene
    private PlayerController   thePlayerControlScript;    // player script
    private GameplayController theGameControllerScript;   // game controller script
    
    private AudioSource theAudio;                         // audio source
    private AudioMixer  theMixer;                         // audio mixer
    private string      _outputMixer;                     // mixer struct which we set with mixer group to use
    public  AudioClip   ammoCollected;                    // audio clip to play on "collecting" petrol
    private bool        hitByPlayer = false;              // set to TRUE when hit by Player, prevents spurious hits

    private void Awake()
    {
        // find game controller
        theGameController = GameObject.FindGameObjectWithTag("GameController");

        if (theGameController != null)
        {
            theGameControllerScript = theGameController.GetComponent<GameplayController>(); // find the controller script
        }
        else
        {
            UnityEngine.Debug.LogError("Didn't find GameController in PetrolCanController Awake(), check tag in Unity");
        }

        // find the player
        thePlayer = GameObject.FindGameObjectWithTag("Player");

        if (thePlayer != null)
        {
            thePlayerControlScript = theGameController.GetComponent<PlayerController>(); // find the controller script
        }
        else
        {
            UnityEngine.Debug.LogError("Didn't find Player object in PetrolCanController Awake(), check tag in Unity");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // setup audio
        theAudio = GetComponent<AudioSource>();
        theMixer = Resources.Load("Music") as AudioMixer; // from created "Resources/Music/..." folder in heirarchy
        _outputMixer = "";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        // check who triggered this - if the player update Clips in Game Controller, and set gun available in Player
        if (other.gameObject.CompareTag("Player") && !hitByPlayer)
        {
            // prevent re-collisions giving more points
            hitByPlayer = true;

            // turn collider off too for extra security
            gameObject.GetComponent<Collider>().enabled = false;

            // update score & health in game manager
            theGameControllerScript.SetAmmoCollected();

            string fuel = "AMMO CLIP COLLECTED!";

            theGameControllerScript.PostStatusMessage(fuel); // display it

            // turn off mesh renderer to hide it
            gameObject.GetComponent<MeshRenderer>().enabled = false;

            // play sound and delay destruction
            StartCoroutine("PlayAmmoCollected");
        }
    }

    IEnumerator PlayAmmoCollected()
    {

        // play ammo collected fanfare noise
        theAudio.clip = ammoCollected;
        theAudio.volume = 0.8f;
        theAudio.PlayOneShot(ammoCollected,0.8f);

        yield return new WaitForSeconds(ammoCollected.length); // until clip finishes

        // now delete
        Destroy(gameObject);
    }
}
