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

    [Header("Saboteur Character Images")]
    [Tooltip("Shown when no one has selected this character")]
    [SerializeField] private GameObject sabotDefaultImage;
    [Tooltip("Shown when the OTHER player has selected this character")]
    [SerializeField] private GameObject sabotNotReadyImage;
    [Tooltip("Shown when YOU have selected this character")]
    [SerializeField] private GameObject sabotSelectedImage;

    [Header("Seeker Character Images")]
    [Tooltip("Shown when no one has selected this character")]
    [SerializeField] private GameObject seekerDefaultImage;
    [Tooltip("Shown when the OTHER player has selected this character")]
    [SerializeField] private GameObject seekerNotReadyImage;
    [Tooltip("Shown when YOU have selected this character")]
    [SerializeField] private GameObject seekerSelectedImage;

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
            ? $"Slug: <b>Player {lobbyManager.SaboteurClientId.Value + 1}</b>"
            : "Slug: <b>Open</b>";
        string seekText = seekerTaken
            ? $"Astronaut: <b>Player {lobbyManager.SeekerClientId.Value + 1}</b>"
            : "Astronaut: <b>Open</b>";
        string readyStatus = iAmReady ? "Ready" : "Not Ready";

        statusText.text = $"Your role: <b>{myRoleText}</b>     You are: <b>{readyStatus}\n</b>" +
                  $"{sabText}     {seekText}";

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

        // --- Character image states ---
        // Saboteur images
        bool otherPlayerIsSaboteur = saboteurTaken && !iAmSaboteur;
        SetCharacterImageState(sabotDefaultImage, sabotNotReadyImage, sabotSelectedImage,
            iAmSaboteur, otherPlayerIsSaboteur);

        // Seeker images
        bool otherPlayerIsSeeker = seekerTaken && !iAmSeeker;
        SetCharacterImageState(seekerDefaultImage, seekerNotReadyImage, seekerSelectedImage,
            iAmSeeker, otherPlayerIsSeeker);
    }

    /// <summary>
    /// Activates exactly one of the three state images for a character slot.
    /// </summary>
    /// <param name="defaultImg">Shown when the slot is unclaimed.</param>
    /// <param name="notReadyImg">Shown when the OTHER player has claimed this slot.</param>
    /// <param name="selectedImg">Shown when the LOCAL player has claimed this slot.</param>
    /// <param name="iSelected">True if the local player owns this slot.</param>
    /// <param name="otherSelected">True if the remote player owns this slot.</param>
    private void SetCharacterImageState(
        GameObject defaultImg, GameObject notReadyImg, GameObject selectedImg,
        bool iSelected, bool otherSelected)
    {
        if (defaultImg) defaultImg.SetActive(!iSelected && !otherSelected);
        if (notReadyImg) notReadyImg.SetActive(otherSelected);
        if (selectedImg) selectedImg.SetActive(iSelected);
    }
}