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
    [SerializeField] private TMP_Text readyButtonText;   // Text component of the ready button

    [Header("Text")]
    [SerializeField] private TMP_Text statusText;        // Main info text

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

        // Subscribe to role changes
        lobbyManager.SaboteurClientId.OnValueChanged += OnRoleChanged;
        lobbyManager.SeekerClientId.OnValueChanged += OnRoleChanged;

        // Subscribe to ready list changes
        lobbyManager.ReadyClients.OnListChanged += OnReadyListChanged;

        // Wire up buttons
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

        // Build status string
        string myRoleText = iAmSaboteur ? "Saboteur" : (iAmSeeker ? "Seeker" : "None");
        string sabText = saboteurTaken
            ? $"Saboteur: Player {lobbyManager.SaboteurClientId.Value}"
            : "Saboteur: Open";
        string seekText = seekerTaken
            ? $"Seeker: Player {lobbyManager.SeekerClientId.Value}"
            : "Seeker: Open";
        string readyStatus = iAmReady ? "Ready" : "Not Ready";

        statusText.text = $"Your role: {myRoleText}\n" +
                          $"{sabText}\n" +
                          $"{seekText}\n" +
                          $"You are: {readyStatus}";

        // Role button interactability
        sabotButton.interactable = !saboteurTaken && !iAmSaboteur;
        seekerButton.interactable = !seekerTaken && !iAmSeeker;

        // Ready button
        bool hasRole = iAmSaboteur || iAmSeeker;
        readyButton.interactable = hasRole;
        deselectButton.gameObject.SetActive(hasRole);      // only visible when you have a role
        deselectButton.interactable = hasRole;

        // Update ready button text and color
        if (readyButtonText != null)
        {
            readyButtonText.text = iAmReady ? "Unready" : "Ready";
        }
        // Optional: change button colors to indicate state
        var colors = readyButton.colors;
        colors.normalColor = iAmReady ? Color.green : Color.white;
        readyButton.colors = colors;
    }
}