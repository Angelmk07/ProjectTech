using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Вешается на игрока. Ищет ближайший IInteractable в радиусе (только по Layer),
/// читает кнопку взаимодействия через новую Input System и вызывает Interact.
/// </summary>
public class InteractionDetector : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Action 'Interact' (Button) с биндингом на E")]
    [SerializeField] private InputActionReference interactAction;

    [Header("Detection")]
    [SerializeField] private LayerMask interactableLayers;
    [SerializeField] private float radius = 1.5f;
    [SerializeField] private Vector2 offset;

    /// <summary>Текущая цель или null.</summary>
    public IInteractable Current { get; private set; }

    /// <summary>true — детектор заблокирован (идёт головоломка и т.п.), цели нет.</summary>
    public bool IsLocked { get; private set; }

    public InputAction Action => interactAction != null ? interactAction.action : null;

    /// <summary>Цель изменилась (в т.ч. на null).</summary>
    public event Action<IInteractable> TargetChanged;

    /// <summary>Прогресс удержания 0..1.</summary>
    public event Action<float> HoldProgressChanged;

    /// <summary>Взаимодействие произошло.</summary>
    public event Action<IInteractable> Interacted;

    private readonly Collider2D[] buffer = new Collider2D[16];
    private ContactFilter2D filter;
    private float holdTimer;
    private bool waitForRelease;

    private void Awake()
    {
        filter = new ContactFilter2D();
        filter.SetLayerMask(interactableLayers);
        filter.useTriggers = true;
    }

    private void OnEnable()
    {
        if (Action != null) Action.Enable();
    }

    private void OnDisable()
    {
        SetTarget(null);
    }

    private void Update()
    {
        if (IsLocked)
        {
            if (Current != null) SetTarget(null);
            return;
        }

        SetTarget(FindBest());
        HandleInput();
    }

    /// <summary>Блокирует/разблокирует взаимодействие (головоломка, диалог, меню).</summary>
    public void SetLocked(bool locked)
    {
        IsLocked = locked;
        if (locked) SetTarget(null);
    }

    private IInteractable FindBest()
    {
        Vector2 origin = (Vector2)transform.position + offset;
        int count = Physics2D.OverlapCircle(origin, radius, filter, buffer);

        IInteractable best = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var col = buffer[i];
            if (col == null) continue;

            var candidate = col.GetComponentInParent<IInteractable>();
            if (candidate == null || !IsAlive(candidate) || !candidate.CanInteract(this)) continue;

            float dist = ((Vector2)col.ClosestPoint(origin) - origin).sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = candidate;
            }
        }

        return best;
    }

    private void HandleInput()
    {
        var action = Action;
        if (action == null || Current == null) return;

        float hold = Current.HoldDuration;

        // Обычное нажатие
        if (hold <= 0f)
        {
            if (action.WasPressedThisFrame()) Trigger();
            return;
        }

        // Удержание
        if (!action.IsPressed())
        {
            waitForRelease = false;
            if (holdTimer > 0f) ResetHold();
            return;
        }

        if (waitForRelease) return;

        holdTimer += Time.deltaTime;
        HoldProgressChanged?.Invoke(Mathf.Clamp01(holdTimer / hold));

        if (holdTimer >= hold)
        {
            ResetHold();
            waitForRelease = true;
            Trigger();
        }
    }

    private void Trigger()
    {
        var target = Current;
        if (target == null || !IsAlive(target)) return;

        target.Interact(this);
        Interacted?.Invoke(target);
    }

    private void SetTarget(IInteractable target)
    {
        if (ReferenceEquals(Current, target)) return;

        if (Current != null && IsAlive(Current)) Current.OnUnfocus(this);

        Current = target;
        ResetHold();

        if (Current != null) Current.OnFocus(this);

        TargetChanged?.Invoke(Current);
    }

    private void ResetHold()
    {
        holdTimer = 0f;
        HoldProgressChanged?.Invoke(0f);
    }

    // Защита от уничтоженных объектов: интерфейс не равен null после Destroy
    private static bool IsAlive(IInteractable i)
    {
        return !(i is UnityEngine.Object o) || o != null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere((Vector2)transform.position + offset, radius);
    }
}
