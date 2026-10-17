using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Объект-головоломка (терминал, замок, взлом). По E блокирует управление игрока,
/// открывает панель головоломки. Головоломка по завершении вызывает Complete(true/false) или Close().
/// Для вашей мини-игры взлома: подключите Complete через UnityEvent или прямой вызов из BreachPuzzleUI.
/// </summary>
public class PuzzleInteractable : InteractableBase
{
    [Header("Puzzle")]
    [Tooltip("Панель головоломки (необязательно, можно запускать через onPuzzleStarted)")]
    [SerializeField] private GameObject puzzlePanel;
    [Tooltip("Скрипты, которые выключаются на время головоломки (движение игрока и т.п.)")]
    [SerializeField] private Behaviour[] disableWhileActive;
    [SerializeField] private bool lockAfterSolved = true;

    [Header("Puzzle events")]
    [SerializeField] private UnityEvent onPuzzleStarted;
    [SerializeField] private UnityEvent onPuzzleSolved;
    [SerializeField] private UnityEvent onPuzzleFailed;
    [SerializeField] private UnityEvent onPuzzleClosed;

    public bool IsSolved { get; private set; }
    public bool IsActive => activeUser != null;

    private InteractionDetector activeUser;
    private readonly List<Behaviour> disabledByUs = new List<Behaviour>();

    public override bool CanInteract(InteractionDetector who)
    {
        return base.CanInteract(who) && !IsActive && !(IsSolved && lockAfterSolved);
    }

    protected override void OnInteract(InteractionDetector who)
    {
        activeUser = who;
        who.SetLocked(true);

        disabledByUs.Clear();
        foreach (var b in disableWhileActive)
        {
            if (b != null && b.enabled)
            {
                b.enabled = false;
                disabledByUs.Add(b);
            }
        }

        if (puzzlePanel != null) puzzlePanel.SetActive(true);
        onPuzzleStarted?.Invoke();
    }

    /// <summary>Вызывается головоломкой по окончании. success = true — решена.</summary>
    public void Complete(bool success)
    {
        if (!IsActive) return;

        if (success) IsSolved = true;
        Finish();

        if (success) onPuzzleSolved?.Invoke();
        else onPuzzleFailed?.Invoke();
    }

    /// <summary>Закрыть без результата (отмена, Esc).</summary>
    public void Close()
    {
        if (!IsActive) return;

        Finish();
        onPuzzleClosed?.Invoke();
    }

    private void Finish()
    {
        if (puzzlePanel != null) puzzlePanel.SetActive(false);

        foreach (var b in disabledByUs)
            if (b != null) b.enabled = true;
        disabledByUs.Clear();

        activeUser.SetLocked(false);
        activeUser = null;
    }
}
