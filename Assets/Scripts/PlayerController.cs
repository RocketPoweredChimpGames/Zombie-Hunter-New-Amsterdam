using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    private GameObject theHeadLamp = null; // player headlamp

    // buttons
    private Button smartBombButton;     // smart bomb indicator button

    // sound clips
    public  AudioClip laserFire;        // laser gunfire sound
    public  AudioClip walkMovement;     // walking sound
    public  AudioClip zombieHit;        // zombie hit
    
    // sky box materials
    public Material daySkyBox;
    public Material nightSkyBox;

    // gravity & speed
    private Vector3 realGravity;          // real world gravity vector (0f,-9.8f,0f)
    public  float   gravityModifier = 1f; // how much extra gravity force is applied
    private float   speed = 22f;          // player movement speed

    // scoring
    private int pointsPerEnemyHitShot = 1;    // points per hit
    private int maxPointsPerEnemy     = 10;   // gamePlayController "points per hit" multiplier - set to 10 as 5 hits per enemy to destroy
    private bool smartBombAvailable   = true; // smart bomb availability to player

    // game boundaries
    private int boundaryZ = 208; // top & bottom (+-) boundaries on Z axis from centre (0,0,0)
    private int boundaryX = 208;  // left & right (+-) boundaries on X axis from centre (0,0,0)

    // night mode toggle
    bool bNightModeOn = false;

    // bool set by panels to stop player input going ahead here until they say so
    bool bAnotherPanelInControl = false;

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
        // get players rigidbody component & find focus
        playerRb = GetComponent<Rigidbody>();
        focusPoint = GameObject.Find("Focus Point");
        
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
        yield return new WaitForSeconds(theFlameThrower.GetComponentInChildren<ParticleSystem>().duration);
    }

    // Update is called once per frame
    void Update()
    {
        // allow player input if game started or restarted and game is not over, but not if a control panel
        // like instructions / credits / highscores panel is currently open and in use
        //if ( theGameControllerScript.HasGameStarted()   && !theGameControllerScript.IsGameOver() && !bAnotherPanelInControl ||
        //    (theGameControllerScript.HasGameRestarted() && !theGameControllerScript.IsGameOver() && !bAnotherPanelInControl))

        if ( (theGameControllerScript.HasGameStarted() || theGameControllerScript.HasGameRestarted()) && !theGameControllerScript.IsGameOver() && !bAnotherPanelInControl)
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

                // get player cursor key inputs
                float horizontalInput = Input.GetAxis("Horizontal"); // -1 to 1 input from key press (left/right X Axis)
                float verticalInput   = Input.GetAxis("Vertical");   // -1 to 1 input from key press (down/up Z Axis)
                float speed           = 12f;

                /*  ORIGINAL CODE FOR PLAYER MOVEMENT IF ALL ELSE FAILS - DO NOT CHANGE STUFF INSIDE HERE!
                    // Move the player depending on which key is pressed
                    if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow))
                    {
                        // Vertical Movement (up) on Z-Axis
                        transform.Translate(Vector3.forward * verticalInput * Time.deltaTime * speed);
                    }
                    else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
                    {
                        // go a bit slower left/right
                        transform.Translate(Vector3.right * horizontalInput * Time.deltaTime * (speed-6f));
                    }
                    
                    transform.Translate(Vector3.right * horizontalInput * Time.deltaTime * speed);
                    END OF ORIGINAL CODE */
                
                   // original dont change
                   // rotate player (sort of)
                   // transform.Rotate(Vector3.up, horizontalInput * Time.deltaTime * 50);
                

                // New movement control code
                if (horizontalInput != 0)
                {
                    // **********   USE THIS IF MY CHANGE DOESNT WORK ***************************************************
                    Rigidbody theRigidBody = gameObject.GetComponentInChildren<Rigidbody>();

                    // Get the Root Transform of the Player Object (i.e. the top level game object)
                    Transform theRootTransform = transform.root;

                    // now create a new vector3 to lookAt
                    Vector3 m_EulerAngleVelocity = new Vector3(0f, 80f, 0f);

                    Quaternion deltaRotation = Quaternion.Euler(m_EulerAngleVelocity * Time.deltaTime * horizontalInput);
                    theRigidBody.MoveRotation(theRigidBody.rotation * deltaRotation);

                    transform.LookAt(theRigidBody.transform.position);
                    theRootTransform.Translate(Vector3.right * horizontalInput * Time.deltaTime * 3);
                }
                // **********   USE THIS IF MY CHANGE DOESNT WORK ***************************************************

                if (horizontalInput != 0)
                {
                    // HORIZONTAL MOVEMENT

                    // rotate around
                    transform.RotateAround(transform.position, Vector3.up, 3f * horizontalInput);
                }

                if (verticalInput != 0)
                {
                    // VERTICAL MOVEMENT

                    // Get the root transform of the Player Object (i.e. our container object's transform)
                    Transform theRootTransform = transform.root;

                    // now create a new vector3 to lookAt based on user input for z-axis movement
                    Vector3 m_EulerAngleVelocity = new Vector3(0f,
                                                               0f,
                                                               theRootTransform.position.z);
                    
                    theRootTransform.Translate(Vector3.forward * verticalInput * Time.deltaTime * speed);
                }
                else
                {
                    transform.root.gameObject.GetComponentInChildren<Rigidbody>().velocity = new Vector3(0f, 0f, 0f);
                }

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

                // Quick exit game!
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    // quit game
                    theGameControllerScript.bGameOver = true;
                    Application.Quit();
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

        // ok game could be over - so allow restart
        if (theGameControllerScript.IsGameOver() && !bAnotherPanelInControl)
        {
            // get restart
            //theGameControllerScript.PostStatusMessage("Game Over - Press 'S' to Restart!");

            // clear any old message
            //theGameControllerScript.PostStatusMessage(" ");

            if (Input.GetKeyDown(KeyCode.R))
            {
                // restart game - keeping high scores etc this time

                // sets day box for restart & turns off headlamp
                if (theHeadLamp.activeSelf == true)
                {
                    // toggle nightmode to off as its currently on
                    ToggleNightMode();
                }

                // restart game - no pressing R in High scores panel should now just reset score health info and pass to
                // instruction panel where pressing S now restarts game....
                theGameControllerScript.RestartGame();
            }
        }
    }

    // Toggle Night Mode
    void ToggleNightMode()
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
            }

            bNightModeOn = !bNightModeOn;

            // turn on fire flies display!
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

            // Turn on Headlamp
            theHeadLamp.SetActive(true);
        }
        else
        {
            // switch to daytime mode
            bNightModeOn = !bNightModeOn;

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
        Vector3 shootPoint = new Vector3(transform.position.x, 1.8f, transform.position.z); // shoot from 1f above ground

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
