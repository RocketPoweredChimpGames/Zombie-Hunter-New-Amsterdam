using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
//using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.UI;


public class HighScoreTableController : MonoBehaviour
{
    private GameObject thePlayer           = null;   // the player
    private Camera     theMainCamera       = null;   // main camera
    
    private GameObject theInstructionPanel = null;   // instructions panel
    private GameObject theScoreLivesPanel  = null;   // player scoring panel
    private GameObject theGameExitPanel    = null;   // game exit control panel

    private GameObject theReturnString     = null;   // disable display of string due to now including a highscore table!
    private GameObject theTitleString      = null;   // disable title string on exit
    private GameObject theGameController   = null;   // gameplay controller

    private Transform entryContainer;                    // container in GUI which holds high score entry templates / is used for position purposes
    private Transform entryTemplate;                     // high score entry template (for each highscore entry (position, score, name))
    private List<HighscoreEntry> highscoreEntryList;     // list of high score entries (score, name)
    private List<Transform> highscoreEntryTransformList; // list of transforms for display purposes

    private List<RectTransform> displayedObjects;    // transforms of the actual objects displayed on the screen

    private int maxHighScoresToDisplay     = 10;     // how many entries to display in table
    private int playerScore                = 0;      // used solely for storing score passed us from gamecontroller

    private GameObject confirmDeleteText;            // confirm deletion text
    private GameObject confirmDeleteBack;            // red confirm background
    private GameObject askHiscoreDelete;             // ask if user wants to delete 

    private GameObject enterNameText;                // enter username text on screen
    private GameObject enterNameBackground;          // enter username red background on screen
    
    private bool          deleteSelected   = false;  // delete not selected yet
    private bool bWaitingForUsernameInput  = false;  // should we only now process input from username field on GUI

    public TMP_InputField UsernameInputField;        // input field on gui for username entry (dragged in on editor)


    public AudioClip  theInstructionPanelSong;       // need to play instruction panel song again after exitting here

    // HighscoreEntry class represents an individual high score entry
    [System.Serializable]
    private class HighscoreEntry
    {
        public int    score; // player score (upto 999999)
        public string name;  // player name  (12 chars max)
    }

    // JSON utility loads entries back in from a serialised byte (text) format, and needs this to
    // be able to turn it back into a list again as only works on objects (can only contain standard data types within)
    private class Highscores
    {
        public List<HighscoreEntry> highscoreEntryList;
    }

    public void TurnOnAsk()
    {
        // turn on ask "press D to delete highscores" text, as can't be visible at first run
        askHiscoreDelete.SetActive(true);
    }

    // very early loading of high scores from PlayerPrefs
    private void Awake()
    {
        // turn off strings shouldn't display yet
        theReturnString = GameObject.FindGameObjectWithTag("HighScoreReturn");
        theTitleString  = GameObject.FindGameObjectWithTag("HighScoresTitle");

        // find objects not needed to be seen yet / needed for later use
        confirmDeleteText   = GameObject.Find("Confirm Delete Text");
        confirmDeleteBack   = GameObject.Find("Confirm Delete Background");
        askHiscoreDelete    = GameObject.Find("Ask Delete Text");
        enterNameText       = GameObject.Find("Enter Username Text");
        enterNameBackground = GameObject.Find("Enter Username Background");
        theGameController   = GameObject.Find("GameplayController");
        theGameExitPanel    = GameObject.Find("Game Exit Panel"); // needed as we can press Escape in here too

        // check if we found it - but don't disable here as Instruction panel does this
        if (!theGameExitPanel)
        {
            Debug.Log("Can't find Game Exit Panel from High Score Table Controller Panel Awake()");
        }

        // and turn them all off, username input is turned off in Start() as need to add listener there
        if (confirmDeleteText != null)
        {
            confirmDeleteText.gameObject.SetActive(false);
        }

        if (confirmDeleteBack != null)
        {
            confirmDeleteBack.gameObject.SetActive(false);
        }

        if (enterNameText != null)
        {
            enterNameText.gameObject.SetActive(false);
        }

        if ( enterNameBackground != null)
        {
            enterNameBackground.gameObject.SetActive(false);
        }
                
        theScoreLivesPanel = GameObject.Find("Score Lives Panel");

        if (theScoreLivesPanel == null)
        {
            // couldn't find Score lives panel from Credits Replay Panel
            Debug.Log("couldn't find Score Lives Panel from High Scores Panel.");
        }

        // Find the container and the entry template (which will be instantiated repeatedly later)
        // The container holds a copy of an entryTemplate for EACH highscore entry (position,score,name)
        // to be displayed within the container on the highscore gui panel
        entryContainer = transform.Find("highscoreEntryContainer");
        entryTemplate  = entryContainer.Find("highscoreEntryTemplate");

        // hide it at start
        entryTemplate.gameObject.SetActive(false);

        // create an empty list to store in - PJH
        displayedObjects = new List<RectTransform>();

        // load the high scores
        LoadHighScores();
    }

    public int GetHighScore()
    {
        // return the highest score in sorted list - the one at position 0
        if (highscoreEntryList != null)
        {
            return highscoreEntryList[0].score;
        }
        else
        {
            return 0;
        }
    }

    private void LoadHighScores()
    {
        // loads, sorts and creates a display list from the high scores in Playerprefs
        // if they exist, otherwise creates a default entry, saves it & reloads the table again
        // to ensure correct operation of this function in all cases

        string     jsonString = PlayerPrefs.GetString("highscoreTable");      // find score data from Playerprefs
        Highscores highscores = JsonUtility.FromJson<Highscores>(jsonString); // convert to a list of highscores

        if (!PlayerPrefs.HasKey("highscoreTable"))
        {
            // no high scores exist yet - so create a default entry
            highscores = new Highscores();

            highscores.highscoreEntryList = new List<HighscoreEntry>() { new HighscoreEntry { score = 10, name = "Zombie Killa" } };

            // save updated list
            string toJSON = JsonUtility.ToJson(highscores);
            PlayerPrefs.SetString("highscoreTable", toJSON);
            PlayerPrefs.Save();

            // get them again now
            jsonString = PlayerPrefs.GetString("highscoreTable");      // find score data from Playerprefs
            highscores = JsonUtility.FromJson<Highscores>(jsonString); // convert to a list of highscores
        }

        // do a simple bubble sort over all elements the list - sorted by descending score
        // could just have done List.Sort()?
        for (int i = 0; i < highscores.highscoreEntryList.Count; i++)
        {
            for (int j = i + 1; j < highscores.highscoreEntryList.Count; j++)
            {
                if (highscores.highscoreEntryList[j].score > highscores.highscoreEntryList[i].score)
                {
                    // swap them around if inner > outer - ie descending sort
                    HighscoreEntry temp = highscores.highscoreEntryList[i];
                    highscores.highscoreEntryList[i] = highscores.highscoreEntryList[j];
                    highscores.highscoreEntryList[j] = temp;
                }
            }
        }

        // truncate the highscores list to only use the maximum allowed in the display
        if (highscores.highscoreEntryList.Count > maxHighScoresToDisplay)
        {
            // remove the excess high scores from the sorted list
            highscores.highscoreEntryList.RemoveRange(maxHighScoresToDisplay, highscores.highscoreEntryList.Count - maxHighScoresToDisplay);

            // save truncated list
            string toJSON = JsonUtility.ToJson(highscores);
            PlayerPrefs.SetString("highscoreTable", toJSON);
            PlayerPrefs.Save();
        }

        // create a new high score entry list
        highscoreEntryList = new List<HighscoreEntry>();

        // store the newly sorted entries in here
        for (int l = 0; l < highscores.highscoreEntryList.Count; l++)
        {
            highscoreEntryList.Add(highscores.highscoreEntryList[l]);
        }

        int listCount = highscores.highscoreEntryList.Count;

        // create some blanks to get rid of any old displayed data
        if (listCount < maxHighScoresToDisplay)
        {
            for (int toAdd = 0; toAdd < maxHighScoresToDisplay - listCount; toAdd++)
            {
                // create some blanks for table - table won't display -99 score, but will check for it & replace with blank space
                highscores.highscoreEntryList.Add(new HighscoreEntry { score = -99, name = "            " });
                highscoreEntryList.Add(highscores.highscoreEntryList[toAdd]);
            }
        }

        // now create our gameobject transforms used to display the sorted list
        CreateDisplayTransformList(highscores);
    }

    void CreateDisplayTransformList(Highscores theList)
    {
        // create the display list (of gameobject transforms) from passed in list of highscore entries

        highscoreEntryTransformList = new List<Transform>(); // global variable holds list

        int howMany = theList.highscoreEntryList.Count;

        // create the display object for the table
        for (int i = 0; i < howMany; i++)
        {
            CreateHighscoreEntryTransform(theList.highscoreEntryList[i], entryContainer, highscoreEntryTransformList);
        }
    }

    // creates a new entry in our transform list from the passed in highscoreEntry object, and uses the passed container
    // to store it for display
    private void CreateHighscoreEntryTransform(HighscoreEntry highscoreEntry, Transform container, List<Transform> transformList)
    {
        float templateHeight = 20f;                                          // height of each entry in the displayed list
        Transform entryTransform = Instantiate(entryTemplate, container);        // create an entry with 'container' transform as parent
        RectTransform entryRectTransform = entryTransform.GetComponent<RectTransform>(); // get its rect transform for positioning inside container

        entryRectTransform.anchoredPosition = new Vector2(0f, -templateHeight * transformList.Count); //  set position at bottom of sorted list
        entryTransform.gameObject.SetActive(true); // set it active to display it

        // add this to our list for clearing display later - PJH
        displayedObjects.Add(entryRectTransform);

        int rank = transformList.Count + 1;
        string rankString;

        // set correct ending
        switch (rank)
        {
            case 1: rankString = "1ST"; break;
            case 2: rankString = "2ND"; break;
            case 3: rankString = "3RD"; break;
            default: rankString = rank + "TH"; break;
        }

        // set up contents of entry
        entryTransform.Find("rankText").GetComponent<TMP_Text>().SetText(rankString.ToString());

        if (highscoreEntry.score == -99)
        {
            // this is a blanking entry for table so just set spaces
            entryTransform.Find("scoreText").GetComponent<TMP_Text>().SetText("      ".ToString());
        }
        else
        {
            entryTransform.Find("scoreText").GetComponent<TMP_Text>().SetText(highscoreEntry.score.ToString());
        }

        entryTransform.Find("nameText").GetComponent<TMP_Text>().SetText(highscoreEntry.name.ToString());

        entryTransform.Find("background").gameObject.SetActive(rank % 2 == 1); // highlight every other one

        if (rank == 1)
        {
            // highlight highest score entry
            entryTransform.Find("rankText").GetComponent<TMP_Text>().color = Color.green;
            entryTransform.Find("scoreText").GetComponent<TMP_Text>().color = Color.green;
            entryTransform.Find("nameText").GetComponent<TMP_Text>().color = Color.green;
        }

        // setup the trophy images for the top 3
        switch (rank)
        {
            default:
                entryTransform.Find("trophy").gameObject.SetActive(false);
                break;
            case 1:
                entryTransform.Find("trophy").GetComponent<Image>().color = new Color32(255, 210, 0, 255); // gold
                break;
            case 2:
                entryTransform.Find("trophy").GetComponent<Image>().color = new Color32(198, 198, 198, 255); // silver
                break;
            case 3:
                entryTransform.Find("trophy").GetComponent<Image>().color = new Color32(183, 11, 86, 255); // bronze
                break;
        }

        // add the newly created object (whose parent transform is the display container) to the list
        transformList.Add(entryTransform);
    }


    // simply adds to the current list at the end - loading back in causes them to be sorted by Score
    // and to only display the top ten scores
    public void AddHighscoreEntry(int theScore, string theName)
    {
        // create a high score entry
        HighscoreEntry theHighscoreEntry = new HighscoreEntry { score = theScore, name = theName };

        // load the saved high scores
        string asJSONString = PlayerPrefs.GetString("highscoreTable");
        Highscores theHighScores = JsonUtility.FromJson<Highscores>(asJSONString);

        // add new entry to existing ones at bottom of sorted list
        theHighScores.highscoreEntryList.Add(theHighscoreEntry);

        // save updated list
        string toJSON = JsonUtility.ToJson(theHighScores);
        PlayerPrefs.SetString("highscoreTable", toJSON);
        PlayerPrefs.Save();

        // go through the old list of transforms and delete the old text mesh pro display objects here
        ClearDisplayedHighscores(); // pjh
        
        // now re-populate the highscore table with new entry just saved
        LoadHighScores();
    }

    public void AddHighscoreEntryWithName( int thePlayerScore)
    {
        // called from game controller, shows UI textinput control to get user name 
        // which itself then calls AddhighScoreEntry with name just input
        
        // re-enable stuff we need to see on screen as we want to enter a username
        enterNameText.gameObject.SetActive(true);
        enterNameBackground.SetActive(true);
        UsernameInputField.gameObject.SetActive(true);

        playerScore = thePlayerScore;

        // set bool to disable all other inputs apart from username input field stuff
        bWaitingForUsernameInput = true;
    }

    

    public void UsernameInputOnGUI(TMP_InputField theName)
    {
        // function called by "Enter Username" control on the screen (enabled by listener() in Start() )
        
        if (theName.text.Length > 0)
        {
            string toPass = theName.text;

            if (toPass.Length >12)
            {
                // trim it down
                toPass = toPass.Substring(0, 12);
            }

            AddHighscoreEntry(playerScore, toPass);
        }
        else if (theName.text.Length == 0)
        {
            AddHighscoreEntry(playerScore, "Anonymous");
        }

        // hide username input stuff now
        enterNameText.gameObject.SetActive(false);
        enterNameBackground.SetActive(false);
        UsernameInputField.gameObject.SetActive(false);
        
        // reset to day mode if we are in night mode at game end 

        if (thePlayer)
        {
            if (thePlayer.GetComponent<PlayerController>().IsNightMode())
            {
                // set to day mode
                thePlayer.GetComponent<PlayerController>().ToggleNightMode();
            }
        }

        // now allow other inputs on this panel again
        bWaitingForUsernameInput = false;

        // reset entry field
        TMP_InputField userName = UsernameInputField.gameObject.GetComponentInChildren<TMP_InputField>();
        if (userName != null)
        {
            userName.text = "            "; // 12 spaces
        }
    }

    private void DeleteAllHighScores()
    {
        // need to clear out the high score table here, as when reloading it will only create a single entry, leaving existing stuff there

        PlayerPrefs.DeleteKey("highscoreTable");
        PlayerPrefs.Save();

        // hide them again
        confirmDeleteBack.SetActive(false);
        confirmDeleteText.SetActive(false);

        // re-enable ask delete
        askHiscoreDelete.SetActive(true);
        deleteSelected = false; // reset flag checked in Update()

        ClearDisplayedHighscores(); // pjh

        // reload which will set them all to zero again apart from a single default entry
        LoadHighScores();
    }

    // clear the currently displayed table entries out
    private void ClearDisplayedHighscores()
    {
        // search saved array contents
        int count = displayedObjects.Count;

        for (int i = 0; i < count; i++)
        {
            displayedObjects[i].Find( "rankText").GetComponent<TMP_Text>().SetText("    ".ToString());         // 4 spaces
            displayedObjects[i].Find("scoreText").GetComponent<TMP_Text>().SetText("      ".ToString());       // 6 spaces
            displayedObjects[i].Find( "nameText").GetComponent<TMP_Text>().SetText("            ".ToString()); // 12 spaces

            displayedObjects[i].Find("background").gameObject.SetActive(false);

            // clear the trophy images for the top 3
            if (i == 1 || i == 2 || i == 3) 
            {
                displayedObjects[i].Find("trophy").gameObject.SetActive(false);
            }
        }
    }
        
    public bool GoodEnoughForHighscores(int thePlayerScore)
    {
        // check if the passed in score is high enough to go in table
        int i = displayedObjects.Count - 1; // check entries from end to beginning

        // check from end as a sorted list!
        displayedObjects[i].Find("scoreText").GetComponent<TMP_Text>().ForceMeshUpdate(); // ensure always has text to return if it has any
            
        string scoreString = displayedObjects[i].Find("scoreText").GetComponent<TMP_Text>().GetParsedText().ToString(); // removes special chars
        
        if (string.IsNullOrWhiteSpace(scoreString))
        {
            // blank string - so must be space available for the score
            return true;
        }

        // ok it's not null or spaces - so get score as int
        int scoreAsInt = int.Parse(scoreString); // convert to an integer

        if (thePlayerScore > scoreAsInt && thePlayerScore >0)
        {
            // ok its high enough to go in as higher than AN entry in there!
            return true;
        }
        else
        {
            return false;
        }
    }

    public void SetFocusToEntryField()
    {
        // set focus of cursor to entry field
        UsernameInputField.Select();
        UsernameInputField.ActivateInputField();
        GameObject.Find("Crosshair Target").SetActive(false);
    }

    public void ShowHighScoresPanel(bool showIt)
    {
        // simply display (or hide) the high scores panel & its entries
        // which was hidden at start in Awake()
        if (theReturnString)
        {
            // hide it for now by turning off text mesh pro ui stuff
            theReturnString.GetComponentInChildren<TMP_Text>().enabled = showIt;
        }

        if (theTitleString)
        {
            // hide it for now by turning off text mesh pro ui stuff
            theTitleString.GetComponentInChildren<TMP_Text>().enabled = showIt;
        }

        gameObject.SetActive(showIt);
        entryTemplate.gameObject.SetActive(showIt);
    }

    // Start is called before the first frame update
    void Start()
    {
        // find the Player as we need the camera child object
        thePlayer = GameObject.Find("Player");

        // Add a listener that will call the "UsernameInputOnGUI" function here when the player finishes entering 
        // the username on screen, passing the UsernameInputField contents to it
        UsernameInputField.onEndEdit.AddListener(delegate { UsernameInputOnGUI(UsernameInputField); });
        
        // disable for now
        UsernameInputField.gameObject.SetActive(false);
        

        theInstructionPanel = GameObject.Find("Instructions Panel");

        if (theInstructionPanel == null)
        {
            // couldn't find Instruction panel from Credits Replay Panel
            Debug.Log("couldn't find Instruction panel from High Scores Panel... Disabled too quickly elsewhere?");
        }

        if (thePlayer == null)
        {
            Debug.Log("Couldn't find player from within High Scores Panel - disabled already?");
        }
        else
        {
            // find camera to be enabled again later
            theMainCamera = thePlayer.GetComponentInChildren<Camera>();

            if (theMainCamera == null)
            {
                Debug.Log("Couldn't find Main Camera from within High Scores Panel");
            }
        }

        // turn off high score return text for now
        if (theReturnString)
        {
            // hide it for now by turning off text mesh pro ui stuff
            theReturnString.GetComponentInChildren<TMP_Text>().enabled = false;
        }

        if (askHiscoreDelete != null)
        {
            askHiscoreDelete.gameObject.SetActive(false);
        }
    }
    void ActivateGameExitPanel()
    {
        // Turn on Game exit panel (overlay on top of Instruction panel)
        theGameExitPanel.SetActive(true);
    }

    // Update is called every frame
    public void Update()
    {
        // always check for escape key regardless of where we are
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // activate Game exit panel, and disable this one
            bWaitingForUsernameInput = false;
            Debug.Log("Escape called in Highscore Panel");
            ActivateGameExitPanel();
        }

        if (!bWaitingForUsernameInput)
        {
            // we are not waiting for username to be input - so process other inputs
            // check for input here to return to Instructions Panel
            if (Input.GetKeyDown(KeyCode.R) && !deleteSelected)
            {
                // enable Instructions panel, and disable this one
                askHiscoreDelete.SetActive(false);

                // ok we have done everything we need to here, so relinquish control and
                // re-activate the Instructions Panel for a potential start game
                thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(false);
                ActivateInstructionsPanel();
            }

            if (Input.GetKeyDown(KeyCode.D) && !deleteSelected)
            {
                // deletion of highscores selected - so disable Ask Delete text and set bool flag 
                deleteSelected = true; // prevent selecting again for now

                // hide text prompts and enable other text
                askHiscoreDelete.SetActive(false);
                theReturnString.GetComponentInChildren<TMP_Text>().enabled = false;
                confirmDeleteText.SetActive(true);
                confirmDeleteBack.SetActive(true);
            }

            if (Input.GetKeyDown(KeyCode.Y) && deleteSelected)
            {
                // delete high scores confirmed - also resets flag
                DeleteAllHighScores();

                // update high score display
                LoadHighScores(); // will create a default entry of "100, Zombie Killa" in Playerprefs
                theReturnString.GetComponentInChildren<TMP_Text>().enabled = true; // turn on prompt for return to instructions panel
            }

            if (Input.GetKeyDown(KeyCode.N) && deleteSelected)
            {
                // turn off delete text & background, reenable original
                askHiscoreDelete.SetActive(true);
                confirmDeleteText.SetActive(false);
                confirmDeleteBack.SetActive(false);
                theReturnString.GetComponentInChildren<TMP_Text>().enabled = true;
                deleteSelected = false;
            }
        }
    }


    void ActivateInstructionsPanel()
    {
        // turn off highScores panel & text, turn off score panel, and re-activate instruction panel and re-start its main camera
        
        if (theReturnString)
        {
            // hide it
            theReturnString.GetComponentInChildren<TMP_Text>().enabled = false;
        }
        
        // re-enable user input in Player controller
        thePlayer.GetComponent<PlayerController>().SetAnotherPanelInControl(false);

        gameObject.SetActive(false);                    // turn off highscore panel
        theInstructionPanel.SetActive(true);            // set instruction panel active again
        theMainCamera.gameObject.SetActive(true);       // turn on main camera
        theScoreLivesPanel.gameObject.SetActive(false); // turn off score lives panel

        // enable audio again & play main instructions panel song
        AudioListener.pause = false;
        
        AudioSource theAudioSource = GetComponent<AudioSource>();

        if (theGameController != null)
        {
            GameplayController theScript = theGameController.GetComponent<GameplayController>();
            theScript.PlayCountdown(false);
            theScript.PlayCriticalCountdown(false);
        }

        
        theAudioSource.clip = theInstructionPanelSong;
        theAudioSource.time = 0f;
        theAudioSource.volume = 46;
        theAudioSource.enabled = true;
        theAudioSource.Play();
    }
}
