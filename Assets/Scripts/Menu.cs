using TMPro;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine;

public class Menu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField ipInput;
    [SerializeField] private TMP_InputField portInput;

    [Header("Defaults")]
    [SerializeField] private string defaultIp = "127.0.0.1";
    [SerializeField] private ushort defaultPort = 7777;

    [SerializeField] private UnityTransport transport;
    [SerializeField] private NetworkManager networkManager;

    private void Awake()
    {
        if (ipInput) ipInput.text = defaultIp;
        if (portInput) portInput.text = defaultPort.ToString();
    }

    public void StartHost()
    {
        ushort port = GetPort();
        transport.SetConnectionData("0.0.0.0", port);

        networkManager.StartHost();
        networkManager.SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
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
