using TMPro;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine;

public class Menu : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject connectionPanel;
    [SerializeField] private GameObject lobbyPanel;

    [Header("Connection Inputs")]
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private TMP_InputField portInput;

    [Header("Defaults")]
    [SerializeField] private string defaultIp = "127.0.0.1";
    [SerializeField] private ushort defaultPort = 7777;

    [Header("Networking")]
    [SerializeField] private UnityTransport transport;
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private GameObject lobbyManagerPrefab;

    private void Awake()
    {
        if (ipInput) ipInput.text = defaultIp;
        if (portInput) portInput.text = defaultPort.ToString();

        if (connectionPanel) connectionPanel.SetActive(true);
        if (lobbyPanel) lobbyPanel.SetActive(false);

        networkManager.OnServerStarted += OnServerStarted;
        networkManager.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDestroy()
    {
        if (networkManager)
        {
            networkManager.OnServerStarted -= OnServerStarted;
            networkManager.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    public void StartHost()
    {
        ushort port = GetPort();
        transport.SetConnectionData("0.0.0.0", port);
        networkManager.StartHost();
    }

    public void JoinGame()
    {
        string ip = GetIp();
        ushort port = GetPort();
        transport.SetConnectionData(ip, port);
        networkManager.StartClient();
    }

    public void StartServer()
    {
        ushort port = GetPort();
        transport.SetConnectionData("0.0.0.0", port);
        networkManager.StartServer();
    }

    private void OnServerStarted()
    {
        if (lobbyManagerPrefab)
        {
            GameObject lobbyObj = Instantiate(lobbyManagerPrefab);
            lobbyObj.GetComponent<NetworkObject>().Spawn();
            DontDestroyOnLoad(lobbyObj);
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == networkManager.LocalClientId)
        {
            SwitchToLobbyUI();
        }
    }

    private void SwitchToLobbyUI()
    {
        if (connectionPanel) connectionPanel.SetActive(false);
        if (lobbyPanel) lobbyPanel.SetActive(true);
    }
    private string GetIp()
    {
        if (!ipInput || string.IsNullOrWhiteSpace(ipInput.text)) 
            return defaultIp;

        return ipInput.text;
    }
    private ushort GetPort()
    {
        if (!portInput || !ushort.TryParse(portInput.text, out ushort port)) 
            return defaultPort;

        return port;
    }

}
