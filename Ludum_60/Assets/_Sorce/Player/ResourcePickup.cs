using UnityEngine;
using DragItem;

public class ResourcePickup : InteractableBase
{
    [Header("Resource")]
    [SerializeField] private ResourceType type = ResourceType.LightA;
    [Tooltip("0 Ч не считать как ресурс (например, дл€ лампы-головоломки)")]
    [SerializeField] private int amount = 1;
    [SerializeField] private LayerMask collectorLayers;

    [Header("Ћампа дл€ головоломки (необ€зательно)")]
    [Tooltip("¬ключить, если это лампа: она попадЄт в свободную €чейку инвентар€ ламп")]
    [SerializeField] private bool isLamp = false;
    [SerializeField] private ItemsState lampState;
    [Tooltip(" артинка лампы в инвентаре. ѕусто Ч берЄтс€ из SpriteRenderer этого объекта, затем из списка Lamp Visuals инвентар€")]
    [SerializeField] private Sprite lampSprite;

    public override bool CanInteract(InteractionDetector who)
    {
        if (!base.CanInteract(who))
            return false;

        int layer = who.gameObject.layer;
        return (collectorLayers.value & (1 << layer)) != 0;
    }

    protected override void OnInteract(InteractionDetector who)
    {
        if (isLamp)
        {
            if (LampInventory.Instance == null)
            {
                Debug.LogWarning("LampInventory не найден на сцене", this);
                return;
            }

            // Ќет свободной €чейки Ч лампа остаЄтс€ лежать
            if (!LampInventory.Instance.AddLamp(lampState, ResolveLampSprite()))
                return;
        }

        if (GameManager.Instance != null)
        {
            if (amount > 0)
                GameManager.Instance.AddResource(type, amount);
        }
        else if (!isLamp)
        {
            Debug.LogWarning("GameManager.Instance не найден", this);
            return;
        }

        Destroy(gameObject);
    }

    private Sprite ResolveLampSprite()
    {
        if (lampSprite != null) return lampSprite;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        return sr != null ? sr.sprite : null;
    }
}