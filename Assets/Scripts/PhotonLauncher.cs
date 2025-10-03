using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PhotonLauncher : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    [SerializeField] private TMP_InputField roomNameInput;
    [SerializeField] private Button connectButton;
    [SerializeField] private TMP_Text statusLabel;

    private void Start()
    {
        connectButton.onClick.AddListener(OnConnectButtonPressed);
        connectButton.interactable = false;
        statusLabel.text = "Conectando a Photon...";
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.NickName = $"Player_{Random.Range(1000, 9999)}";
        PhotonNetwork.ConnectUsingSettings();
    }

    private void OnConnectButtonPressed()
    {
        string roomName = string.IsNullOrWhiteSpace(roomNameInput.text)
            ? "SalaPractica"
            : roomNameInput.text.Trim();

        connectButton.interactable = false;
        statusLabel.text = $"Uniéndose a \"{roomName}\"...";
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = 4,
            PlayerTtl = 0,
            EmptyRoomTtl = 0
        };
        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
    }

    public override void OnConnectedToMaster()
    {
        connectButton.interactable = true;
        statusLabel.text = "Conectado. Ingresa el nombre de la sala.";
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        connectButton.interactable = false;
        statusLabel.text = $"Desconectado: {cause}. Reintentando...";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnJoinedRoom()
    {
        statusLabel.text = $"En la sala \"{PhotonNetwork.CurrentRoom.Name}\"";
        PhotonNetwork.LoadLevel("Lobby");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        connectButton.interactable = true;
        statusLabel.text = $"No se pudo unir: {message}";
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        connectButton.interactable = true;
        statusLabel.text = $"No se pudo crear: {message}";
    }
}
