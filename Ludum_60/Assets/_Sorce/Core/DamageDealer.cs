using UnityEngine;

/// <summary>
/// Повесьте на шипы, врагов, снаряды, зону-пропасть.
/// Нужен Collider2D (триггер или обычный). Цель определяется только по слою.
/// </summary>
public class DamageDealer : MonoBehaviour
{
    [SerializeField] int damage = 1;
    [SerializeField] bool instantKill;
    [SerializeField] LayerMask targetLayers;

    void OnCollisionEnter2D(Collision2D collision) => TryDamage(collision.collider);
    void OnCollisionStay2D(Collision2D collision) => TryDamage(collision.collider);
    void OnTriggerEnter2D(Collider2D other) => TryDamage(other);
    void OnTriggerStay2D(Collider2D other) => TryDamage(other);

    void TryDamage(Collider2D other)
    {
        if ((targetLayers.value & (1 << other.gameObject.layer)) == 0) return;

        Health health = other.GetComponentInParent<Health>();
        if (health == null) return;

        if (instantKill) health.Kill();
        else health.TakeDamage(damage, transform.position);
    }
}
