using UnityEngine;

/// <summary>
/// Триггер-коробка. Игрок касается → экран затемняется → игрок переносится в точку назначения → экран осветляется.
/// Один и тот же скрипт и для входа в комнату, и для возврата в холл — отличается только точка назначения.
/// Нужен BoxCollider2D с включённым Is Trigger (добавляется и включается сам при добавлении скрипта).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class RoomTeleportTrigger : MonoBehaviour
{
    [Header("Куда переносим")]
    [Tooltip("Точка, в которую попадёт игрок: пустой объект в нужной комнате или в холле")]
    [SerializeField] private Transform destination;

    [Header("Затемнение")]
    [Tooltip("Скорость затемнения: 1 = за 1 сек, 2 = за 0.5 сек, 0.5 = за 2 сек")]
    [SerializeField] private float fadeOutSpeed = 2f;

    [Tooltip("Скорость осветления после переноса")]
    [SerializeField] private float fadeInSpeed = 2f;

    [Header("Кто срабатывает")]
    [Tooltip("Слой игрока")]
    [SerializeField] private LayerMask playerLayer;

    private void Reset()
    {
        if (TryGetComponent(out Collider2D col)) col.isTrigger = true;
        playerLayer = LayerMask.GetMask("Player");
    }

    private void Awake()
    {
        if (TryGetComponent(out Collider2D col) && !col.isTrigger)
            Debug.LogWarning($"[{name}] У коллайдера не включено Is Trigger — телепорт не сработает.", this);

        if (playerLayer.value == 0)
            Debug.LogWarning($"[{name}] Не выбран слой игрока — телепорт не сработает.", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (ScreenFader.IsBusy || !IsPlayer(other)) return;

        if (destination == null)
        {
            Debug.LogWarning($"[{name}] Не задана точка назначения.", this);
            return;
        }

        Transform player = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
        ScreenFader.Transition(fadeOutSpeed, fadeInSpeed, () => Teleport(player));
    }

    private bool IsPlayer(Collider2D other)
    {
        if (InPlayerLayer(other.gameObject)) return true;

        Rigidbody2D body = other.attachedRigidbody;
        return body != null && InPlayerLayer(body.gameObject);
    }

    private bool InPlayerLayer(GameObject go)
    {
        return (playerLayer.value & (1 << go.layer)) != 0;
    }

    // Вызывается на полностью чёрном экране.
    private void Teleport(Transform player)
    {
        if (player == null || destination == null) return;

        Vector3 pos = destination.position;
        pos.z = player.position.z;
        player.position = pos;

        if (player.TryGetComponent(out Rigidbody2D rb))
        {
            rb.position = pos;
            rb.angularVelocity = 0f;
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
        }

        Physics2D.SyncTransforms();

        // Камера не должна «лететь» через весь уровень к новой комнате.
        if (CameraFollow2D.Instance != null) CameraFollow2D.Instance.SnapToTarget();
    }

    private void OnDrawGizmos()
    {
        if (destination == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, destination.position);
        Gizmos.DrawWireSphere(destination.position, 0.3f);
    }
}
