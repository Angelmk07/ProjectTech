using UnityEngine;

/// <summary>
/// Контракт для всего, с чем можно взаимодействовать:
/// предметы, двери, головоломки, укрытия и т.д.
/// </summary>
public interface IInteractable
{
    /// <summary>Текст действия в подсказке ("Взломать", "Спрятаться"...). Может меняться динамически.</summary>
    string PromptText { get; }

    /// <summary>Точка в мире, над которой показывается подсказка.</summary>
    Transform PromptAnchor { get; }

    /// <summary>0 = обычное нажатие, больше 0 = нужно удерживать кнопку столько секунд.</summary>
    float HoldDuration { get; }

    /// <summary>Можно ли взаимодействовать прямо сейчас.</summary>
    bool CanInteract(InteractionDetector who);

    /// <summary>Объект стал целью (игрок рядом и это ближайший объект).</summary>
    void OnFocus(InteractionDetector who);

    /// <summary>Объект перестал быть целью.</summary>
    void OnUnfocus(InteractionDetector who);

    /// <summary>Само взаимодействие.</summary>
    void Interact(InteractionDetector who);
}
