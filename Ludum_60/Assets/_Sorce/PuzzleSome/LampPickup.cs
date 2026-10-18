using UnityEngine;
using DragItem;

/// <summary>
/// Лампа, лежащая на уровне. По E кладётся в LampInventory и исчезает из мира.
/// </summary>
public class LampPickup : InteractableBase
{
    [Header("Лампа")]
    [SerializeField] private ItemsState lampState;

    protected override void OnInteract(InteractionDetector who)
    {
        if (LampInventory.Instance == null)
        {
            Debug.LogWarning("LampInventory не найден на сцене", this);
            return;
        }

        // Если инвентарь полон — лампа остаётся лежать
        if (!LampInventory.Instance.AddLamp(lampState))
            return;

        Destroy(gameObject);
    }
}
