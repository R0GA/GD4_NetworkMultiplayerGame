using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using System.Collections;

public class LobbyUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button sabotButton;
    [SerializeField] private Button seekerButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button deselectButton;
    [SerializeField] private TMP_Text readyButtonText;

    [Header("Text")]
    [SerializeField] private TMP_Text statusText; 

    private LobbyNetworkManager lobbyManager;
    private NetworkManager netManager;

    private void Start()
    {
        StartCoroutine(WaitForLobbyManager());
    }

    private IEnumerator WaitForLobbyManager()
    {
        while (lobbyManager == null)
        {
            lobbyManager = FindObjectOfType<LobbyNetworkManager>();
            yield return null;
        }

        netManager = NetworkManager.Singleton;

        lobbyManager.SaboteurClientId.OnValueChanged += OnRoleChanged;
        lobbyManager.SeekerClientId.OnValueChanged += OnRoleChanged;

        lobbyManager.ReadyClients.OnListChanged += OnReadyListChanged;

        sabotButton.onClick.AddListener(() => lobbyManager.RequestRoleServerRpc(RoleType.Saboteur));
        seekerButton.onClick.AddListener(() => lobbyManager.RequestRoleServerRpc(RoleType.Seeker));
        deselectButton.onClick.AddListener(() => lobbyManager.ClearMyRoleServerRpc());
        readyButton.onClick.AddListener(() => lobbyManager.ToggleReadyServerRpc());

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (lobbyManager != null)
        {
            lobbyManager.SaboteurClientId.OnValueChanged -= OnRoleChanged;
            lobbyManager.SeekerClientId.OnValueChanged -= OnRoleChanged;
            lobbyManager.ReadyClients.OnListChanged -= OnReadyListChanged;
        }
    }

    private void OnRoleChanged(ulong old, ulong current) => RefreshUI();
    private void OnReadyListChanged(NetworkListEvent<ulong> changeEvent) => RefreshUI();

    private void RefreshUI()
    {
        if (lobbyManager == null || netManager == null) return;

        ulong myId = netManager.LocalClientId;
        ulong UNASSIGNED = LobbyNetworkManager.UNASSIGNED;

        bool iAmSaboteur = lobbyManager.SaboteurClientId.Value == myId;
        bool iAmSeeker = lobbyManager.SeekerClientId.Value == myId;
        bool saboteurTaken = lobbyManager.SaboteurClientId.Value != UNASSIGNED;
        bool seekerTaken = lobbyManager.SeekerClientId.Value != UNASSIGNED;
        bool iAmReady = lobbyManager.ReadyClients.Contains(myId);

        string myRoleText = iAmSaboteur ? "Slug" : (iAmSeeker ? "Astronaut" : "None");
        string sabText = saboteurTaken
            ? $"Slug: Player {lobbyManager.SaboteurClientId.Value}"
            : "Slug: Open";
        string seekText = seekerTaken
            ? $"Astronaut: Player {lobbyManager.SeekerClientId.Value}"
            : "Astronaut: Open";
        string readyStatus = iAmReady ? "Ready" : "Not Ready";

        statusText.text = $"Your role: {myRoleText}\n" +
                          $"{sabText}\n" +
                          $"{seekText}\n" +
                          $"You are: {readyStatus}";

        sabotButton.interactable = !saboteurTaken && !iAmSaboteur;
        seekerButton.interactable = !seekerTaken && !iAmSeeker;

        bool hasRole = iAmSaboteur || iAmSeeker;
        readyButton.interactable = hasRole;
        deselectButton.gameObject.SetActive(hasRole);   
        deselectButton.interactable = hasRole;

        if (readyButtonText != null)
        {
            readyButtonText.text = iAmReady ? "Unready" : "Ready";
        }
        var colors = readyButton.colors;
        colors.normalColor = iAmReady ? Color.green : Color.white;
        readyButton.colors = colors;
    }
}