using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages game win conditions and end state for the multiplayer game.
/// 
/// Win Conditions:
/// - Astronaut wins: Slug player (SlugPlayer) health reaches 0
/// - Slug wins: Astronaut oxygen reaches 0 OR all sabotage tasks are completed
/// </summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] private NetworkFPSPlayer astronautPlayer;
    [SerializeField] private SlugPlayer slugPlayer;
    [SerializeField] private TaskManager taskManager;

    public enum GameEndState
    {
        Active,
        AstronautWins,
        SlugTasks,
        AstroDeath
    }

    private GameEndState currentGameState = GameEndState.Active;

    // Event triggered when game ends with win state
    public UnityEvent<GameEndState> OnGameEnd = new UnityEvent<GameEndState>();

    private static GameManager instance;

    private void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeGameManager();
    }

    private void InitializeGameManager()
    {
        // Find components if not assigned in inspector
        if (astronautPlayer == null)
            astronautPlayer = FindObjectOfType<NetworkFPSPlayer>();

        if (slugPlayer == null)
            slugPlayer = FindObjectOfType<SlugPlayer>();

        if (taskManager == null)
            taskManager = FindObjectOfType<TaskManager>();

        // Subscribe to astronaut death (oxygen depletion)
        if (astronautPlayer != null)
        {
            OxygenManager oxygenManager = astronautPlayer.GetComponent<OxygenManager>();
            if (oxygenManager != null)
            {
                oxygenManager.OnDeath.AddListener(OnAstronautDeath);
            }
            else
            {
                Debug.LogWarning("[GameManager] OxygenManager not found on Astronaut player!");
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] NetworkFPSPlayer not found in scene!");
        }

        // Subscribe to slug death (health depletion)
        if (slugPlayer != null)
        {
            NetworkHealth slugHealth = slugPlayer.GetComponent<NetworkHealth>();
            if (slugHealth != null)
            {
                slugHealth.OnDeath.AddListener(OnSlugDeath);
            }
            else
            {
                Debug.LogWarning("[GameManager] NetworkHealth not found on Slug player!");
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] SlugPlayer not found in scene!");
        }

        // Subscribe to task completion
        if (taskManager != null)
        {
            taskManager.OnAllTasksCompleted.AddListener(OnTasksCompleted);
        }
        else
        {
            Debug.LogWarning("[GameManager] TaskManager not found in scene!");
        }
    }

    /// <summary>
    /// Called when the astronaut dies (oxygen reaches 0).
    /// Slug wins immediately.
    /// </summary>
    private void OnAstronautDeath()
    {
        if (currentGameState != GameEndState.Active)
            return;

        Debug.Log("[GameManager] Astronaut died! Slug wins!");
        EndGame(GameEndState.AstroDeath);
    }

    /// <summary>
    /// Called when the slug dies (health reaches 0).
    /// Astronaut wins immediately.
    /// </summary>
    private void OnSlugDeath()
    {
        if (currentGameState != GameEndState.Active)
            return;

        Debug.Log("[GameManager] Slug died! Astronaut wins!");
        EndGame(GameEndState.AstronautWins);
    }

    /// <summary>
    /// Called when all sabotage tasks are completed.
    /// Slug wins if astronaut is still alive.
    /// </summary>
    private void OnTasksCompleted()
    {
        if (currentGameState != GameEndState.Active)
            return;

        // Check if astronaut is still alive
        if (astronautPlayer != null)
        {
            OxygenManager oxygenManager = astronautPlayer.GetComponent<OxygenManager>();
            if (oxygenManager != null && !oxygenManager.IsDead)
            {
                Debug.Log("[GameManager] All tasks completed! Slug wins!");
                EndGame(GameEndState.SlugTasks);
            }
        }
    }

    /// <summary>
    /// Ends the game with the given end state.
    /// Triggers OnGameEnd event and handles cleanup/UI.
    /// </summary>
    private void EndGame(GameEndState endState)
    {
        if (currentGameState != GameEndState.Active)
            return;

        currentGameState = endState;

        // Trigger event for UI or scene management
        OnGameEnd?.Invoke(endState);

        // Log game result
        switch (endState)
        {
            case GameEndState.AstronautWins:
                Debug.Log("[GameManager] ===== ASTRONAUT WINS! =====");
                NetworkManager.Singleton.SceneManager.LoadScene("AstroWin", LoadSceneMode.Single);
                break;
            case GameEndState.AstroDeath:
                Debug.Log("[GameManager] ===== ASTRO DIED SLUG WINS! =====");
                NetworkManager.Singleton.SceneManager.LoadScene("SlugWin-Oxy", LoadSceneMode.Single);
                break;
            case GameEndState.SlugTasks:
                Debug.Log("[GameManager] ===== SLUG WINS BY TASKS! =====");
                NetworkManager.Singleton.SceneManager.LoadScene("SlugWin-Tasks", LoadSceneMode.Single);
                break;
        }

        // Optional: Pause the game
         Time.timeScale = 0f;
    }

    /// <summary>
    /// Gets the current game state.
    /// </summary>
    public GameEndState GetGameState() => currentGameState;

    /// <summary>
    /// Returns true if the game is still active.
    /// </summary>
    public bool IsGameActive() => currentGameState == GameEndState.Active;

    public static GameManager Instance => instance;
}
