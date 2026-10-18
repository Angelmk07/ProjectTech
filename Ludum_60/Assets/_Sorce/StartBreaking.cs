using UnityEngine;
using UnityEngine.Events;

public class StartBreaking : InteractableBase
{
    [SerializeField] private string enterPrompt = "Взломать";
    [SerializeField] private string exitPrompt = "Взломано";
    [SerializeField] private BreachPuzzleUI puzzle;

    [Header("Параметры этого терминала (необязательно)")]
    [SerializeField] private bool overrideDifficulty = false;
    [SerializeField, Min(2)] private int keyLength = 4;
    [SerializeField] private float timeLimit = 30f;

    [Header("Что произойдёт после взлома именно этого терминала")]
    [SerializeField] private UnityEvent onHacked;

    private bool solved = false;

    public override string PromptText => solved ? exitPrompt : enterPrompt;

    protected override void OnInteract(InteractionDetector who)
    {
        if (solved) return;

        if (puzzle == null)
        {
            Debug.LogWarning("BreachPuzzleUI не назначен", this);
            return;
        }

        if (overrideDifficulty)
            puzzle.StartPuzzle(keyLength, timeLimit, OnPuzzleSolved);
        else
            puzzle.StartPuzzle(OnPuzzleSolved);
    }

    private void OnPuzzleSolved()
    {
        solved = true;
        onHacked?.Invoke();
    }
}