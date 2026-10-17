using System;
using UnityEngine;


public class HideController : MonoBehaviour, IHideable
{
    [Tooltip("Скрипты управления, которые выключаются, пока игрок спрятан")]
    [SerializeField] private Behaviour[] disableWhileHidden;
    [Tooltip("Прозрачность игрока в укрытии (0 = невидим, 1 = обычный)")]
    [Range(0f, 1f)]
    [SerializeField] private float hiddenAlpha = 0.4f;
    [SerializeField] private Rigidbody2D body;

    public bool IsHidden { get; private set; }

    public event Action<bool> HiddenChanged;

    private SpriteRenderer[] sprites;
    private Color[] originalColors;

    private void Awake()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
    }

    public void Hide(Transform hidePoint)
    {
        if (IsHidden) return;
        IsHidden = true;

        foreach (var b in disableWhileHidden)
            if (b != null) b.enabled = false;

        if (body != null)
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = Vector2.zero;
#else
            body.velocity = Vector2.zero;
#endif
            body.simulated = false;
        }

        if (hidePoint != null) transform.position = hidePoint.position;

        sprites = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            originalColors[i] = sprites[i].color;
            var c = originalColors[i];
            c.a = hiddenAlpha;
            sprites[i].color = c;
        }

        HiddenChanged?.Invoke(true);
    }

    public void Unhide(Transform exitPoint = null)
    {
        if (!IsHidden) return;
        IsHidden = false;

        if (exitPoint != null) transform.position = exitPoint.position;

        if (body != null) body.simulated = true;

        foreach (var b in disableWhileHidden)
            if (b != null) b.enabled = true;

        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) sprites[i].color = originalColors[i];

        HiddenChanged?.Invoke(false);
    }
}
