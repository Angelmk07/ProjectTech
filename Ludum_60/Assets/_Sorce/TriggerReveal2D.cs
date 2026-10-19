using UnityEngine;

/// <summary>
/// Повесь на объект с Collider2D (Is Trigger = true).
/// Игрок вошёл в зону -> объект включается.
/// Игрок вышел из зоны -> объект уничтожается / отключается.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TriggerReveal2D : MonoBehaviour
{
    [Tooltip("Объект на сцене, который включается в зоне.")]
    [SerializeField] private GameObject target;

    [Tooltip("Слой игрока.")]
    [SerializeField] private LayerMask playerLayer;

    [Tooltip("Сработать только один раз.")]
    [SerializeField] private bool onlyOnce = true;

    [Tooltip("Уничтожать объект (Destroy) вместо отключения при выходе.")]
    [SerializeField] private bool destroyOnExit = true;

    private bool used;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        if (target != null)
            target.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (used && onlyOnce) return;
        if (target == null) return;

        target.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        if (target == null) return;

        if (destroyOnExit) Destroy(target);
        else target.SetActive(false);

        used = true;
    }

    private bool IsPlayer(Collider2D other)
    {
        return (playerLayer.value & (1 << other.gameObject.layer)) != 0;
    }
}