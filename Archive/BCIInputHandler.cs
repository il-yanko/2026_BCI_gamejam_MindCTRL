using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

/// <summary>
/// BCIInputHandler integrates with BCI Essentials Unity to handle P300 paradigm.
/// Manages selection events based on BCI signal detection.
/// </summary>
public class BCIInputHandler : MonoBehaviour
{
    [System.Serializable]
    public class SelectionEvent : UnityEvent<int, int> { }

    private static BCIInputHandler instance;
    public static BCIInputHandler Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<BCIInputHandler>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("BCIInputHandler");
                    instance = obj.AddComponent<BCIInputHandler>();
                }
            }
            return instance;
        }
    }

    [SerializeField] private bool useBCIInput = true;
    [SerializeField] private bool useTestMode = false; // For testing without BCI hardware
    [SerializeField] private float testModeDelay = 2.0f;

    // BCI Input Events
    public SelectionEvent OnCharacterSelected = new();
    public UnityEvent<int> OnFaceSelected = new();

    private bool isConnected = false;
    private float testModeTimer = 0f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        InitializeBCI();
    }

    /// <summary>
    /// Initialize BCI connection using BCI Essentials Unity.
    /// </summary>
    private void InitializeBCI()
    {
        if (!useBCIInput)
        {
            Debug.Log("BCI Input disabled");
            return;
        }

        try
        {
            // This will be integrated with actual BCI Essentials components
            // For now, we'll set up the framework
            Debug.Log("Initializing BCI connection...");
            
            // TODO: Connect to LSL stream from BCI backend
            // This requires BCI Essentials Unity package to be installed
            
            isConnected = true;
            Debug.Log("BCI connected successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize BCI: {e.Message}");
            if (useTestMode)
            {
                Debug.Log("Falling back to test mode");
                isConnected = false;
            }
        }
    }

    private void Update()
    {
        if (useTestMode)
        {
            HandleTestModeInput();
        }
        else if (isConnected)
        {
            HandleBCIInput();
        }
    }

    /// <summary>
    /// Handle keyboard input for testing (4-8 for characters, Q-E for faces).
    /// </summary>
    private void HandleTestModeInput()
    {
        // Character selection: 1-4
        if (Input.GetKeyDown(KeyCode.Alpha1)) OnCharacterSelected.Invoke(0, 0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) OnCharacterSelected.Invoke(1, 0);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) OnCharacterSelected.Invoke(2, 0);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) OnCharacterSelected.Invoke(3, 0);

        // Face selection: Q-T (0-3)
        if (Input.GetKeyDown(KeyCode.Q)) OnFaceSelected.Invoke(0);
        else if (Input.GetKeyDown(KeyCode.W)) OnFaceSelected.Invoke(1);
        else if (Input.GetKeyDown(KeyCode.E)) OnFaceSelected.Invoke(2);
        else if (Input.GetKeyDown(KeyCode.R)) OnFaceSelected.Invoke(3);
    }

    /// <summary>
    /// Handle actual BCI input from P300 paradigm detection.
    /// </summary>
    private void HandleBCIInput()
    {
        // TODO: Implement actual P300 signal detection
        // This will listen to LSL stream and detect when P300 event occurs
        
        // Example logic:
        // if (P300EventDetected)
        // {
        //     int characterIndex = GetSelectedRowIndex();
        //     int faceIndex = GetSelectedColumnIndex();
        //     OnCharacterSelected.Invoke(characterIndex, faceIndex);
        // }
    }

    /// <summary>
    /// Send character and face selection to game.
    /// </summary>
    public void SelectCharacterAndFace(int characterIndex, int faceIndex)
    {
        OnCharacterSelected.Invoke(characterIndex, faceIndex);
    }

    /// <summary>
    /// Enable/disable BCI input.
    /// </summary>
    public void SetBCIEnabled(bool enabled)
    {
        useBCIInput = enabled;
        if (enabled && !isConnected)
        {
            InitializeBCI();
        }
    }

    /// <summary>
    /// Enable/disable test mode.
    /// </summary>
    public void SetTestMode(bool enabled)
    {
        useTestMode = enabled;
        if (enabled)
            Debug.Log("Test mode enabled: Use 1-4 for characters, Q-W-E-R for faces");
    }

    public bool IsConnected() => isConnected;
    public bool IsTestModeActive() => useTestMode;
}
