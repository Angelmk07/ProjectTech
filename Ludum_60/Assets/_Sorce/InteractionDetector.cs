using System;
using UnityEngine;
using UnityEngine.InputSystem;


public class InteractionDetector : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Action 'Interact' (Button) с биндингом на E")]
    [SerializeField] private InputActionReference interactAction;

    [Header("Detection")]
    [SerializeField] private LayerMask interactableLayers;
    [SerializeField] private float radius = 1.5f;
    [SerializeField] private Vector2 offset;

    public IInteractable Current { get; private set; }

    public bool IsLocked { get; private set; }

    public InputAction Action => interactAction != null ? interactAction.action : null;

    public event Action<IInteractable> TargetChanged;

    public event Action<float> HoldProgressChanged;

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

        if (hold <= 0f)
        {
            if (action.WasPressedThisFrame()) Trigger();
            return;
        }

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
