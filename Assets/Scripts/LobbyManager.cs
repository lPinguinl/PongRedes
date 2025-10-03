using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ExitGames.Client.Photon;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    [SerializeField] private Transform playerListRoot;
    [SerializeField] private GameObject playerEntryPrefab;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private Button startButton;

    private readonly Dictionary<int, LobbyPlayerEntry> playerEntries = new();

    private void Start()
    {
        EnsureLocalCustomProperties();
        startButton.onClick.AddListener(OnStartButtonPressed);
        startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        RebuildPlayerList();
        RefreshLobbyStatus();
        RefreshStartButtonState();
    }

    private void EnsureLocalCustomProperties()
    {
        Hashtable pendingChanges = new Hashtable();

        if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("team", out _))
        {
            int index = System.Array.IndexOf(PhotonNetwork.PlayerList, PhotonNetwork.LocalPlayer);
            pendingChanges.Add("team", index % 2);
        }

        if (!PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("ready", out _))
        {
            pendingChanges.Add("ready", false);
        }

        if (pendingChanges.Count > 0)
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(pendingChanges);
        }
    }

    private void RebuildPlayerList()
    {
        foreach (Transform child in playerListRoot)
        {
            Destroy(child.gameObject);
        }

        playerEntries.Clear();

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject entryGO = Instantiate(playerEntryPrefab, playerListRoot);
            LobbyPlayerEntry entry = entryGO.GetComponent<LobbyPlayerEntry>();

            bool isLocal = player == PhotonNetwork.LocalPlayer;
            entry.SetData(player, isLocal);
            playerEntries[player.ActorNumber] = entry;
        }
    }

    private void RefreshLobbyStatus()
    {
        int team0 = PhotonNetwork.PlayerList.Count(p => ReadTeamIndex(p) == 0);
        int team1 = PhotonNetwork.PlayerList.Count(p => ReadTeamIndex(p) == 1);
        int ready = PhotonNetwork.PlayerList.Count(p => ReadReadyFlag(p));
        int total = PhotonNetwork.PlayerList.Length;

        statusLabel.text = $"Equipo 1: {team0} | Equipo 2: {team1} · Listos: {ready}/{total}";
    }

    private void RefreshStartButtonState()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            startButton.interactable = false;
            return;
        }

        bool teamsReady = HasAtLeastOnePlayerPerTeam();
        bool allReady = AreAllPlayersReady();

        startButton.interactable = teamsReady && allReady;
    }

    private bool HasAtLeastOnePlayerPerTeam()
    {
        bool team0 = PhotonNetwork.PlayerList.Any(p => ReadTeamIndex(p) == 0);
        bool team1 = PhotonNetwork.PlayerList.Any(p => ReadTeamIndex(p) == 1);
        return team0 && team1;
    }

    private bool AreAllPlayersReady()
    {
        return PhotonNetwork.PlayerList.All(ReadReadyFlag);
    }

    private static int ReadTeamIndex(Player player)
    {
        if (player.CustomProperties.TryGetValue("team", out object teamObj) && teamObj is int team)
        {
            return team;
        }

        return -1;
    }

    private static bool ReadReadyFlag(Player player)
    {
        return player.CustomProperties.TryGetValue("ready", out object readyObj) &&
               readyObj is bool ready &&
               ready;
    }

    private void OnStartButtonPressed()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!HasAtLeastOnePlayerPerTeam()) return;
        if (!AreAllPlayersReady()) return;

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.LoadLevel("Game");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        RebuildPlayerList();
        RefreshLobbyStatus();
        RefreshStartButtonState();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RebuildPlayerList();
        RefreshLobbyStatus();
        RefreshStartButtonState();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (playerEntries.TryGetValue(targetPlayer.ActorNumber, out LobbyPlayerEntry entry))
        {
            entry.ApplyPlayerProperties(targetPlayer);
        }

        RefreshLobbyStatus();
        RefreshStartButtonState();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        RefreshStartButtonState();
    }
}