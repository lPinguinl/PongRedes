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
        PhotonNetwork.AutomaticallySyncScene = true;

        EnsureReadyFlag(PhotonNetwork.LocalPlayer);
        EnsureTeamForPlayer(PhotonNetwork.LocalPlayer);

        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(OnStartButtonPressed);
        startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        RebuildPlayerList();
        RefreshLobbyStatus();
        RefreshStartButtonState();
    }

    private void EnsureReadyFlag(Player player)
    {
        if (player.CustomProperties.ContainsKey("ready")) return;

        Hashtable props = new Hashtable { { "ready", false } };
        if (player == PhotonNetwork.LocalPlayer)
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
        else if (PhotonNetwork.IsMasterClient)
        {
            player.SetCustomProperties(props);
        }
    }

    private void EnsureTeamForPlayer(Player player)
    {
        if (ReadTeamIndex(player) != -1) return;

        int team = ChooseTeamForNextPlayer(player);

        Hashtable props = new Hashtable { { "team", team } };
        if (player == PhotonNetwork.LocalPlayer)
        {
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }
        else if (PhotonNetwork.IsMasterClient)
        {
            player.SetCustomProperties(props);
        }
    }

    private int ChooseTeamForNextPlayer(Player player)
    {
        int team0Count = PhotonNetwork.PlayerList.Count(p => ReadTeamIndex(p) == 0);
        int team1Count = PhotonNetwork.PlayerList.Count(p => ReadTeamIndex(p) == 1);

        // El nuevo jugador todavía tiene team = -1, así que no cuenta en los totales.
        // Asignamos al equipo con menos jugadores (desempate favorece al equipo 0).
        return team0Count <= team1Count ? 0 : 1;
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
        if (PhotonNetwork.IsMasterClient)
        {
            EnsureReadyFlag(newPlayer);
            EnsureTeamForPlayer(newPlayer);
        }

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
        if (PhotonNetwork.IsMasterClient)
        {
            EnsureTeamForPlayer(targetPlayer);
            EnsureReadyFlag(targetPlayer);
        }

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

        if (PhotonNetwork.IsMasterClient)
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                EnsureReadyFlag(player);
                EnsureTeamForPlayer(player);
            }
        }
    }
}