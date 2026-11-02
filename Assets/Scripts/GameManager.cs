using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{

    private static GameManager instance;
    public static GameManager Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public enum GameEndState { Active, AstronautWins, SlugTasks, AstroDeath }

    private GameEndState currentGameState = GameEndState.Active;
    public UnityEvent<GameEndState> OnGameEnd = new();

    // Populated at runtime once players spawn (see RegisterPlayer / RegisterTaskManager).
    private NetworkFPSPlayer astronautPlayer;
    private SlugPlayer slugPlayer;
    private TaskManager taskManager;


    /// <summary>
    /// Puts the manager back into a clean "match about to start" state.
    ///
    /// matchOver = false : called when a game scene spawns. Only resets state; it
    ///                     deliberately leaves the TaskManager reference and the
    ///                     OnGameEnd listeners alone, because the new scene's
    ///                     objects may already have registered / subscribed.
    /// matchOver = true  : called from the end scene. Also drops the (now destroyed)
    ///                     TaskManager and clears OnGameEnd listeners that belong to
    ///                     the previous game scene's destroyed objects.
    /// </summary>
    public void ResetForNewMatch(bool matchOver = false)
    {
        currentGameState = GameEndState.Active;
        astronautPlayer = null;
        slugPlayer = null;
        Time.timeScale = 1f;

        if (matchOver)
        {
            if (taskManager != null)
                taskManager.OnAllTasksCompleted.RemoveListener(OnTasksCompleted);
            taskManager = null;
            OnGameEnd.RemoveAllListeners();
        }

        Debug.Log($"[GameManager] State reset (matchOver={matchOver}).");
    }


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

        if (taskManager != null)
            taskManager.OnAllTasksCompleted.RemoveListener(OnTasksCompleted);

        taskManager = manager;
        taskManager.OnAllTasksCompleted.AddListener(OnTasksCompleted);
        Debug.Log("[GameManager] TaskManager registered.");
    }


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

        var oxy = astronautPlayer != null ? astronautPlayer.GetComponent<OxygenManager>() : null;
        if (oxy == null || !oxy.IsDead.Value)
            EndGame(GameEndState.SlugTasks);
    }

    private void EndGame(GameEndState endState)
    {
        if (currentGameState != GameEndState.Active) return;

        // LoadScene must only be called from the server
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[GameManager] EndGame called on client — ignoring.");
            return;
        }

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

    public GameEndState GetGameState() => currentGameState;
    public bool IsGameActive() => currentGameState == GameEndState.Active;
}
