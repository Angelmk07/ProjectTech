using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DragItem;

/// <summary>
/// Терминал головоломки с лампами (аналог StartBreaking).
/// Хранит параметры своей головоломки и дверь, передаёт их в общий PuzzleCanvas.
/// Результат приходит от головоломки — тогда терминал открывает дверь.
/// </summary>
public class PuzzleTerminal : InteractableBase
{
    [SerializeField] private string enterPrompt = "Включить";
    [SerializeField] private string exitPrompt = "Готово";
    [Tooltip("Можно не указывать — найдётся единственный PuzzleCanvas на сцене")]
    [SerializeField] private PuzzleCanvas puzzle;

    [Header("Головоломка этого терминала")]
    [SerializeField, Min(1)] private int rows = 2;
    [SerializeField, Min(1)] private int columns = 4;
    [Tooltip("Адрес ячейки: буква = строка, цифра = столбец (A1 — первая строка, первый столбец) и нужная лампа. Остальные ячейки — пустые")]
    [SerializeField] private List<CellRequirement> requirements = new List<CellRequirement>();

    [Header("Что произойдёт после решения")]
    [Tooltip("Дверь, которую откроет этот терминал (необязательно)")]
    [SerializeField] private Door2D door;
    [Tooltip("Для поста, финала и всего остального")]
    [SerializeField] private UnityEvent onSolved;

    private bool solved = false;

    public override string PromptText => solved ? exitPrompt : enterPrompt;

    protected override void OnInteract(InteractionDetector who)
    {
        if (solved) return;

        PuzzleCanvas ui = puzzle != null ? puzzle : PuzzleCanvas.Instance;
        if (ui == null)
        {
            Debug.LogWarning("PuzzleCanvas не найден", this);
            return;
        }

        ui.StartPuzzle(rows, columns, requirements, OnPuzzleSolved);
    }

    private void OnPuzzleSolved()
    {
        solved = true;
        if (door != null) door.OpenDoor();
        onSolved?.Invoke();
    }
}