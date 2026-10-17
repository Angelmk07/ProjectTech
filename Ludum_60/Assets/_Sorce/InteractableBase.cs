using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Базовый класс для интерактивных объектов. Наследуйтесь и переопределяйте OnInteract.
/// Объект должен быть на слое из interactableLayers детектора и иметь Collider2D (можно Trigger).
/// </summary>
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] protected string promptText = "Взаимодействовать";
    [SerializeField] protected Transform promptAnchor;
    [Tooltip("0 = нажатие, больше 0 = удержание в секундах")]
    [SerializeField] protected float holdDuration = 0f;
    [SerializeField] protected bool interactionEnabled = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onFocus;
    [SerializeField] private UnityEvent onUnfocus;
    [SerializeField] private UnityEvent onInteracted;

    public virtual string PromptText => promptText;
    public Transform PromptAnchor => promptAnchor != null ? promptAnchor : transform;
    public float HoldDuration => holdDuration;

    public virtual bool CanInteract(InteractionDetector who) => interactionEnabled;

    public void SetInteractable(bool value) => interactionEnabled = value;

    public virtual void OnFocus(InteractionDetector who) => onFocus?.Invoke();

    public virtual void OnUnfocus(InteractionDetector who) => onUnfocus?.Invoke();

    public void Interact(InteractionDetector who)
    {
        if (!CanInteract(who)) return;

        OnInteract(who);
        onInteracted?.Invoke();
    }

    protected abstract void OnInteract(InteractionDetector who);
}
