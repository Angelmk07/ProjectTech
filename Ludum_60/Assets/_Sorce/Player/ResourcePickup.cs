
using UnityEngine;

public class ResourcePickup : InteractableBase
{
    [Header("Resource")]
    [SerializeField] private ResourceType type = ResourceType.LightA;
    [SerializeField] private int amount = 1;
    [SerializeField] private LayerMask collectorLayers;

    

    public override bool CanInteract(InteractionDetector who)
    {
        if (!base.CanInteract(who))
            return false;

        int layer = who.gameObject.layer;
        return (collectorLayers.value & (1 << layer)) != 0;
    }

    protected override void OnInteract(InteractionDetector who)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance не найден", this);
            return;
        }

        GameManager.Instance.AddResource(type, amount);
        Destroy(gameObject);
    }
}
