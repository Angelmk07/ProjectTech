using UnityEngine;

/// <summary>
/// Плавная камера для 2D: следование, мёртвая зона, заглядывание вперёд, границы уровня.
/// Повесить на камеру (Orthographic).
/// </summary>
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(100)]
public class CameraFollow2D : MonoBehaviour
{
    public static CameraFollow2D Instance { get; private set; }

    [Header("Цель")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = new Vector2(0f, 1f);
    [SerializeField] private float cameraZ = -10f;

    [Header("Сглаживание")]
    [SerializeField] private float smoothTime = 0.15f;

    [Header("Мёртвая зона (камера не двигается, пока игрок внутри)")]
    [SerializeField] private Vector2 deadZone = new Vector2(0.5f, 0.3f);

    [Header("Заглядывание вперёд")]
    [SerializeField] private bool useLookAhead = true;
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float lookAheadSmoothTime = 0.3f;
    [SerializeField] private float lookAheadMinSpeed = 0.5f;

    [Header("Границы уровня (мировые координаты)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Rect levelBounds = new Rect(-50f, -20f, 100f, 40f);

    private Camera cam;
    private Vector3 velocity;
    private float lookAheadX;
    private float lookAheadVelocity;
    private Vector3 lastTargetPos;

    private void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (target != null)
        {
            lastTargetPos = target.position;
            SnapToTarget();
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        UpdateLookAhead();

        Vector2 camPos = transform.position;
        Vector2 focus = (Vector2)target.position + offset + new Vector2(lookAheadX, 0f);
        Vector2 diff = focus - camPos;

        Vector2 move = Vector2.zero;
        if (Mathf.Abs(diff.x) > deadZone.x) move.x = diff.x - Mathf.Sign(diff.x) * deadZone.x;
        if (Mathf.Abs(diff.y) > deadZone.y) move.y = diff.y - Mathf.Sign(diff.y) * deadZone.y;

        Vector3 goal = new Vector3(camPos.x + move.x, camPos.y + move.y, cameraZ);
        goal = ClampToBounds(goal);

        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }

    private void UpdateLookAhead()
    {
        if (!useLookAhead)
        {
            lookAheadX = 0f;
            return;
        }

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float speedX = (target.position.x - lastTargetPos.x) / dt;
        lastTargetPos = target.position;

        float goalX = Mathf.Abs(speedX) > lookAheadMinSpeed
            ? Mathf.Sign(speedX) * lookAheadDistance
            : 0f;

        lookAheadX = Mathf.SmoothDamp(lookAheadX, goalX, ref lookAheadVelocity, lookAheadSmoothTime);
    }

    private Vector3 ClampToBounds(Vector3 pos)
    {
        if (!useBounds || !cam.orthographic) return pos;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        pos.x = levelBounds.width <= halfW * 2f
            ? levelBounds.center.x
            : Mathf.Clamp(pos.x, levelBounds.xMin + halfW, levelBounds.xMax - halfW);

        pos.y = levelBounds.height <= halfH * 2f
            ? levelBounds.center.y
            : Mathf.Clamp(pos.y, levelBounds.yMin + halfH, levelBounds.yMax - halfH);

        return pos;
    }

    /// <summary>Сменить цель (например, при смене персонажа).</summary>
    public void SetTarget(Transform newTarget, bool snap = false)
    {
        target = newTarget;
        if (target != null) lastTargetPos = target.position;
        if (snap) SnapToTarget();
    }

    /// <summary>Мгновенно поставить камеру на игрока (после телепорта, лестницы, загрузки уровня).</summary>
    public void SnapToTarget()
    {
        if (target == null) return;

        velocity = Vector3.zero;
        lookAheadX = 0f;
        lookAheadVelocity = 0f;
        lastTargetPos = target.position;

        Vector3 pos = new Vector3(target.position.x + offset.x, target.position.y + offset.y, cameraZ);
        transform.position = ClampToBounds(pos);
    }

    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(levelBounds.center, levelBounds.size);
    }
}
