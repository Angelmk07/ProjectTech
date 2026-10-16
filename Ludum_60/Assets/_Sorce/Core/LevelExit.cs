using UnityEngine;

/// <summary>
/// Дверь / портал на следующий уровень. Нужен Collider2D с включённым Is Trigger.
/// Перед сменой сцены сохраняет ресурсы и здоровье игрока.
/// </summary>
public class LevelExit : MonoBehaviour
{
    [SerializeField] LayerMask playerLayers;

    [Tooltip("-1 = следующая сцена по порядку в Build Settings")]
    [SerializeField] int targetSceneIndex = -1;

    bool used;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        if ((playerLayers.value & (1 << other.gameObject.layer)) == 0) return;

        Health health = other.GetComponentInParent<Health>();
        if (health == null || health.IsDead) return;

        used = true;
        GameManager.Instance.CompleteLevel(health.Current, targetSceneIndex);
    }
}
