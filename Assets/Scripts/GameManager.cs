using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

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
    public UnityEvent<GameEndState> OnGameEnd = new UnityEvent<GameEndState>();
    private static GameManager instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() => InitializeGameManager();

    private void Update()
    {
        if (astronautPlayer == null || slugPlayer == null || taskManager == null)
            InitializeGameManager();
    }

    private void InitializeGameManager()
    {
        if (astronautPlayer == null)
            astronautPlayer = FindObjectOfType<NetworkFPSPlayer>();

        if (slugPlayer == null)
            slugPlayer = FindObjectOfType<SlugPlayer>();

        if (taskManager == null)
            taskManager = FindObjectOfType<TaskManager>();

        if (astronautPlayer != null)
        {
            OxygenManager oxygenManager = astronautPlayer.GetComponent<OxygenManager>();
            if (oxygenManager != null)
                oxygenManager.OnDeath.AddListener(OnAstronautDeath);
            else
                Debug.LogWarning("[GameManager] OxygenManager not found on Astronaut player!");
        }
        else
        {
            Debug.LogWarning("[GameManager] NetworkFPSPlayer not found in scene!");
        }

        if (slugPlayer != null)
        {
            NetworkHealth slugHealth = slugPlayer.GetComponent<NetworkHealth>();
            if (slugHealth != null)
                slugHealth.OnDeath.AddListener(OnSlugDeath);
            else
                Debug.LogWarning("[GameManager] NetworkHealth not found on Slug player!");
        }
        else
        {
            Debug.LogWarning("[GameManager] SlugPlayer not found in scene!");
        }

        if (taskManager != null)
            taskManager.OnAllTasksCompleted.AddListener(OnTasksCompleted);
        else
            Debug.LogWarning("[GameManager] TaskManager not found in scene!");
    }

    private void OnAstronautDeath()
    {
        if (currentGameState != GameEndState.Active) return;
        EndGame(GameEndState.AstroDeath);
    }

    private void OnSlugDeath()
    {
        if (currentGameState != GameEndState.Active) return;
        EndGame(GameEndState.AstronautWins);
    }

    private void OnTasksCompleted()
    {
        if (currentGameState != GameEndState.Active) return;

        if (astronautPlayer != null)
        {
            OxygenManager oxygenManager = astronautPlayer.GetComponent<OxygenManager>();
            if (oxygenManager != null && !oxygenManager.IsDead)
                EndGame(GameEndState.SlugTasks);
        }
    }

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

        Debug.Log($"[GameManager] Game ending, loading {sceneName} for ALL clients via NGO");

        // This single call loads the scene on EVERY connected client simultaneously
        // No RPCs, no NetworkVariables needed — NGO handles it natively
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public GameEndState GetGameState() => currentGameState;
    public bool IsGameActive() => currentGameState == GameEndState.Active;
    public static GameManager Instance => instance;
}