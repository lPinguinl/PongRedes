using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    [Header("Referencias")]
    [SerializeField] private ScoreBoardUI scoreboard;
    [SerializeField] private Transform[] team0SpawnPoints;
    [SerializeField] private Transform[] team1SpawnPoints;
    [SerializeField] private GameObject paddlePrefab;
    [SerializeField] private GameObject ballPrefab;

    private readonly Dictionary<int, PaddleController> paddlesByActor = new();
    private BallController currentBall;

    private int nextSpawnIndexTeam0;
    private int nextSpawnIndexTeam1;
    private int scoreTeam0;
    private int scoreTeam1;
    private bool matchEnded;
    private bool gamePaused;

    public bool AllowPlayerInput => !matchEnded && !gamePaused;
    public bool AllowBallMovement => !matchEnded && !gamePaused;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        scoreboard.UpdateScore(0, 0);
        scoreboard.ShowPause(false, string.Empty);
        scoreboard.ShowEnd(false, string.Empty);

        if (PhotonNetwork.IsMasterClient)
        {
            SpawnAllPaddles();
            SpawnBall();
        }
    }

    private void SpawnAllPaddles()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            SpawnPaddleForPlayer(player);
        }
    }

    private void SpawnPaddleForPlayer(Player player)
    {
        int teamIndex = ReadTeamIndex(player);
        Transform spawnPoint = teamIndex == 0
            ? team0SpawnPoints[nextSpawnIndexTeam0++ % team0SpawnPoints.Length]
            : team1SpawnPoints[nextSpawnIndexTeam1++ % team1SpawnPoints.Length];

        object[] instantiateData = { player.ActorNumber, teamIndex };

        GameObject paddleGO = PhotonNetwork.InstantiateRoomObject(
            paddlePrefab.name,
            spawnPoint.position,
            spawnPoint.rotation,
            0,
            instantiateData);

        PhotonView view = paddleGO.GetComponent<PhotonView>();
        if (view != null && view.OwnerActorNr != player.ActorNumber)
        {
            view.TransferOwnership(player);
        }
    }

    private void SpawnBall()
    {
        PhotonNetwork.InstantiateRoomObject(ballPrefab.name, Vector3.zero, Quaternion.identity);
    }

    public void RegisterPaddle(PaddleController controller)
    {
        int actorNumber = controller.OwnerActorNumber;
        paddlesByActor[actorNumber] = controller;
    }

    public void UnregisterPaddle(PaddleController controller)
    {
        paddlesByActor.Remove(controller.OwnerActorNumber);
    }

    public void RegisterBall(BallController ball)
    {
        currentBall = ball;
        UpdateBallMovementState();
    }

    public void UnregisterBall(BallController ball)
    {
        if (currentBall == ball)
        {
            currentBall = null;
        }
    }

    public void ReportGoal(int scoringTeam)
    {
        if (matchEnded) return;

        if (scoringTeam == 0)
        {
            scoreTeam0++;
        }
        else
        {
            scoreTeam1++;
        }

        scoreboard.UpdateScore(scoreTeam0, scoreTeam1);

        if (scoreTeam0 >= 5 || scoreTeam1 >= 5)
        {
            matchEnded = true;
            UpdateBallMovementState();
            string message = scoringTeam == 0 ? "¡Ganó el Equipo 1!" : "¡Ganó el Equipo 2!";
            scoreboard.ShowEnd(true, message);
        }
        else
        {
            if (PhotonNetwork.IsMasterClient && currentBall != null)
            {
                currentBall.PrepareNextServe(scoringTeam);
            }
        }
    }

    public void SetPaused(bool paused, string message)
    {
        gamePaused = paused;
        scoreboard.ShowPause(paused, message);
        UpdateBallMovementState();
    }

    private void UpdateBallMovementState()
    {
        if (currentBall == null) return;
        currentBall.SetMovementEnabled(AllowBallMovement && !matchEnded);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        SetPaused(true, $"{otherPlayer.NickName} se desconectó");
    }

    private static int ReadTeamIndex(Player player)
    {
        if (player.CustomProperties.TryGetValue("team", out object teamObj) && teamObj is int teamIndex)
        {
            return Mathf.Clamp(teamIndex, 0, 1);
        }

        return 0;
    }
}