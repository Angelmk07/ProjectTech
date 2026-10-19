using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Записка/подсказка в мире. По E открывает полноэкранную записку с заданным текстом.
/// Пока записка открыта, взаимодействие блокируется и выключаются указанные скрипты (движение).
/// </summary>
public class NoteInteractable : InteractableBase
{
    [Header("Note")]
    [SerializeField] private string title;
    [TextArea(4, 14)]
    [SerializeField] private string text;
    [Tooltip("Скрипты, которые выключаются, пока записка открыта (движение игрока и т.п.)")]
    [SerializeField] private Behaviour[] disableWhileReading;

    [Header("Note events")]
    [SerializeField] private UnityEvent onNoteOpened;
    [SerializeField] private UnityEvent onNoteClosed;

    private InteractionDetector reader;
    private readonly List<Behaviour> disabledByUs = new List<Behaviour>();

    private void Reset()
    {
        promptText = "Прочитать";
    }

    public override bool CanInteract(InteractionDetector who)
    {
        return base.CanInteract(who) && reader == null;
    }

    protected override void OnInteract(InteractionDetector who)
    {
        var view = NoteViewUI.Instance;
        if (view == null)
        {
            Debug.LogWarning("NoteViewUI не найден на сцене", this);
            return;
        }

        reader = who;
        who.SetLocked(true);

        disabledByUs.Clear();
        foreach (var b in disableWhileReading)
        {
            if (b != null && b.enabled)
            {
                b.enabled = false;
                disabledByUs.Add(b);
            }
        }

        view.Show(title, text, OnClosed);
        onNoteOpened?.Invoke();
    }

    private void OnClosed()
    {
        foreach (var b in disabledByUs)
            if (b != null) b.enabled = true;
        disabledByUs.Clear();

        if (reader != null) reader.SetLocked(false);
        reader = null;

        onNoteClosed?.Invoke();
    }
}
