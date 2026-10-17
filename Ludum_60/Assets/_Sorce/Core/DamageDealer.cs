
using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private bool instantKill;
    [SerializeField] private LayerMask targetLayers;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        DealDamage(collision.collider.transform);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        DealDamage(collision.collider.transform);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        DealDamage(other.transform);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        DealDamage(other.transform);
    }

    public void DealDamage(Transform target)
    {
        if (target == null) return;

        int targetLayer = 1 << target.gameObject.layer;

        if ((targetLayers.value & targetLayer) == 0)
            return;

        Health health = target.GetComponentInParent<Health>();

        if (health == null)
            return;

        if (instantKill)
            health.Kill();
        else
            health.TakeDamage(damage, transform.position);
    }
}
