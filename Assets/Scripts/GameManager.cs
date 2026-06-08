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

    public enum GameEndState { Active, AstronautWins, SlugTasks, AstroDeath }

    private GameEndState currentGameState = GameEndState.Active;
    public UnityEvent<GameEndState> OnGameEnd = new();

    // Populated at runtime once players spawn (see RegisterPlayer / RegisterTaskManager).
    private NetworkFPSPlayer astronautPlayer;
    private SlugPlayer slugPlayer;
    private TaskManager taskManager;


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
        if (!NetworkManager.Singleton.IsServer)
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