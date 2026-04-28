using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;

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
        Debug.Log("[GameManager] Astronaut died! Slug wins!");
        EndGame(GameEndState.AstroDeath);
    }

    private void OnSlugDeath()
    {
        if (currentGameState != GameEndState.Active) return;
        Debug.Log("[GameManager] Slug died! Astronaut wins!");
        EndGame(GameEndState.AstronautWins);
    }

    private void OnTasksCompleted()
    {
        if (currentGameState != GameEndState.Active) return;

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

    private void EndGame(GameEndState endState)
    {
        if (currentGameState != GameEndState.Active) return;

        currentGameState = endState;
        OnGameEnd?.Invoke(endState);

        string sceneName = endState switch
        {
            GameEndState.AstronautWins => "AstroWin",
            GameEndState.AstroDeath => "SlugWin-Oxy",
            GameEndState.SlugTasks => "SlugWin-Tasks",
            _ => ""
        };

        Time.timeScale = 1f;

        // Notify clients FIRST before any despawning or shutdown
        if (GameManagerNetwork.Instance != null)
        {
            Debug.Log("[GameManager] Notifying clients of game end");
            GameManagerNetwork.Instance.NotifyClientsGameEnd(sceneName);
        }
        else
        {
            Debug.LogError("[GameManager] GameManagerNetwork.Instance is null!");
        }

        StartCoroutine(ServerShutdownAndLoad(sceneName));
    }

    private IEnumerator ServerShutdownAndLoad(string sceneName)
    {
        // Wait long enough for the RPC to reach and be processed by clients
        // before we start tearing down NetworkObjects
        Debug.Log("[GameManager] Server waiting before despawn...");
        yield return new WaitForSecondsRealtime(2f);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            Debug.Log("[GameManager] Server despawning all NetworkObjects");
            var spawnedObjects = new System.Collections.Generic.List<NetworkObject>(
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values
            );

            foreach (var netObj in spawnedObjects)
            {
                if (netObj != null)
                    netObj.Despawn(true);
            }
        }

        yield return null;

        Debug.Log("[GameManager] Server shutting down NetworkManager");
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        yield return new WaitUntil(() =>
            NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsListening
        );

        if (NetworkManager.Singleton != null)
            Destroy(NetworkManager.Singleton.gameObject);

        yield return null;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log($"[GameManager] Server loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    public GameEndState GetGameState() => currentGameState;
    public bool IsGameActive() => currentGameState == GameEndState.Active;
    public static GameManager Instance => instance;
}