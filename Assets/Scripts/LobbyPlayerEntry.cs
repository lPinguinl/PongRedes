using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ExitGames.Client.Photon;

public class LobbyPlayerEntry : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameLabel;
    [SerializeField] private TMP_Text teamLabel;
    [SerializeField] private Toggle readyToggle;

    private int ownerActorNumber;
    private bool suppressToggleEvent;

    public void SetData(Player player, bool isLocalPlayer)
    {
        ownerActorNumber = player.ActorNumber;
        playerNameLabel.text = player.NickName;
        readyToggle.onValueChanged.RemoveAllListeners();
        readyToggle.interactable = isLocalPlayer;

        if (isLocalPlayer)
        {
            readyToggle.onValueChanged.AddListener(OnLocalReadyToggleChanged);
        }

        ApplyPlayerProperties(player);
    }

    public void ApplyPlayerProperties(Player player)
    {
        teamLabel.text = TryBuildTeamLabel(player);
        bool isReady = TryReadReadyFlag(player);

        suppressToggleEvent = true;
        readyToggle.isOn = isReady;
        suppressToggleEvent = false;
    }

    private void OnLocalReadyToggleChanged(bool isOn)
    {
        if (suppressToggleEvent) return;

        Hashtable props = new Hashtable
        {
            { "ready", isOn }
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    private static string TryBuildTeamLabel(Player player)
    {
        if (player.CustomProperties.TryGetValue("team", out object teamObj) && teamObj is int teamIndex)
        {
            return teamIndex == 0 ? "Equipo 1" : "Equipo 2";
        }

        return "Sin equipo";
    }

    private static bool TryReadReadyFlag(Player player)
    {
        return player.CustomProperties.TryGetValue("ready", out object readyObj) &&
               readyObj is bool readyValue &&
               readyValue;
    }
}
