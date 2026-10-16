using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Здоровье для игрока и врагов. Ничего не знает про UI — только события.
/// </summary>
public class Health : MonoBehaviour
{
    [SerializeField] int maxHealth = 5;
    [SerializeField] float invulnerabilityTime = 1f;

    [Tooltip("Для игрока: брать здоровье из сохранения (переносится между уровнями)")]
    [SerializeField] bool persistBetweenLevels;

    [Tooltip("Для врагов: уничтожить объект после смерти")]
    [SerializeField] bool destroyOnDeath;

    [Tooltip("Необязательно: спрайты, которые мигают во время неуязвимости")]
    [SerializeField] SpriteRenderer[] blinkRenderers;
    [SerializeField] float blinkInterval = 0.1f;

    public int Current { get; private set; }
    public int Max => maxHealth;
    public bool IsDead => Current <= 0;

    public event Action<int, int> Changed;     // текущее, максимальное
    public event Action<int, Vector2> Damaged; // урон, позиция источника
    public event Action Died;

    float invulnerableUntil;

    void Awake()
    {
        Current = maxHealth;

        if (persistBetweenLevels)
        {
            int saved = GameManager.Instance.SavedPlayerHealth;
            if (saved > 0) Current = Mathf.Min(saved, maxHealth);
        }
    }

    public bool TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (IsDead || amount <= 0 || Time.time < invulnerableUntil) return false;

        Current = Mathf.Max(0, Current - amount);
        invulnerableUntil = Time.time + invulnerabilityTime;

        Changed?.Invoke(Current, maxHealth);
        Damaged?.Invoke(amount, sourcePosition);

        if (IsDead) HandleDeath();
        else if (blinkRenderers != null && blinkRenderers.Length > 0 && invulnerabilityTime > 0f)
            StartCoroutine(Blink());

        return true;
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;

        Current = Mathf.Min(maxHealth, Current + amount);
        Changed?.Invoke(Current, maxHealth);
    }

    // Мгновенная смерть (пропасть, шипы-убийцы). Игнорирует неуязвимость.
    public void Kill()
    {
        if (IsDead) return;

        Current = 0;
        Changed?.Invoke(Current, maxHealth);
        HandleDeath();
    }

    void HandleDeath()
    {
        StopAllCoroutines();
        SetBlinkVisible(true);

        Died?.Invoke();

        if (destroyOnDeath) Destroy(gameObject);
    }

    IEnumerator Blink()
    {
        bool visible = false;
        while (Time.time < invulnerableUntil)
        {
            SetBlinkVisible(visible);
            visible = !visible;
            yield return new WaitForSeconds(blinkInterval);
        }
        SetBlinkVisible(true);
    }

    void SetBlinkVisible(bool visible)
    {
        if (blinkRenderers == null) return;
        foreach (var r in blinkRenderers)
            if (r != null) r.enabled = visible;
    }
}
