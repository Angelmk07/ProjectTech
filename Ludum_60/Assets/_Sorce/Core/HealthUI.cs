using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Полоска сердечек. Количество подстраивается под максимальное здоровье.
/// Пока спрайты не назначены, сердечки не отображаются — просто перетащите картинки в поля.
/// </summary>
public class HealthUI : MonoBehaviour
{
    [SerializeField] Health health;

    [Tooltip("Контейнер для сердечек (лучше с Horizontal Layout Group). Пусто = этот объект")]
    [SerializeField] RectTransform container;

    [Header("Картинки (ждут назначения)")]
    [SerializeField] Sprite fullHeartSprite;
    [SerializeField] Sprite emptyHeartSprite;
    [SerializeField] Vector2 heartSize = new Vector2(48f, 48f);

    readonly List<Image> hearts = new List<Image>();

    void Awake()
    {
        if (container == null) container = (RectTransform)transform;
    }

    void OnEnable()
    {
        if (health != null) health.Changed += Refresh;
    }

    void OnDisable()
    {
        if (health != null) health.Changed -= Refresh;
    }

    void Start()
    {
        if (health == null)
        {
            Debug.LogWarning("HealthUI: не назначен Health игрока.", this);
            return;
        }

        Refresh(health.Current, health.Max);
    }

    void Refresh(int current, int max)
    {
        while (hearts.Count < max) hearts.Add(CreateHeart());

        while (hearts.Count > max)
        {
            Destroy(hearts[hearts.Count - 1].gameObject);
            hearts.RemoveAt(hearts.Count - 1);
        }

        for (int i = 0; i < hearts.Count; i++)
        {
            Sprite sprite = i < current ? fullHeartSprite : emptyHeartSprite;
            hearts[i].sprite = sprite;
            hearts[i].enabled = sprite != null;
        }
    }

    Image CreateHeart()
    {
        var go = new GameObject("Heart", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(container, false);
        ((RectTransform)go.transform).sizeDelta = heartSize;

        var image = go.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }
}
