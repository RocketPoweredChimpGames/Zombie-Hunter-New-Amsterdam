using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    // player & game components
    private Rigidbody  playerRb;                         // player rigidbody component
    private GameObject focusPoint;                       // camera focus point (a child of player object)
    private GameObject theGameController;                // invisible game object in scene so gets updates
    private Animator   theAnimator;                      // player's animator component
    private GameplayController theGameControllerScript;  // game controller script

    // particle effects
    private GameObject   theFireFlies    = null;  // fire flies for night mode
    private GameObject[] theCandles      = null;  // table candles for night mode effects
    public  GameObject   theFlameThrower = null;  // flamethrower effect for gun

    // spot light
    private GameObject   theHeadLamp = null;  // player headlamp
    private GameObject[] theStreetlightBulbs; // street lighting <move this to game controller?)
    private GameObject[] theHQSpotlights;     // HQ spotlighting (move this to game controller?)
    private GameObject[] theFlickeryLanterns; // Flickering Lanterns (move this to game controller?)

    // buttons
    private Button      smartBombButton;  // smart bomb indicator button

    // sound clips
    public  AudioClip   laserFire;        // laser gunfire sound
    public  AudioClip   walkMovement;     // walking sound
    public  AudioClip   zombieHit;        // zombie hit
    public  AudioClip   youReallyWannaGo; // "wanna go" - escape key voice

    private AudioSource theAudio;         // audio source

    // sky box materials
    public Material daySkyBox;
    public Material nightSkyBox;

    // gravity & speed
    private Vector3 realGravity;          // real world gravity vector (0f,-9.8f,0f)
    public  float   gravityModifier = 1f; // how much extra gravity force is applied
    private float   speed = 12f;          // player movement speed

    // scoring
    private int pointsPerEnemyHitShot = 1;    // points per hit
    private int maxPointsPerEnemy     = 10;   // gamePlayController "points per hit" multiplier - set to 10 as 5 hits per enemy to destroy
    private bool smartBombAvailable   = true; // smart bomb availability to player

    // game boundaries
    private int boundaryZ = 208; // top & bottom (+-) boundaries on Z axis from centre (0,0,0)
    private int boundaryX = 208;  // left & right (+-) boundaries on X axis from centre (0,0,0)

    // night mode toggle
    public bool bNightModeOn = false;

    // bool set by panels to stop player input going ahead here until they say so
    bool bAnotherPanelInControl = false;

    // character controller stuff - added 25/9/2020 to try out
    // unity example code
    //
    private CharacterController playerMovementController;
    private Vector3 playerVelocity;
    private bool    groundedPlayer;
    private float   playerSpeed  = 15.0f;
    private float   jumpHeight   = 1.0f;
    private float   gravityValue = -9.81f;

    // from another example online
    public float    rotationSpeed = 180;
    private Vector3 rotation;
    //
    // end example code

    // stuff required for BUG in Unity character controller!
    private int            overlappingCollidersCount = 0;
    private Collider[]     overlappingColliders      = new Collider[256];
    private List<Collider> ignoredColliders          = new List<Collider>(256);

    private void PreCharacterControllerUpdate()
    {
        Vector3 center = transform.TransformPoint(playerMovementController.center);
        Vector3 delta = (0.5f * playerMovementController.height - playerMovementController.radius) * Vector3.up;
        Vector3 bottom = center - delta;
        Vector3 top = bottom + delta;

        overlappingCollidersCount = Physics.OverlapCapsuleNonAlloc(bottom, top, playerMovementController.radius, overlappingColliders);

        for (int i = 0; i < overlappingCollidersCount; i++)
        {
            Collider overlappingCollider = overlappingColliders[i];

            if (overlappingCollider.gameObject.isStatic)
            {
                continue;
            }

            ignoredColliders.Add(overlappingCollider);
            Physics.IgnoreCollision(playerMovementController, overlappingCollider, true);
        }
    }

    private void PostCharacterControllerUpdate()
    {
        for (int i = 0; i < ignoredColliders.Count; i++)
        {
            Collider ignoredCollider = ignoredColliders[i];

            Physics.IgnoreCollision(playerMovementController, ignoredCollider, false);
        }

        ignoredColliders.Clear();
    }
    // END OF stuff required for BUG in Unity character controller! Ignores Collisions which make it 'jump up'


    public void SetAnotherPanelInControl(bool inControl)
    {
        bAnotherPanelInControl = inControl;
    }

    public bool IsAnotherPanelInControl()
    {
        // needed by Instruction panel to see if some other user panel is currently in control
        return bAnotherPanelInControl;
    }

    // Start is called before the first frame update
    void Start()
    {
        // get players rigidbody component & find focus point for camera
        playerRb   = GetComponent<Rigidbody>();
        focusPoint = GameObject.Find("Focus Point");

        // add character controller
        playerMovementController = gameObject.GetComponent<CharacterController>();

        // get audio source
        theAudio = GetComponent<AudioSource>();

        if (focusPoint == null)
        {
            Debug.Log("Couldn't find the Players Focus Point object, check enabled in gui and correctly tagged/named");
        }

        // Set gravity up in Physics system
        realGravity = new UnityEngine.Vector3(0f, -9.8f, 0f); // downwards force 9.8m/s2
        Physics.gravity = realGravity * gravityModifier;

        // set up laser (actually a flamethrower now) sound
        GetComponent<AudioSource>().playOnAwake = false;
        GetComponent<AudioSource>().clip = laserFire;
        
        // get smart bomb button
        smartBombButton = GameObject.FindGameObjectWithTag("SmartBomb").GetComponent<Button>();

        if (smartBombButton != null)
        {
            // set text and enable it
            smartBombButton.GetComponentInChildren<Text>().text = "Smart Bomb";
            smartBombButton.interactable = true;
            smartBombAvailable = true;
        }

        // find for later usage - turn off for now
        theHQSpotlights = GameObject.FindGameObjectsWithTag("HQ Entry Spots");
        ActivateHQEntrySpots(false);

        // find the flickering wall lanterns in scene
        theFlickeryLanterns = GameObject.FindGameObjectsWithTag("Wall Lantern Flickering");

        // turn off streetlight light bulbs!
        theStreetlightBulbs = GameObject.FindGameObjectsWithTag("Light Bulb");

        foreach (GameObject bulb in theStreetlightBulbs)
        {
            bulb.SetActive(false);
        }

        // turn off lanterns
        foreach (GameObject lantern in theFlickeryLanterns)
        {
            lantern.SetActive(false);
        }

        // get game manager
        theGameController = GameObject.FindGameObjectWithTag("GameController");
        
        if (theGameController == null)
        {
            Debug.LogError("There is no game controller object in the scene!");
        }

        theGameControllerScript = theGameController.GetComponent<GameplayController>();

        // get the animator
        theAnimator = this.GetComponent<Animator>();
        //theAnimator = this.GetComponentInChildren<Animator>();

        if (theAnimator == null)
        {
            Debug.LogError("There is no animator object in: " + gameObject.name);
        }

        theFireFlies = GameObject.FindGameObjectWithTag("Fire Flies");
        
        if (theFireFlies == null)
        {
            Debug.LogError("There are no fire flies in: " + gameObject.name);
        }
        else
        {
            // now we have pointer to them, turn off
            theFireFlies.SetActive(false);
        }

        // Find Headlamp and switch off
        theHeadLamp = GameObject.FindGameObjectWithTag("Player Head Lamp");

        if (theHeadLamp == null)
        {
            Debug.LogError("The Player doesnt have a headlamp object: " + gameObject.name);
        }
        else
        {
            // now we have pointer to them, turn off
            theHeadLamp.SetActive(false);
        }

        theCandles = GameObject.FindGameObjectsWithTag("Candles");

        if (theCandles == null)
        {
            Debug.LogError("There are no candles in: " + gameObject.name);
        }
        else
        {
            // now we have pointer to them, turn off
            foreach (GameObject aCandle in theCandles)
            {
                aCandle.SetActive(false);
            }
            
        }

        // find flamethrower as we hide it on startup
        theFlameThrower = GameObject.FindGameObjectWithTag("Flame Thrower");

        if (theFlameThrower == null)
        {
            Debug.Log("Can't find Flamethrower object, is it unticked (i.e. disabled), not tagged, or missing from player object in editor?");
        }
        else
        {
            // disable for now
            theFlameThrower.SetActive(false);
        }
    }

    
    IEnumerator ShootFlamethrower()
    {
        //  Plays Flamethrower particle effect
        yield return new WaitForSeconds(0.2f); // wait for gun shooting animation to start elsewhere

        theFlameThrower.SetActive(true); // set it active

        Quaternion shootAngle = transform.rotation;  // current rotation of player
        Vector3 shootPoint;
        
        shootPoint = gameObject.transform.Find("Flame Point").position; // point to shoot from (very very very small on screen so not visible!)

        if (shootPoint == Vector3.zero)
        {
            // couldn't find shootpoint so use a default
            Debug.Log("Does the Flame Point exist as a child of the Player object, and has correct name 'Flame Point'");
            shootPoint = new Vector3(transform.position.x + 0.25f, 2f, transform.position.z);
        }

        
        // set flames to be at end of gun
        theFlameThrower.GetComponent<Transform>().position = shootPoint;

        // set direction to fire in to be direction we are facing
        Vector3 newDirectionToGoIn = transform.rotation.eulerAngles;

        // set and play effect
        theFlameThrower.GetComponent<Transform>().localEulerAngles = newDirectionToGoIn;
        theFlameThrower.GetComponentInChildren<ParticleSystem>().Play();

        // suspend turning off for a little bit to allow it to end sequence
        yield return new WaitForSeconds(theFlameThrower.GetComponentInChildren<ParticleSystem>().main.duration);
    }

    // Update is called once per frame
    void Update()
    {
        // allow player input if game started or restarted and game is not over, but not if a control panel
        // like instructions / credits / highscores panel is currently open and in use
        if ( theGameControllerScript.HasGameStarted() && !theGameControllerScript.IsGameOver() && !bAnotherPanelInControl)
        {
            // game has started/restarted and isn't over yet

            // toggle pause game
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (theGameControllerScript.IsGamePaused() == true)
                {
                    // resume game
                    theGameControllerScript.PauseGame(false);
                }
                else
                {
                    // pause game
                    theGameControllerScript.PauseGame(true);
                }
            }

            if (!theGameControllerScript.IsGamePaused())
            {
                // process inputs as not currently paused
                
                // check if player is on ground (example code still)
                groundedPlayer = playerMovementController.isGrounded;

                if (groundedPlayer && playerVelocity.y < 0)
                {
                    playerVelocity.y = 0f;
                }

                // get player cursor key inputs
                float horizontalInput = Input.GetAxis("Horizontal"); // -1 to 1 input from key press (left/right X Axis)
                float verticalInput   = Input.GetAxis("Vertical");   // -1 to 1 input from key press (down/up Z Axis)
                float speed           = 12f;

                // NEW MOVEMENT CONTROLLER CODE 25/9/2020
                //Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));

                this.rotation = new Vector3(0,    Input.GetAxisRaw("Horizontal") * rotationSpeed * Time.deltaTime *0.23f, 0);
                Vector3 move  = new Vector3(0, 0, Input.GetAxisRaw("Vertical") * Time.deltaTime);

                move   = this.transform.TransformDirection(move);

                // Ignore some Collisions
                //PreCharacterControllerUpdate();
                
                playerMovementController.Move(move * playerSpeed);
                playerMovementController.transform.position = new Vector3(transform.position.x, 0f, transform.position.z); // always on ground

                //PostCharacterControllerUpdate();

                this.transform.Rotate(this.rotation);

                // Changes the height position of the player..
                //if (Input.GetButtonDown("Jump") && groundedPlayer)
                //{
                //    playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
                //}

                //playerVelocity.y += gravityValue * Time.deltaTime;
                //controller.Move(playerVelocity * Time.deltaTime);

                // check boundaries for player movement
                if (horizontalInput != 0 || verticalInput != 0)
                {
                    // keep player within playfield
                    if (transform.position.z > boundaryZ)
                    {
                        Vector3 reposTransform = new Vector3(transform.position.x, 0f, boundaryZ - 0.75f);
                        transform.position = reposTransform;
                    }
                    else if (transform.position.z < -boundaryZ)
                    {
                        Vector3 reposTransform = new Vector3(transform.position.x, 0f, -(boundaryZ - 0.75f));
                        transform.position = reposTransform;
                    }
                    else if (transform.position.x > boundaryX)
                    {
                        Vector3 reposTransform = new Vector3(boundaryX - 0.75f, 0f, transform.position.z);
                        transform.position = reposTransform;
                    }
                    else if (transform.position.x < -boundaryX)
                    {
                        Vector3 reposTransform = new Vector3(-(boundaryX - 0.75f), 0f, transform.position.z);
                        transform.position = reposTransform;
                    }
                }

                // change animation state to correct one dependent on movement direction
                // can ONLY be one state at a time as no move forward/left, back/left, forward/right, backward/right animations
                // in the package I found, also has some delays in starting due to transitions baked in, grrr!
                if (horizontalInput != 0)
                {
                    if (horizontalInput < 0)
                    {
                        // we want to move left
                        theAnimator.SetBool("b_moveRight", false);
                        theAnimator.SetBool("b_moveForward", false);
                        theAnimator.SetBool("b_walkBackwards", false);
                        theAnimator.SetBool("b_moveLeft", true);
                    }
                    else
                    {
                        // must be moving right
                        theAnimator.SetBool("b_moveLeft", false);
                        theAnimator.SetBool("b_moveForward", false);
                        theAnimator.SetBool("b_walkBackwards", false);
                        theAnimator.SetBool("b_moveRight", true);
                    }
                }

                else
                {
                    // static - so turn off move left/right animations
                    theAnimator.SetBool("b_moveLeft", false);
                    theAnimator.SetBool("b_moveRight", false);
                }

                // change animation state depending on input
                if (verticalInput != 0)
                {
                    if (verticalInput > 0)
                    {
                        // going forward, set animation running forward
                        theAnimator.SetBool("b_walkBackwards", false);
                        theAnimator.SetBool("b_moveLeft", false);
                        theAnimator.SetBool("b_moveRight", false);
                        theAnimator.SetBool("b_moveForward", true);
                        theAnimator.SetFloat("f_Speed", 2.1f);
                    }
                    else
                    {
                        // set animation moving backwards
                        // need to add on only do it if finished
                        theAnimator.SetBool("b_moveForward", false);
                        theAnimator.SetBool("b_moveLeft", false);
                        theAnimator.SetFloat("f_Speed", 0f);
                        theAnimator.SetBool("b_moveRight", false);
                        theAnimator.SetBool("b_walkBackwards", true);
                    }
                }
                else
                {
                    // stop running
                    theAnimator.SetBool("b_moveForward", false);
                    theAnimator.SetBool("b_walkBackwards", false);
                    theAnimator.SetFloat("f_Speed", 0f);
                }

                // Shoot Flamethrower
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    // Shoot the flamethrower gun / do particle effects & gun noise
                    ShootGun();
                }

                // smart bomb destroy of all enemies
                if (Input.GetKeyDown(KeyCode.G))
                {
                    // Destroy all enemies with smart bomb
                    DestroyAllEnemies();
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    // Toggle Night Mode
                    ToggleNightMode();
                }
                
                if (Input.GetKeyDown(KeyCode.H))
                {
                    // just toggle player headlamp on/off
                    if (theHeadLamp.activeSelf)
                    {
                        theHeadLamp.SetActive(false);
                    }
                    else
                    {
                        theHeadLamp.SetActive(true);
                    }
                }

                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    // low speed (much lower than normal)
                    theGameControllerScript.ResetEnemySpeeds(0);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad1))
                { 
                    // low speed (much lower than normal)
                    theGameControllerScript.ResetEnemySpeeds(0);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    // medium speed (less than normal)
                    theGameControllerScript.ResetEnemySpeeds(1);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad2))
                {
                    // medium speed (less than normal)
                    theGameControllerScript.ResetEnemySpeeds(1);
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    // normal speed
                    theGameControllerScript.ResetEnemySpeeds(2);
                }
                else if (Input.GetKeyDown(KeyCode.Keypad3))
                {
                    // normal speed
                    theGameControllerScript.ResetEnemySpeeds(2);
                }
            }
        }
    }

    // detect triggers set up which can be activated by the player
    private void OnTriggerEnter(Collider other)
    {
        // check what we collided with
        if (other.gameObject.CompareTag("HQ Entry Sensor") && IsNightMode())
        {
            // turn on the spotlighting at the HQ building
            Debug.Log("Entered HQ Zone!");
            ActivateHQEntrySpots(true);
        }
    }

    // detect triggers set up which can be de-activated by the player
    private void OnTriggerExit(Collider other)
    {
        // check what we collided with
        if (other.gameObject.CompareTag("HQ Exit Sensor"))
        {
            // turn off the spotlighting at the HQ building
            Debug.Log("Leaving HQ Zone!");
            ActivateHQEntrySpots(false);
        }
    }

    // Turn on/off the HQ lighting
    private void ActivateHQEntrySpots(bool onOff)
    {
        // turn them on/off
        foreach (GameObject current in theHQSpotlights)
        {
            Light currLight = current.GetComponent<Light>(); 
            currLight.gameObject.SetActive(onOff);
        }
    }

    public bool IsNightMode()
    {
        // needed to turn on day mode if ending and entering a password in HighscoreController
        if (UnityEngine.RenderSettings.skybox == nightSkyBox)
        {
            return true;
        }
        else return false;
    }

    // Toggle Night Mode
    public void ToggleNightMode()
    {
        GameObject theSceneLight = GameObject.FindGameObjectWithTag("Main Lighting");
        Light theLight = theSceneLight.GetComponent<Light>();

        
        if (!bNightModeOn)
        {
            // set to night time mode!
            // set skybox to Night Sky
            UnityEngine.RenderSettings.skybox = nightSkyBox;

            if (theLight != null)
            {
                // set intensity to dark
                theLight.intensity = 0f;

                UnityEngine.RenderSettings.ambientIntensity    = 0.2f; // Will make it dark
                UnityEngine.RenderSettings.reflectionIntensity = 0.2f; // will make it dark
            }
            
            // turn on searchlights
            GameObject.Find("SearchlightController").GetComponent<SearchlightController>().EnableSearchlights(true);

            bNightModeOn = !bNightModeOn;

            // turn on fire flies display!
            theFireFlies.transform.position = new Vector3(30f, 0f, -25f);
            theFireFlies.SetActive(true);
            theFireFlies.GetComponentInChildren<ParticleSystem>().playOnAwake = true;
            theFireFlies.GetComponentInChildren<ParticleSystem>().Play();

            // turn on candles display
            foreach (GameObject aCandle in theCandles)
            {
                aCandle.SetActive(true);
                aCandle.GetComponentInChildren<ParticleSystem>().playOnAwake = true;
                aCandle.GetComponentInChildren<ParticleSystem>().Play();
            }

            // turn on flickering lanterns
            foreach (GameObject lantern in theFlickeryLanterns)
            {
                lantern.SetActive(true);
            }

            // turn on street lighting
            foreach (GameObject bulb in theStreetlightBulbs)
            {
                bulb.SetActive(true);
            }

            // turn on glowing powerups - (these can be destroyed dynamically elsewhere)
            // by each PowerupController, so check for this

            GameObject[] glowingPowerups = GameObject.FindGameObjectsWithTag("Glowing Powerup"); // current (at this millisecond!) ones

            foreach (GameObject glowing in glowingPowerups)
            {
                // could have been destroyed on the C++ native side as Unity's Destroy() actually just
                // deletes the C++ object behind it and as C# uses the CLR managed garbage collection, it just
                // pretends it's deleted/'gone' and lets the C# garbage collector delete it when all refs are gone
                // So, always CHECK if null first!
             
                if (glowing != null)
                {
                    // set glowing light to 'on'
                    glowing.GetComponentInChildren<PowerUpController>().SetPowerupGlowing(true);
                }
                else
                {
                    Debug.Log("Deleted glowing Powerup - in turn on");
                }
            }

            // Turn on Headlamp
            theHeadLamp.SetActive(true);
        }
        else
        {
            // switch to daytime mode
            bNightModeOn = !bNightModeOn;
            UnityEngine.RenderSettings.skybox = daySkyBox;
            UnityEngine.RenderSettings.ambientIntensity    = 1f; // Will make it light
            UnityEngine.RenderSettings.reflectionIntensity = 1f; // will make it light
            UnityEngine.RenderSettings.skybox = daySkyBox;

            // turn off searchlights
            GameObject.Find("SearchlightController").GetComponent<SearchlightController>().EnableSearchlights(false);

            // turn off street lighting
            foreach (GameObject bulb in theStreetlightBulbs)
            {
                bulb.SetActive(false);
            }

            // turn off flickering lanterns
            foreach (GameObject lantern in theFlickeryLanterns)
            {
                lantern.SetActive(false);
            }

            // turn off glowing powerups - (these can be destroyed dynamically elsewhere)
            // by each PowerupController, so check for this

            GameObject[] glowingPowerups = GameObject.FindGameObjectsWithTag("Glowing Powerup"); // current (at this millisecond!) ones

            foreach (GameObject glowing in glowingPowerups)
            {
                // could have been destroyed on the C++ native side as Unity's Destroy() actually just
                // deletes the C++ object behind it and as C# uses the CLR managed garbage collection, it just
                // pretends it's deleted/'gone' and lets the C# garbage collector delete it when all refs are gone
                // So, always CHECK if null first!

                if (glowing != null)
                {
                    // set glowing light to 'off'
                    glowing.GetComponent<PowerUpController>().SetPowerupGlowing(false);
                }
                else
                {
                    Debug.Log("Deleted glowing Powerup - in turn off");
                }
            }

            // turn off HQ spotlights in case we did toggle night mode in there
            ActivateHQEntrySpots(false);

            // reset night to day!
            UnityEngine.RenderSettings.skybox = daySkyBox;
            theLight.intensity = 1f;

            theFireFlies.SetActive(false);
            theFireFlies.GetComponentInChildren<ParticleSystem>().Stop();

            // turn off candles display
            foreach (GameObject aCandle in theCandles)
            {
                aCandle.SetActive(false);
                aCandle.GetComponentInChildren<ParticleSystem>().playOnAwake = false;
                aCandle.GetComponentInChildren<ParticleSystem>().Stop();
            }

            // Turn off Headlamp
            theHeadLamp.SetActive(false);
        }
    }

    // DestroyAllEnemies
    public void DestroyAllEnemies()
    {
        // if we have a SmartBomb we can destroy all enemies at once
        if (smartBombAvailable)
        {
            smartBombAvailable = false; // always first line of code in here!
            enemyAttacker = null;

            // find and destroy all enemies!
            // reset bomb caption, and disable it
            if (smartBombButton != null)
            {
                smartBombButton.GetComponentInChildren<Text>().text = "BOMB USED";
                smartBombButton.interactable = false;
            }

            // get all enemies
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy Warrior Base Object");

            // destroy them one by one
            foreach (GameObject enemy in enemies)
            {
                // destroy them all, calling dyingstate() allows a short delay for sound
                // and animations to finish playing before destroying them
                if (enemy != null)
                {
                    enemy.GetComponentInChildren<EnemyController>().DyingState();

                    if (!theGameControllerScript.IsGameOver())
                    {
                        // gives max points to the player (for each enemy) for killing them, if game isn't over, should really be 
                        // max points minus hit points per object but hey run out of time to do stuff!
                        theGameControllerScript.UpdatePlayerScore(pointsPerEnemyHitShot * maxPointsPerEnemy); // give max points for each enemy destroyed
                    }
                }
            }
        }
    }

    public float damageDone   = 20f;
    public float rangeForHits = 2f;

    // re enable smart bomb button
    public void SmartBombReset()
    {
        if (smartBombButton != null)
        {
            smartBombButton.interactable = true;
            smartBombButton.GetComponentInChildren<Text>().text = "SMART BOMB";
        }
        // make bomb available again
        smartBombAvailable = true;
    }

    void ShootGun()
    {
        // Does a raycast from the players transform in a forward facing direction
        // the 'hit' variable returns the position of the hit on an object collider which we check to see if an enemy character
        // or not, if it is, send a message to enemy object to do damage, and cause it to explode/die,
        // Regardless, does a flame particle effect in transforms forward direction.

        // set to repeat shot animation, as more likely to be firing repeatedly and single shot anim is too slow to activate!
        theAnimator.SetBool("b_RepeatShot", true);

        AudioSource source = GetComponent<AudioSource>();

        // play flamethrower sound straight away - dont wait for completion just play, sounds ok anyway if done repeatedly!
        source.enabled = true;
        source.clip = laserFire;
        source.Play();

        RaycastHit pointHit;

        // Shoot the ray to see if we hit something!
        // - the shoot point for flames (not this shoot point!) is an invisible box positioned exactly where animation of gun reaches, on the player object
        Vector3 shootPoint = new Vector3(transform.position.x, 1.85f, transform.position.z); // shoot from 1.25f above ground

        // shoot the flames
        StartCoroutine(ShootFlamethrower());

        if (Physics.Raycast(shootPoint, transform.forward, out pointHit, rangeForHits))
        {
            // ok... we hit something in range with the raycast
            //Debug.Log("Laser hit :  " + pointHit.transform.name);
            //Debug.Log("Position of Ray cast Hit (x,y,z) is: x=   " + pointHit.point.x + ", y=   " + pointHit.point.y + ", z=   " + pointHit.point.z + ".");
            //Debug.DrawRay(shootPoint, transform.TransformDirection(Vector3.forward) * pointHit.distance, Color.white, 2.0f);

            // check who collided with us - if player update game manager with score        
            if (pointHit.transform.CompareTag("Enemy Warrior"))
            {
                // hit warrior (zombie) - update score in game manager
                theGameControllerScript.UpdatePlayerScore(pointsPerEnemyHitShot); // one point per hit

                // increment hit count for the object we have hit - but only if not dying
                EnemyController theEnemyController = pointHit.rigidbody.gameObject.GetComponent<EnemyController>();

                if (theEnemyController)
                {
                    // only add hit points if enemy not dying already!
                    if (!theEnemyController.IsDying())
                    {
                        // not dead so add a hit
                         theEnemyController.AddHit();
                    }
                }
            }
        }

        // start coroutine to turn off Attack boolean to allow animator to do its animation completely
        // without repeating (hopefully), but only turn it off after shotting gun if a dying state animation has finished playing

        // check if ended already
        if (!theAnimator.GetCurrentAnimatorStateInfo(0).IsName("Z_FallingBack"))
        {
            StartCoroutine("TurnOffAttack");
        }
    }

    IEnumerator TurnOffAttack()
    {
        yield return new WaitForSeconds(0.5f); // give it time to do it - DO NOT CHANGE THIS VALUE NOW, it works!
        theAnimator.SetBool("b_RepeatShot", false); // change to gun idle animation
    }

    bool playerUnderAttack = false;
    GameObject enemyAttacker = null;


    // may need to change distance between objects check elsewhere, to ensure colliders are activated
    // or change size of collider on enemy so player is hit at end point of arms when animated so looks like arms 
    // when hitting player are causing damage points
    private void OnCollisionEnter(Collision collision)
    {
        // check if player has collided with something
        if (collision.gameObject.CompareTag("Power Up"))
        {
            // hit the power up
        }

        if (collision.gameObject.CompareTag("Enemy Warrior"))
        {
            enemyAttacker = collision.gameObject;

            if (!playerUnderAttack)
            {
                playerUnderAttack = true;

                // now start a repeated call to add damage every two seconds - cancelled when we leave collider
                InvokeRepeating("AddHealthDamage", 0f, 2f);
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // not under attack now
        playerUnderAttack = false;

        // should check if ALL enemies are outside player collider rather than just stopping all at once
        CancelInvoke("AddHealthDamage"); // cancel health damage
    }

    void AddHealthDamage()
    {
        //Debug.Log("Adding damage");
        if (enemyAttacker != null)
        {
            theGameControllerScript.UpdatePlayerHealth(GameObject.FindGameObjectWithTag("Enemy Warrior").GetComponent<EnemyController>().hitDamage);
        }
    }
}
