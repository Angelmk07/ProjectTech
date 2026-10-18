using UnityEngine;
public class HideSpot : InteractableBase
{
    [Header("Hide")]
    [SerializeField] private string enterPrompt = "Спрятаться";
    [SerializeField] private string exitPrompt = "Выйти";
    [Tooltip("Куда переносится игрок внутри укрытия (по умолчанию — центр объекта)")]
    [SerializeField] private Transform hidePoint;
    [Tooltip("Куда выйти (по умолчанию — туда, откуда зашёл)")]
    [SerializeField] private Transform exitPoint;

    private HideController occupant;

    public override string PromptText => occupant != null ? exitPrompt : enterPrompt;

    public override bool CanInteract(InteractionDetector who)
    {
        if (!base.CanInteract(who)) return false;

        return occupant == null || occupant.gameObject == who.gameObject;
    }

    protected override void OnInteract(InteractionDetector who)
    {
        if (occupant != null)
        {
            occupant.Unhide(exitPoint);
            occupant = null;
            return;
        }

        var hider = who.GetComponent<HideController>();
        if (hider == null)
        {
            Debug.LogWarning("У игрока нет HideController", who);
            return;
        }

        occupant = hider;
        hider.Hide(hidePoint != null ? hidePoint : transform);
    }
}
