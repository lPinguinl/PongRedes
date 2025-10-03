using System.Collections;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class BallController : MonoBehaviourPun
{
    [Header("Movimiento")]
    [SerializeField] private float speed = 7.5f;
    [SerializeField] private float verticalLimit = 4.25f;
    [SerializeField] private float horizontalLimit = 8.5f;

    [Header("Rebotes")]
    [SerializeField] private float maxBounceAngle = 45f;

    private Vector2 direction;
    private bool movementEnabled = true;
    private Coroutine serveRoutine;

    private void Start()
    {
        GameManager.Instance.RegisterBall(this);

        if (PhotonNetwork.IsMasterClient)
        {
            InitializeDirection();
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterBall(this);
        }
    }

    private void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!movementEnabled) return;

        Vector3 displacement = (Vector3)(direction * speed * Time.deltaTime);
        transform.Translate(displacement, Space.World);

        CheckVerticalBounds();
        CheckHorizontalBounds();
    }

    private void InitializeDirection()
    {
        float x = Random.value < 0.5f ? -1f : 1f;
        float y = Random.Range(-0.35f, 0.35f);
        SetDirection(new Vector2(x, y).normalized);
    }

    private void CheckVerticalBounds()
    {
        float y = transform.position.y;
        if (y > verticalLimit || y < -verticalLimit)
        {
            direction.y = -direction.y;
            float clampedY = Mathf.Clamp(y, -verticalLimit, verticalLimit);
            Vector3 position = transform.position;
            position.y = clampedY;
            transform.position = position;
            photonView.RPC(nameof(RPC_SyncDirection), RpcTarget.Others, direction.x, direction.y);
        }
    }

    private void CheckHorizontalBounds()
    {
        float x = transform.position.x;
        if (x > horizontalLimit)
        {
            GameManager.Instance.ReportGoal(0);
        }
        else if (x < -horizontalLimit)
        {
            GameManager.Instance.ReportGoal(1);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PaddleController paddle = other.GetComponentInParent<PaddleController>();
        if (paddle == null) return;

        Vector3 paddlePos = paddle.transform.position;
        float relativeIntersectY = transform.position.y - paddlePos.y;
        float normalized = Mathf.Clamp(relativeIntersectY / (other.bounds.extents.y), -1f, 1f);

        float bounceAngle = normalized * maxBounceAngle * Mathf.Deg2Rad;
        float directionSign = Mathf.Sign(transform.position.x - paddlePos.x) * -1f;

        Vector2 newDir = new Vector2(
            Mathf.Cos(bounceAngle) * directionSign,
            Mathf.Sin(bounceAngle));

        SetDirection(newDir.normalized);
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;

        if (!enabled && serveRoutine != null)
        {
            StopCoroutine(serveRoutine);
            serveRoutine = null;
        }
    }

    public void PrepareNextServe(int scoringTeam)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (serveRoutine != null)
        {
            StopCoroutine(serveRoutine);
        }

        serveRoutine = StartCoroutine(ServeRoutine(scoringTeam));
    }

    private IEnumerator ServeRoutine(int scoringTeam)
    {
        movementEnabled = false;

        transform.position = Vector3.zero;
        photonView.RPC(nameof(RPC_SyncPosition), RpcTarget.Others, transform.position);

        float dirX = scoringTeam == 0 ? -1f : 1f;
        float dirY = Random.Range(-0.45f, 0.45f);
        SetDirection(new Vector2(dirX, dirY).normalized);

        yield return new WaitForSeconds(1f);

        serveRoutine = null;
        movementEnabled = GameManager.Instance.AllowBallMovement;
    }

    private void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
        photonView.RPC(nameof(RPC_SyncDirection), RpcTarget.Others, direction.x, direction.y);
    }

    [PunRPC]
    private void RPC_SyncDirection(float x, float y)
    {
        direction = new Vector2(x, y).normalized;
    }

    [PunRPC]
    private void RPC_SyncPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
    }
}