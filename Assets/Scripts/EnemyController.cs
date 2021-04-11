using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Controller script used by Enemy (warrior) objects to do a patrol/or attack player, or search for energy power ups
// on the Game Mesh, **NOTE** NOT USED YET as Nav Mesh wouldn't work at start of dev, couldn't warp to it whatever I tried as said
// NavMeshAgent is too far away.
//
// Has code to do patrols, but no patrolling in this project / collecting of Powerups, we just simply attack the Player!

//notgonnabeusing UnityEditor.Experimental.UIElements.GraphView;

public class EnemyController : MonoBehaviour
{
    // serialise the theDestination transform, makes private variables editable in Unity editor
    [SerializeField]
    Transform        currentDestination; // current destination of enemy (player, poweup, or patrol position)
    Transform        startPosition;      // original start position for returning from patrols
    NavMeshAgent     theNavMeshAgent;    // nav mesh agent attached to this character
    GameObject       theDestination;     // object we are now going to (may be a player or a powerup)
    
    public AudioClip dyingScreech;       // zombie death cry
    public AudioClip longestAttack;      // longest attacking noise
    public AudioClip longAttack;         // longer  attacking noise
    public AudioClip shortAttack;        // short   attacking noise

    // the scripts we need (obvious functions!)
    private PlayerController   thePlayerControllerScript;
    private SpawnManager       theSpawnManagerScript;
    private GameplayController theGameControllerScript;

    // game objects
    private GameObject thePlayer         = null;   // player object
    private GameObject theSpawnManager   = null;   // spawn manager
    private GameObject theGameController = null;   // game play controller
    private Light      theNightLight     = null;   // illuminate enemy at night
    private Animator   theAnimator       = null;   // enemy animator component
    private Rigidbody  theEnemyRb        = null;   // enemy rigidbody
    private AudioSource theAudioSource   = null;   // audio source
    
    // Zombie states : patrolling, attacking, or looking for energy
    private bool  onPatrol               = true;   // zombie patrolling
    private bool  onAttack               = false;  // zombie attacking player
    private bool  onPowerUps             = false;  // zombie needs energy (yum yum!)
    private bool  powerGobbled           = false;  // true if eats a power up, false right after in case of updates inbetween -not used yet!

    private float lastUpdateTime;                  // time this zombie was last updated
    private float startTime;                       // time this zombie appeared on screen
    private float depleteEnergyPeriod    = 5.0f;   // time period in which some energy is lost 
    private float enemySpeed             = 3.5f;   // speed the zombie moves towards player (length vector normalised before)
    private float spaceBetween           = 1f;     // solely for avoiding clipping characters on attacks

    private int   currentPowerStatus     = 100;    // zombie starts at full power, and uses 'energyLoss' percent of energy every depleteEnergyPeriod
    private int   energyLoss             = 5;      // zombie energy loss per time period
    private int   minEnergy              = 15;     // minimum energy level before must search for a power up or may die (15%)
    public  int   hitDamage              = -1;     // damage per hit from zombie
    private int   hitCount               = 0;      // how many times has this enemy been hit by player gun
    private int   maxHits                = 5;      // max number of times hit before dying
    
    // Playfield Boundaries (change these if ever resizing play area)
    private int   boundaryZ              = 104;    // top & bottom (+-) boundaries on Z axis from centre (0,0,0)
    private int   boundaryX              = 33;     // left & right (+-) boundaries on X axis from centre (0,0,0)


    // Start is called once only before the first frame update
    void Start()
    {
        thePlayer         = GameObject.FindGameObjectWithTag("Player");         // player
        theSpawnManager   = GameObject.FindGameObjectWithTag("SpawnManager");   // spawn manager 
        theGameController = GameObject.FindGameObjectWithTag("GameController"); // game controller 
        theNightLight     = transform.root.GetComponentInChildren<Light>();

        // get class scripts etc
        thePlayerControllerScript = thePlayer.GetComponentInParent<PlayerController>();    // find the player controller
        theSpawnManagerScript     = theSpawnManager.GetComponent<SpawnManager>();         // find the spawn manager 
        theGameControllerScript   = theGameController.GetComponent<GameplayController>(); // find the gameplay controller
        theAnimator               = this.GetComponent<Animator>(); // animator
        theAudioSource            = GetComponent<AudioSource>();   // audio player
        startPosition             = gameObject.transform;          // our starting position (always returns here after a patrol) (unused for now)
        theDestination            = thePlayer;                     // destination for enemy object - set to player position

        // set a random position for initial patrol, use later, set to player for now!
        // currentDestination = theSpawnManagerScript.getPatrolLocation();

        if (theNightLight)
        {
            // turn on/off depending on current night mode
            theNightLight.GetComponent<Light>().intensity = (thePlayerControllerScript.IsNightMode() ? 100f : 0f);
        }

        // for use when patrolling (used in later projects now)
        lastUpdateTime = Time.realtimeSinceStartup; // start time we will increment later in update()
        startTime      = lastUpdateTime;

        if (thePlayer == null)
        {
            Debug.LogError("Unable to find an object named Player in the scene. Please check name used: ");
        }

        if (thePlayerControllerScript == null)
        {
            Debug.LogError("Unable to find playerControllerScript.");
        }

        // get nav mesh used by 'this' object
        theNavMeshAgent = gameObject.GetComponent<NavMeshAgent>();

        if (theNavMeshAgent == null)
        {
            // post error
            Debug.LogError("The nav mesh is not attached to this object: " + gameObject.name);
        }
        else
        {
            // Warp Navmesh to correct positiom
            if (theNavMeshAgent.enabled && !theNavMeshAgent.isOnNavMesh)
            {
                // Disable in Unity Editor if Nav Mesh ever fails AGAIN!

                Vector3 position = transform.position; // our current position
                NavMeshHit hit; // closest point on the Nav Mesh

                NavMesh.SamplePosition(position, out hit, 50.0f, NavMesh.AllAreas); // find it

                
                /*Debug.Log("Enemy Position x: " + position.x +
                                      " , y: " + position.y +
                                      " , z: " + position.z);*/

                position = hit.position; // usually hardly changes

                /*Debug.Log("Hit Position x: " + position.x +
                                    " , y: " + position.y +
                                    " , z: " + position.z);*/

                theNavMeshAgent.Warp(position); // set the agent to be on mesh - but won't work EVER... WHY?
            }

            // set destination for nav mesh to go to when activated
            // disabled for now --->  SetNavDestination();
        }

        // change animation state from static to running forward
        theAnimator.SetFloat("f_Speed", 2.1f);
        theAnimator.SetBool("b_moveForward", true);
        changeState = true;

        theEnemyRb  = this.GetComponent<Rigidbody>();
        
        if (theEnemyRb == null)
        {
            Debug.Log("The enemy doesn't have a Rigidbody component on it!");
        }
    }

    private void SetNavDestination()
    {
        // set destination for the nav mesh agent to be something to move to - actually set to player for testing purposes
        if (currentDestination != null)
        {
            // we have already set up an object for the navmesh to navigate the enemy character to
            // do it all dynamically later on in development as will either be the player or a power up pill to refresh itself
            if (theNavMeshAgent == null)
            {
                Debug.LogError("No navmesh agent has been found! " + gameObject.name);
            }
            else
            {
                if (theNavMeshAgent.enabled && theNavMeshAgent.isOnNavMesh)
                {
                    Vector3 destVector = currentDestination.transform.position;
                    theNavMeshAgent.SetDestination(destVector);
                }
            }
        }

        // should set destination to Player here i think if null, but not now as no nav mesh!
    }

    bool timeHasPassed()
    {
        // increment time if a second has passed
        if (lastUpdateTime + 1.0f >= Time.realtimeSinceStartup)
        {
            // update time to now
            lastUpdateTime += 1.0f;
            return true;
        }
        else
        {
            return false;
        }
    }

    // States controlling enemy actions
    void setEnemyCurrentState(String currentActivity)
    {
        // allow other scripts to set current activity of this enemy
        if (string.Equals("Patrolling", currentActivity))
        {
            // set to patrol mode
            onPatrol = true;
            onAttack = onPowerUps = false;
        }
        else if (string.Equals("Attacking", currentActivity))
        {
            // set to attack mode
            onAttack = true;
            onPatrol = onPowerUps = false;
        }
        else if (string.Equals("Hungry", currentActivity))
        {
            // set to look for an energy pill
            onPowerUps = true;
            onPatrol = onAttack = false;
        }
    }

    public float startAttackTime;
    private bool attackingPlayer = false; // checked by Attack sound coroutine to stop attack noises
    bool         changeState     = false;

    private void FixedUpdate()
    {
        // check if night mode
        if (thePlayerControllerScript.IsNightMode())
        {
            // turn on night mode light
            theNightLight.intensity = 100f;
        }
        else
        {
            theNightLight.intensity = 0f;
        }
    }

    float healthPeriod = 5.0f; // lose some health every five seconds (per attacker!)

    // Update is called once per frame
    void Update()
    {
        if (timeHasPassed() && !powerGobbled)
        {
            // decrease energy ONLY every "depleteEnergyPeriod"
            if ((lastUpdateTime - startTime) % depleteEnergyPeriod == 0)
            {
                // a depleteEnergyPeriod has passed
                currentPowerStatus -= energyLoss; // reduce energy of object

                // check if we need to look for energy
                if (currentPowerStatus <= minEnergy)
                {
                    GameObject aPowerUp = theGameControllerScript.GetPowerUpObject();  // vector to a free power up

                    if (aPowerUp != null)
                    {
                        SetDestination(aPowerUp);
                    }

                    setEnemyCurrentState("Hungry");
                    //spaceBetween = 0.0f; // allow collision with powerup collider
                }
            }
        }

        // we have a player - so determine what to do now
        if (onPatrol)
        {
            // initial state - just patrol for now between our random patrol position

        }

        // NOTE: *** Changed Priority of project - Enemies always attack Player and don't do any patrol /or require energy! ***
        onAttack = true; // DO NOT REMOVE THIS

        if (onAttack)
        {
            // player within range / or we have been fired upon - let's attack
            SetDestination(thePlayer); // set player object as destination - as player will be moving around to avoid us

            if (changeState == false && !IsDying())
            {
                // change animation state from static to run
                theAnimator.SetFloat("f_Speed", 2.1f);
                theAnimator.SetBool("b_moveForward", true);
                changeState = true;
            }

            // start a coroutine here to play a different noise when closing in on player ONLY
            //if ((Vector3.Distance(thePlayer.transform.position, transform.position) <= 10f) && !attackingPlayer && !IsDying())
            //{
                //StartCoroutine("PlayNearToPlayerNoise");
            //}

            if ((Vector3.Distance(thePlayer.transform.position, transform.position) <= 2.5f) && !attackingPlayer && !IsDying())
            {
                //Debug.Log("attacking player! - distance between" + Vector3.Distance(thePlayer.transform.position, transform.position));

                theAnimator.SetBool("b_Attack", true);
                startAttackTime = Time.realtimeSinceStartup;
                attackingPlayer = true;

                // start coroutine to play attack sounds until cancelled
                StartCoroutine("PlayZombieAttack");
            }

            

            if (attackingPlayer)
            {
                // only decrease player health every few seconds from startAttackTime
                if (Time.realtimeSinceStartup + healthPeriod >= startAttackTime) 
                {
                  startAttackTime = Time.realtimeSinceStartup;
                  theGameControllerScript.UpdatePlayerHealth(hitDamage);
                }
            }

            if (Vector3.Distance(thePlayer.transform.position, transform.position) > 2.5 && attackingPlayer && !IsDying())
            {
                // not in attack range of player OR dead!
                //theAnimator.SetFloat("f_Speed", 2.1f);
                theAnimator.SetBool("b_Attack", false);
                attackingPlayer = false;
                changeState     = false; // allow it to go to run state again on next update()
            }
        }

        // Every Powerup may have just been destroyed after decaying to zero, so check while moving, and default back to player position
        // if Powerup already gone
        if (theDestination == null)
        {
            theDestination = thePlayer;
        }

        // only move enemy towards player if game is not paused
        if (!theGameControllerScript.IsGamePaused())
        {
            // Move Enemy towards Player object
            if (Vector3.Distance(thePlayer.transform.position, transform.position) >= spaceBetween)
            {
                // get vector to player position (and normalise it to same 'length' per update)
                Vector3 direction = (thePlayer.transform.position - transform.position);
                
                // change magnitude TO UNIT LENGTH if necessary
                if (direction.magnitude > 1.0f)
                {
                    direction = direction.normalized;
                }

                // check if enemy is within bounds AND UPDATES direction variable if not
                direction = CheckMoveVector(direction);
                direction.y = 0.0f; // ensure always on ground

                // turn enemy rigidbody in direction of player and LookAt the players direction
                theEnemyRb.velocity = direction;
                transform.LookAt(thePlayer.transform);
                transform.rotation = Quaternion.LookRotation(direction);

                if (!IsDying())
                {
                    // only move towards player if this enemy isn't already dead
                    // use Movetowards() and leave a small gap inbetween player and zombie to avoid Character controller Y-Axis 'JUMP UP' problem
                    if (!attackingPlayer)
                    {
                        gameObject.transform.position = Vector3.MoveTowards(transform.position, thePlayer.transform.position, Time.deltaTime * FindCurrentEnemySpeed());
                    }
                    //gameObject.transform.position = Vector3.MoveTowards(transform.position, thePlayer.transform.position, Time.deltaTime * FindCurrentEnemySpeed());
                }
                else
                {
                    theEnemyRb.velocity = new Vector3(0f,0f,0f);
                }
            }
        }
    }

    IEnumerator PlayZombieAttack()
    {
        // play a randomly selected Zombie attack noise, and at end change it to a different random one
        while (attackingPlayer)
        {
            if (!theAudioSource.isPlaying)
            {
                // nothing playing - so play a clip
                AudioClip clipToPlay;
                int clipNum = UnityEngine.Random.Range(0, 2);

                switch (clipNum)
                {
                    case 1:  clipToPlay = shortAttack;   break;
                    case 2:  clipToPlay = longAttack;    break;
                    case 3:  clipToPlay = longestAttack; break;
                    default: clipToPlay = longAttack;    break;
                }

                theAudioSource.PlayOneShot(clipToPlay, 1f);

                yield return new WaitForSeconds(clipToPlay.length);
            }
            else yield return null;
        }

        yield return null;
    }

    enum enemySpeeds { slow, medium, normal }

    public float FindCurrentEnemySpeed()
    {
        // gets the enemy speed set by player from the game controller

        int nextSpeed = theGameControllerScript.GetEnemySpeed();

        switch (nextSpeed)
        {
            // Speeds to use
            case 0:  enemySpeed  = 1.5f; break; // very slow speed (maybe it will fix probs for some users?)
            case 1:  enemySpeed  = 2.5f; break; // medium speed (less than normal)
            case 2:  enemySpeed  = 3.5f; break; // normal speed
            default: enemySpeed  = 3.5f; break; // normal speed defaulted to
        }

        // return new speed
        return enemySpeed;
    }

    Vector3 CheckMoveVector(Vector3 goingTo)
    {
        // keep enemy character within bounds of play field
        
        Vector3 reposTransform;

        if (goingTo.z > boundaryZ)
        {
            reposTransform = new Vector3(goingTo.x, 0f, boundaryZ - 0.5f);
        }
        else if (goingTo.z < -boundaryZ)
        {
            reposTransform = new Vector3(goingTo.x, 0f, -(boundaryZ - 0.5f));
        }
        else if (goingTo.x > boundaryX)
        {
            reposTransform = new Vector3(boundaryX - 0.5f, 0f, goingTo.z);
        }
        else if (goingTo.x < -boundaryX)
        {
            reposTransform = new Vector3(-(boundaryX - 0.5f), 0f, goingTo.z);
        }
        else
        {
            return goingTo; // return original as ok
        }

        // returns a new transform within bounds of playfield
        return reposTransform;
    }

    void SetDestination(GameObject myDest)
    {
        if (myDest != null)
        {
            theDestination = myDest;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // we collided with something, check if the player, powerup pill, or an obstacle
        //if (collision.other.CompareTag("Power Up"))
        //if (collision.gameObject.CompareTag("Power Up"))
        //{
            // hit by a warrior
           // Debug.Log("Warrior hit Powerup!\n "); // + collision.other.gameObject.tag);

            // increase our energy here if searching for energy?????
            // send message to game controller to destroy powerup
            //theGameControllerScript.DestroyPowerUp(collision.other);
        //}

        if (collision.gameObject.CompareTag("Player"))
        {
            // we are attacking the player or have collided with him - same thing!
            //Debug.Log("Zombie attacking Player!\n");
            theGameControllerScript.UpdatePlayerHealth(hitDamage);
            theGameControllerScript.StatusDisplay.SetText("LOOK OUT! THERE'S A ZOMBIE ABOUT!");
        }
    }

    bool dyingPlaying = false;

    public bool IsDying()
    {
        // return dying state
        return dyingPlaying;
    }

    public void AddHit()
    {
        // increment hits & destroy enemy if equals maximum
        hitCount++;

        if (hitCount >= maxHits)
        {
            if (!dyingPlaying)
            {
                // play death sound and destroy
                dyingPlaying = true;
                DyingState();
            }
        }
    }
    
    public void DyingState()
    {
        // Play dying noise, change animation state, and starts coroutine to delete when finished animation
        // turn off collider so don't add more damage to player as now dead
        Collider theCollider = gameObject.GetComponent<Collider>();

        if (theCollider)
        {
            theCollider.enabled = false;
        }

        // update kill count display
        theGameControllerScript.UpdateEnemiesKilled();

        // set to enemy is falling back animation
        // had problems with this as even though has end time it went
        // back to idle or something grrrr!
            
        // check if ended already
        if (!theAnimator.GetCurrentAnimatorStateInfo(0).IsName("Z_FallingBack"))
        {
            //theAnimator.SetFloat("f_Speed", 0f);
            //theAnimator.SetBool("b_moveForward", false);
            bool isLooping = theAnimator.GetCurrentAnimatorStateInfo(0).loop;

            Debug.Log("Animator looping state  = " + (isLooping ? "is true!" : "is false!"));

            theAnimator.SetBool("b_Attack", false); // prevent transition to attack
            theAnimator.SetBool("b_isShot", true);  // set dead
            Debug.Log("Setting Dying Playing!");
        }
            
        // set & play audio clip
        AudioSource theSource = GetComponent<AudioSource>();
        theSource.playOnAwake = true;
        theSource.clip = dyingScreech;
        theSource.Play();

        // remove our entry in array of gameobjects being separated apart (to prevent character clipping elsewhere)
        //this.GetComponent<EnemySeparation>().RemoveDestroyedEnemy(gameObject);

        // delay destruction till animation completes
        StartCoroutine("CancelIsShot");
    }

    IEnumerator CancelIsShot()
    {
        // wait for exact duration of animation "Zombie_fallingback" (42 frames at 30 fps = 1.4s) plus 1s for dead on ground
        yield return new WaitForSeconds(gameObject.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length);

        // now has been given chance for animation, so prevent it repeating
        theAnimator.SetBool("b_IsShot", false);
        theAnimator.SetBool("b_isDead", true);

        yield return new WaitForSeconds(0.1f);

        // if you ever put an object in a parent object hierarchy must destroy parent container object to destroy child too!
        // but now changed back, so just destroy object!
        Destroy(transform.parent.gameObject);
    }
}
