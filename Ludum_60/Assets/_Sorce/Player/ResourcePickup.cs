using UnityEngine;

/// <summary>
/// Монетка, ключ, кристалл. Нужен Collider2D с включённым Is Trigger.
/// Картинка — обычный SpriteRenderer на этом же объекте.
/// </summary>
public class ResourcePickup : MonoBehaviour
{
    [SerializeField] ResourceType type = ResourceType.Coin;
    [SerializeField] int amount = 1;
    [SerializeField] LayerMask collectorLayers;

    bool collected;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if ((collectorLayers.value & (1 << other.gameObject.layer)) == 0) return;

        collected = true;
        GameManager.Instance.AddResource(type, amount);
        // сюда: звук / эффект подбора
        Destroy(gameObject);
    }
}
