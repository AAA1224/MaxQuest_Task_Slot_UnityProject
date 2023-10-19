using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using UnitySocketIO.Events;
using Herman;
using System;
using DG.Tweening;
using UnityEngine.UI; // Import the UI namespace
using TMPro; // Make sure to include the TMPro namespace

public class SlotManager : MonoBehaviour
{
    public static SlotManager instance;

    #region websocket
    [SerializeField]
    private ConnectionManager connections; // Websocket connect manager
    #endregion websocket 

    #region options
    [SerializeField]
    private GameObject connectingPanel; // Block screen if websocket is connected
    [SerializeField]
    private GameObject slotSymbolPrefab; // Reference to a prefab containing SlotSymbol component
    [SerializeField]
    private Button spinButton; // Reference to your spin button in the Unity Inspector
    [SerializeField]
    private GameObject freespinsWindow;
    [SerializeField]
    private TMP_Text freespinsCount;
    [SerializeField]
    private float spinDuration = 3.0f; // Duration of the spin animation
    [SerializeField]
    private int minSteps = 30; // Minimum number of steps to move
    [SerializeField]
    private int maxSteps = 40; // Maximum number of steps to move
    [SerializeField]
    private float reelDelay = 1.0f; // Delay between starting each reel spin
    [SerializeField]
    private Transform[] reels; // References to the parent GameObjects for each reel
    [SerializeField]
    private Sprite[] symbolSprites; // Define the sprite array for your symbols
    [SerializeField]
    private LineRenderer[] lineRenderers; // Reference to the LineRenderer components
    #endregion options

    #region temp vars
    private List<Vector3[]> linesToDraw = new List<Vector3[]>();
    private List<Transform>[] symbolsOnReels; // List of symbols on each reel
    private float stepSize;
    private List<LineRenderer> activeLineRenderers = new List<LineRenderer>();
    private List<GameObject> displayedGameObjects = new List<GameObject>();
    private List<string>[] wordGroups = new List<string>[3];
    private SoundManager soundManager;  // Sounds manager (spinSound, winSound)
    #endregion temp vars

    #region initialize

    private void Awake()
    {

        instance = this;
    }
    private void Init()
    {
        // Initialize the list of symbols on each reel
        symbolsOnReels = new List<Transform>[reels.Length];

        for (int i = 0; i < reels.Length; i++)
        {
            symbolsOnReels[i] = new List<Transform>();

            // Populate the list with the symbols (child objects) of the reel
            foreach (Transform child in reels[i])
            {
                symbolsOnReels[i].Add(child);
            }
        }

        stepSize = 3.0f;
    }
    // Start is called before the first frame update
    void Start()
    {
        // Get a reference to the SoundManager instance
        soundManager = SoundManager.instance;
        connections = ConnectionManager.Instance;
        if (Global.createdSocket == false)
        {
            connections.EnterServer();
            Global.createdSocket = true;

        }
        else
        {
            OnSocketConnected();

        }


    }
    #endregion initialize

    #region websocketresponse
    /// <summary>
    /// Called when the socket connection is established. Handles necessary setup and data retrieval.
    /// </summary>
    public void OnSocketConnected()
    {
        // Hide the connecting panel when the socket is connected
        connectingPanel.SetActive(false);

        // Register event handlers for socket events
        connections.socket.On(Constant.getRandomReelValues, OnGetRandomReelValues);
        connections.socket.On(Constant.checkResultWithPayline, OnCheckResultWithPayline);

        // Request random reel values from the server
        GetReelRandomValues();
    }
    /// <summary>
    /// Handles the server response for getting random reel values and initializes the game symbols.
    /// </summary>
    /// <param name="evt">The SocketIOEvent containing the response data.</param>
    /// 
    public void OnGetRandomReelValues(SocketIOEvent evt)
    {
        //       Debug.Log(evt.name + evt.data);
        JsonData jsonObject = JsonMapper.ToObject(evt.data);

        wordGroups[0] = new List<string>();
        wordGroups[1] = new List<string>();
        wordGroups[2] = new List<string>();

        JsonData data = jsonObject["data"];

        if (data != null)
        {
            // Assuming "reel1", "reel2", and "reel3" are arrays in your JSON data
            if (data["reel1"] != null)
            {
                foreach (JsonData item in data["reel1"])
                {
                    wordGroups[0].Add(item.ToString());
                }
            }

            if (data["reel2"] != null)
            {
                foreach (JsonData item in data["reel2"])
                {
                    wordGroups[1].Add(item.ToString());
                }
            }

            if (data["reel3"] != null)
            {
                foreach (JsonData item in data["reel3"])
                {
                    wordGroups[2].Add(item.ToString());
                }
            }
        }

        // Iterate through the reels and assign word groups
        for (int i = 0; i < reels.Length; i++)
        {
            Transform reel = reels[i];
            List<string> wordGroup = wordGroups[i];

            // Initialize Y position
            float yPos = 36.0f;

            // Initialize X position based on reel index
            float xPos = (i - 1) * 3.0f;

            // Iterate through the word group and create GameObjects
            foreach (string word in wordGroup)
            {
                GameObject newSlotSymbol = Instantiate(slotSymbolPrefab, reel); // Create a new GameObject as a child of the reel

                // Set the position of the new GameObject
                newSlotSymbol.transform.localPosition = new Vector3(0, yPos, 0);

                // Set the SpriteRenderer's sprite based on the word
                SpriteRenderer spriteRenderer = newSlotSymbol.GetComponent<SpriteRenderer>();

                // Find the sprite by name in the symbolSprites array
                Sprite sprite = Array.Find(symbolSprites, s => s.name == word);

                if (sprite != null)
                {
                    spriteRenderer.sprite = sprite;
                }
                else
                {
                    // Handle the case where the sprite for the word is not found.
                    Debug.LogWarning("Sprite for word not found: " + word);
                }

                // Set the SlotSymbol's symbolName based on the word
                SlotSymbol slotSymbol = newSlotSymbol.GetComponent<SlotSymbol>();
                slotSymbol.symbolName = word;

                // Decrease Y position for the next GameObject
                yPos -= 3.0f;
            }
        }
        Init();

    }

    private void AddLineIfRule(bool rule, params Vector3[] positions)
    {
        if (rule)
        {
            linesToDraw.Add(positions);
        }
    }
    /// <summary>
    /// Handles the server response for checking results with paylines. Parses payline rules and triggers animations.
    /// </summary>
    /// <param name="evt">The SocketIOEvent containing the response data.</param>
    public void OnCheckResultWithPayline(SocketIOEvent evt)
    {
        Debug.Log(evt.name + evt.data);
        JsonData jsonObject = JsonMapper.ToObject(evt.data);
        JsonData data = jsonObject["data"];
        if (data != null)
        {
            JsonData paylines = data["paylines"];
            JsonData freeSpinCount = data["freeSpinCount"];

            // Handle paylines data
            bool rule1 = (bool)paylines["rule1"];
            bool rule2 = (bool)paylines["rule2"];
            bool rule3 = (bool)paylines["rule3"];
            bool rule4 = (bool)paylines["rule4"];
            bool rule5 = (bool)paylines["rule5"];
            bool rule6 = (bool)paylines["rule6"];
            bool rule7 = (bool)paylines["rule7"];

            // Inside your OnCheckResultWithPayline function:
            AddLineIfRule(rule1, new Vector3[] { new Vector3(-3, 0, 0), new Vector3(0, 0, 0), new Vector3(3, 0, 0) });
            AddLineIfRule(rule2, new Vector3[] { new Vector3(-3, 3, 0), new Vector3(0, 3, 0), new Vector3(3, 3, 0) });
            AddLineIfRule(rule3, new Vector3[] { new Vector3(-3, -3, 0), new Vector3(0, -3, 0), new Vector3(3, -3, 0) });
            AddLineIfRule(rule4, new Vector3[] { new Vector3(-3, 3, 0), new Vector3(0, 0, 0), new Vector3(3, 3, 0) });
            AddLineIfRule(rule5, new Vector3[] { new Vector3(-3, -3, 0), new Vector3(0, 0, 0), new Vector3(3, -3, 0) });
            AddLineIfRule(rule6, new Vector3[] { new Vector3(-3, 0, 0), new Vector3(0, 3, 0), new Vector3(3, 0, 0) });
            AddLineIfRule(rule7, new Vector3[] { new Vector3(-3, 0, 0), new Vector3(0, -3, 0), new Vector3(3, 0, 0) });

            if (paylines["rule8"] != null && paylines["rule8"].IsArray)
            {
                JsonData rule8Data = paylines["rule8"];
                List<int> rule8 = new List<int>();

                // Create a dictionary to map intValue to the index of displayedGameObjects
                Dictionary<int, int> intValueToIndex = new Dictionary<int, int>();

                for (int i = 0; i < displayedGameObjects.Count; i++)
                {
                    intValueToIndex[i] = i;
                }

                foreach (JsonData item in rule8Data)
                {
                    // Parse each element in "rule8" as an integer
                    if (int.TryParse(item.ToString(), out int intValue))
                    {
                        // Check if intValue is in the dictionary
                        if (intValueToIndex.ContainsKey(intValue))
                        {
                            int index1 = intValueToIndex[intValue];
                            displayedGameObjects[index1].GetComponent<Animator>().enabled = true;
                        }
                    }
                }


            }
            // Draw all lines accumulated in the list using separate LineRenderer components
            int index = 0;
            foreach (Vector3[] positions in linesToDraw)
            {
                if (index < lineRenderers.Length)
                {
                    Debug.Log("shows_line_positions" + positions);
                    DrawLine(lineRenderers[index], positions);
                }
                index++;
            }

            if ((int)freeSpinCount > 0)
            {
                freespinsCount.text = freeSpinCount.ToString();
                freespinsWindow.SetActive(true);
                soundManager.PlaySound("win_freespinds_sound");
                spinButton.interactable = false;

            }
            else
            {
                spinButton.interactable = true;
            }

        }

    }

    #endregion websocketresponse

    #region payline

    /// <summary>
    /// Sends the slot results to the server for checking against paylines.
    /// </summary>
    /// <param name="slotResult">A list of symbols representing the current slot result.</param>

    private void CheckPayLine(List<string> slotResult)
    {
        Dictionary<string, List<string>> data = new Dictionary<string, List<string>>();
        data["slotResult"] = slotResult;
        connections.socket.Emit(Constant.checkResultWithPayline, JsonMapper.ToJson(data));

    }

    /// <summary>
    /// Draws a line using the given LineRenderer component and specified positions.
    /// </summary>
    /// <param name="lineRenderer">The LineRenderer component to draw the line with.</param>
    /// <param name="positions">An array of Vector3 positions that define the points of the line.</param>
    /// 
    private void DrawLine(LineRenderer lineRenderer, params Vector3[] positions)
    {
        lineRenderer.positionCount = positions.Length;
        lineRenderer.SetPositions(positions);

        activeLineRenderers.Add(lineRenderer);
    }


    /// <summary>
    /// Disables the animation of displayed game objects.
    /// </summary>
    private void DisableAnimation()
    {
        foreach (GameObject obj in displayedGameObjects)
        {
            obj.GetComponent<Animator>().enabled = false;
        }
    }

    /// <summary>
    /// Clears any active LineRenderer components and the linesToDraw list, effectively hiding any drawn lines.
    /// </summary>
    private void ClearLines()
    {
        // Disable and destroy any active LineRenderer components
        foreach (var lineRenderer in activeLineRenderers)
        {
            lineRenderer.positionCount = 0;

        }

        // Clear the linesToDraw list
        linesToDraw.Clear();
    }


    #endregion payline

    #region spinreel
    /// <summary>
    /// Sends a request to the server to get random reel values.
    /// </summary>
    public void GetReelRandomValues()
    {
        // Prepare an empty data dictionary
        Dictionary<string, string> data = new Dictionary<string, string>();

        // Emit the request to the server with JSON data
        connections.socket.Emit(Constant.getRandomReelValues, JsonMapper.ToJson(data));
    }



    /// <summary>
    /// Initiates the spinning of all three reels, triggering animations and symbol movement.
    /// </summary>
    public void SpinReels()
    {
        soundManager.PlaySound("spin_sound");

        ClearLines();
        DisableAnimation();
        displayedGameObjects.Clear();
        // Disable the spin button during the spin to prevent multiple spins
        spinButton.interactable = false;

        // Create a coroutine to move all symbols by one step
        IEnumerator MoveReelSymbolsCoroutine(int reelIndex)
        {
            // Generate a random number of steps to move
            int randomSteps = UnityEngine.Random.Range(minSteps, maxSteps + 1);

            // Calculate the time it takes to move one step
            if (reelIndex == 0)
            {
                spinDuration = 3.0f;
            }
            else if (reelIndex == 1)
            {
                spinDuration = 4.0f;
            }
            else if (reelIndex == 2)
            {
                spinDuration = 5.0f;
            }
            float stepDuration = spinDuration / randomSteps;

            while (randomSteps > 0)
            {
                // Move all symbols on the specified reel by one step with the correct step size
                if (randomSteps < 6 && randomSteps > 1)
                {
                    stepDuration += 0.1f;
                }
                else if (randomSteps == 1)
                {
                    stepDuration += 0.8f;
                }
                foreach (Transform symbol in symbolsOnReels[reelIndex])
                {
                    symbol.DOLocalMoveY(symbol.localPosition.y - stepSize, stepDuration)
                        .SetEase(Ease.Linear);
                }

                yield return new WaitForSeconds(stepDuration); // Adjust the duration as needed
                                                               // Check if any symbol has reached the bottom
                foreach (Transform symbol in symbolsOnReels[reelIndex])
                {
                    if (symbol.localPosition.y <= -39.0f)
                    {
                        // Wrap around to the top position
                        symbol.localPosition = new Vector3(symbol.localPosition.x, 36.0f, symbol.localPosition.z);
                    }
                }

                // Continue moving symbols on the specified reel
                randomSteps--;
            }
            // Get the displayed symbols at specific positions after spinning
            List<string> displayedSymbols = new List<string>();



            // Enable the spin button when the spin is complete for this reel
            if (reelIndex == reels.Length - 1)
            {
                for (int i = 0; i < reels.Length; i++)
                {
                    foreach (Transform symbol in symbolsOnReels[i])
                    {
                        if (i == 0)
                        {
                            if ((symbol.localPosition.y == 3 || symbol.localPosition.y == 0 || symbol.localPosition.y == -3) && reels[i].localPosition.x == -3)
                            {
                                displayedSymbols.Add(symbol.GetComponent<SlotSymbol>().symbolName);
                                displayedGameObjects.Add(symbol.gameObject);
                            }
                        }
                        else if (i == 1)
                        {
                            if ((symbol.localPosition.y == 3 || symbol.localPosition.y == 0 || symbol.localPosition.y == -3) && reels[i].localPosition.x == 0)
                            {
                                displayedSymbols.Add(symbol.GetComponent<SlotSymbol>().symbolName);
                                displayedGameObjects.Add(symbol.gameObject);
                            }
                        }
                        else if (i == 2)
                        {
                            if ((symbol.localPosition.y == 3 || symbol.localPosition.y == 0 || symbol.localPosition.y == -3) && reels[i].localPosition.x == 3)
                            {
                                displayedSymbols.Add(symbol.GetComponent<SlotSymbol>().symbolName);
                                displayedGameObjects.Add(symbol.gameObject);
                            }
                        }


                    }
                }
                CheckPayLine(displayedSymbols);

                //spinButton.interactable = true; // Enable the button
            }

        }

        // Start spinning all three reels at the same time
        for (int i = 0; i < reels.Length; i++)
        {
            StartCoroutine(MoveReelSymbolsCoroutine(i));
        }
    }

    #endregion spinreel

   

    /// <summary>
    /// Unsubscribes from the "getRandomReelValues" event when the script is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        connections.socket.Off(Constant.getRandomReelValues, OnGetRandomReelValues);

    }

}
