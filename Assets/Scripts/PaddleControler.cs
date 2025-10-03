using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class PaddleController : MonoBehaviourPun
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float minY = -3.75f;
    [SerializeField] private float maxY = 3.75f;

    [Header("Render")]
    [SerializeField] private Renderer paddleRenderer;
    [SerializeField] private Color[] availableColors;

    public int OwnerActorNumber { get; private set; }
    public int TeamIndex { get; private set; }

    private bool initialized;
    private Color appliedColor;

    private void Awake()
    {
        if (paddleRenderer == null)
        {
            paddleRenderer = GetComponentInChildren<Renderer>();
        }
    }

    private void Start()
    {
        if (!initialized && photonView.IsMine)
        {
            Debug.LogWarning($"Paddle {name} no recibió datos de inicialización.");
        }

        GameManager.Instance.RegisterPaddle(this);
        ApplyMovementAllowance(GameManager.Instance.AllowPlayerInput);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterPaddle(this);
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        GameManager.Instance.RegisterPaddle(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterPaddle(this);
        }
    }

    public override void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = info.photonView.InstantiationData;
        if (data == null || data.Length < 2)
        {
            Debug.LogError("Paddle sin datos de InstantiationData.");
            return;
        }

        OwnerActorNumber = (int)data[0];
        TeamIndex = (int)data[1];
        initialized = true;

        ApplyTeamColor();
    }

    private void Update()
    {
        if (!photonView.IsMine) return;
        if (GameManager.Instance == null) return;
        if (!GameManager.Instance.AllowPlayerInput) return;

        float input = Input.GetAxisRaw("Vertical");
        float delta = input * moveSpeed * Time.deltaTime;

        if (Mathf.Approximately(delta, 0f)) return;

        Vector3 displacement = new Vector3(0f, delta, 0f);
        transform.Translate(displacement, Space.World);

        Vector3 position = transform.position;
        position.y = Mathf.Clamp(position.y, minY, maxY);
        transform.position = position;
    }

    private void ApplyTeamColor()
    {
        if (paddleRenderer == null || availableColors == null || availableColors.Length == 0)
        {
            return;
        }

        Color chosen = availableColors[Mathf.Clamp(TeamIndex, 0, availableColors.Length - 1)];

        if (photonView.InstantiationData != null && photonView.InstantiationData.Length >= 3)
        {
            int colorIndex = (int)photonView.InstantiationData[2];
            if (colorIndex >= 0 && colorIndex < availableColors.Length)
            {
                chosen = availableColors[colorIndex];
            }
        }

        appliedColor = chosen;
        paddleRenderer.material.color = chosen;
    }

    private void ApplyMovementAllowance(bool canMove)
    {
        // Este método queda preparado para aplicar visuales extra si se desea (por ahora no hace nada extra).
    }
}