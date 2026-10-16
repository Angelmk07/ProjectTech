using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Показывает ресурсы: иконка + число. Для каждого ресурса добавьте строку в Slots,
/// назначьте Image, Text и картинку — и всё работает. Без картинки иконка скрыта.
/// </summary>
public class ResourcesUI : MonoBehaviour
{
    [Serializable]
    public class Slot
    {
        public ResourceType type;
        [Tooltip("Image, в который подставится иконка")]
        public Image icon;
        [Tooltip("Картинка ресурса (ждёт назначения)")]
        public Sprite iconSprite;
        public TMP_Text amountText;
        public string format = "x{0}";
    }

    [SerializeField] List<Slot> slots = new List<Slot>();

    void OnEnable()
    {
        GameManager.Instance.ResourceChanged += OnResourceChanged;
    }

    void OnDisable()
    {
        if (GameManager.HasInstance)
            GameManager.Instance.ResourceChanged -= OnResourceChanged;
    }

    void Start()
    {
        foreach (var slot in slots)
        {
            if (slot.icon != null)
            {
                slot.icon.sprite = slot.iconSprite;
                slot.icon.enabled = slot.iconSprite != null;
            }

            UpdateText(slot, GameManager.Instance.GetResource(slot.type));
        }
    }

    void OnResourceChanged(ResourceType type, int amount)
    {
        foreach (var slot in slots)
            if (slot.type == type) UpdateText(slot, amount);
    }

    static void UpdateText(Slot slot, int amount)
    {
        if (slot.amountText != null)
            slot.amountText.text = string.Format(slot.format, amount);
    }
}
