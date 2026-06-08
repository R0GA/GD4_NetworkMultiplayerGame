using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Tracks win/loss conditions. Because players are spawned dynamically by
/// GamePlayerSpawner we cannot serialise references in the Inspector — instead
/// each player prefab calls GameManager.Instance.RegisterPlayer() on spawn.
/// </summary>
public class GameManager : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Singleton
    // -------------------------------------------------------------------------

    private static GameManager instance;
    public static GameManager Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    public enum GameEndState { Active, AstronautWins, SlugTasks, AstroDeath }

    private GameEndState currentGameState = GameEndState.Active;
    public UnityEvent<GameEndState> OnGameEnd = new();

    // Populated at runtime once players spawn (see RegisterPlayer / RegisterTaskManager).
    private NetworkFPSPlayer astronautPlayer;
    private SlugPlayer slugPlayer;
    private TaskManager taskManager;

    // -------------------------------------------------------------------------
    // Registration API — called by each player prefab on OnNetworkSpawn
    // -------------------------------------------------------------------------

    public void RegisterAstronaut(NetworkFPSPlayer player)
    {
        if (astronautPlayer == player) return;
        astronautPlayer = player;

        var oxy = player.GetComponent<OxygenManager>();
        if (oxy != null)
            oxy.OnDeath.AddListener(OnAstronautDeath);
        else
            Debug.LogWarning("[GameManager] OxygenManager not found on astronaut player!");

        Debug.Log("[GameManager] Astronaut registered.");
    }

    public void RegisterSlug(SlugPlayer player)
    {
        if (slugPlayer == player) return;
        slugPlayer = player;

        var health = player.GetComponent<NetworkHealth>();
        if (health != null)
            health.OnDeath.AddListener(OnSlugDeath);
        else
            Debug.LogWarning("[GameManager] NetworkHealth not found on slug player!");

        Debug.Log("[GameManager] Slug registered.");
    }

    public void RegisterTaskManager(TaskManager manager)
    {
        if (taskManager == manager) return;
        taskManager = manager;
        taskManager.OnAllTasksCompleted.AddListener(OnTasksCompleted);
        Debug.Log("[GameManager] TaskManager registered.");
    }

    // -------------------------------------------------------------------------
    // Win condition callbacks
    // -------------------------------------------------------------------------

    private void OnAstronautDeath()
    {
        if (currentGameState != GameEndState.Active) return;
        EndGame(GameEndState.AstroDeath);
    }

    private void OnSlugDeath()
    {
        Debug.Log("[GameManager] Slug died.");
        if (currentGameState != GameEndState.Active) return;
        EndGame(GameEndState.AstronautWins);
    }

    private void OnTasksCompleted()
    {
        if (currentGameState != GameEndState.Active) return;

        // Only counts as a slug win if the astronaut is still alive.
        var oxy = astronautPlayer != null ? astronautPlayer.GetComponent<OxygenManager>() : null;
        if (oxy == null || !oxy.IsDead)
            EndGame(GameEndState.SlugTasks);
    }

    // -------------------------------------------------------------------------
    // End game
    // -------------------------------------------------------------------------

    private void EndGame(GameEndState endState)
    {
        if (currentGameState != GameEndState.Active) return;

        currentGameState = endState;
        Time.timeScale = 1f;
        OnGameEnd?.Invoke(endState);

        string sceneName = endState switch
        {
            GameEndState.AstronautWins => "AstroWin",
            GameEndState.AstroDeath => "SlugWin-Oxy",
            GameEndState.SlugTasks => "SlugWin-Tasks",
            _ => ""
        };

        if (string.IsNullOrEmpty(sceneName)) return;

        Debug.Log($"[GameManager] Ending game → {sceneName}");
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    // -------------------------------------------------------------------------
    // Utilities
    // -------------------------------------------------------------------------

    public GameEndState GetGameState() => currentGameState;
    public bool IsGameActive() => currentGameState == GameEndState.Active;
}