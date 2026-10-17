using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BreachPuzzleUI : MonoBehaviour
{
    [Header("Настройки головоломки")]
    [SerializeField] int gridSize = 5;
    [SerializeField, Min(2)] int keyLength = 4;
    [SerializeField] float timeLimit = 30f;
    [SerializeField, Min(1)] int maxErrors = 3;
    [SerializeField] BreachMoveRule moveRule = BreachMoveRule.CyberpunkAlternating;
    [Tooltip("Ставить игру на паузу (Time.timeScale = 0) на время взлома")]
    [SerializeField] bool pauseGameplay = true;
    [SerializeField] float closeDelay = 1f;
    [SerializeField] string[] codePool = { "1C", "55", "BD", "E9", "7A", "FF" };

    [Header("UI")]
    [SerializeField] GameObject panelRoot;
    [SerializeField] RectTransform gridRoot;        
    [SerializeField] BreachCell cellPrefab;
    [SerializeField] RectTransform keyRoot;         
    [SerializeField] TMP_Text keySlotPrefab;
    [SerializeField] TMP_Text timerText;
    [SerializeField] Image timerFill;              
    [SerializeField] TMP_Text errorsText;
    [SerializeField] TMP_Text statusText;           

    [Header("Цвета ключа")]
    [SerializeField] Color slotPending = Color.white;
    [SerializeField] Color slotCurrent = new Color(1f, 0.9f, 0.2f);
    [SerializeField] Color slotDone = new Color(0.35f, 0.35f, 0.35f);

    [Header("События")]
    public UnityEvent OnSolved;
    public UnityEvent OnFailed;

    BreachData data;
    readonly List<BreachCell> cells = new List<BreachCell>();
    readonly List<BreachCell> picked = new List<BreachCell>();
    readonly List<TMP_Text> slots = new List<TMP_Text>();

    int progress;
    int errors;
    float timeLeft;
    bool timerRunning;
    bool finished = true;
    float prevTimeScale = 1f;

    void Awake() 
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }
    //private void Start()
    //{
    //    StartPuzzle();
    //}

    public void StartPuzzle() => StartPuzzle(keyLength, timeLimit);

    public void StartPuzzle(int newKeyLength, float newTimeLimit)
    {
        keyLength = newKeyLength;
        timeLimit = newTimeLimit;

        StopAllCoroutines();
        panelRoot.SetActive(true);

        if (pauseGameplay)
        {
            prevTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }

        data = BreachGenerator.Generate(gridSize, keyLength, codePool, moveRule);
        BuildGrid();
        BuildKey();

        progress = 0;
        errors = 0;
        timeLeft = timeLimit;
        timerRunning = false;
        finished = false;

        if (statusText != null) statusText.text = "";
        RefreshHighlights();
        RefreshHud();
    }


    void BuildGrid()
    {
        foreach (Transform child in gridRoot) Destroy(child.gameObject);
        cells.Clear();
        picked.Clear();

        var layout = gridRoot.GetComponent<GridLayoutGroup>();
        if (layout != null)
        {
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = data.size;
        }
        for (int y = 0; y < data.size; y++)
        {
            for (int x = 0; x < data.size; x++)
            {
                var cell = Instantiate(cellPrefab, gridRoot);
                cell.Init(new Vector2Int(x, y), data.grid[y, x], OnCellClicked);
                cells.Add(cell);
            }
        }
    }

    void BuildKey()
    {
        foreach (Transform child in keyRoot) Destroy(child.gameObject);
        slots.Clear();

        foreach (var code in data.key)
        {
            var slot = Instantiate(keySlotPrefab, keyRoot);
            slot.text = code;
            slots.Add(slot);
        }
    }


    void OnCellClicked(BreachCell cell)
    {
        if (finished || cell.Used) return;
        if (!IsLegal(cell.Pos)) return;

        timerRunning = true;

        if (cell.Code == data.key[progress])
        {
            cell.MarkUsed();
            picked.Add(cell);
            progress++;

            if (progress >= data.key.Length)
            {
                Finish(true);
                return;
            }
        }
        else
        {
            RegisterError();
            if (finished) return;
        }

        RefreshHighlights();
        RefreshHud();
    }

    bool IsLegal(Vector2Int pos)
    {
        Vector2Int last = progress > 0 ? picked[progress - 1].Pos : default;
        return BreachGenerator.IsLegalStep(moveRule, progress, last, pos);
    }

    void RegisterError()
    {
        errors++;
        if (errors >= maxErrors)
        {
            Finish(false);
            return;
        }

        foreach (var c in picked) c.ResetState();
        picked.Clear();
        progress = 0;
    }

    void Update()
    {
        if (finished || !timerRunning) return;

        timeLeft -= Time.unscaledDeltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            RefreshHud();
            Finish(false);
            return;
        }
        RefreshHud();
    }

    void Finish(bool success)
    {
        finished = true;
        timerRunning = false;

        foreach (var c in cells) c.SetAvailable(false);
        RefreshHud();

        if (statusText != null)
        {
            statusText.text = success ? "ВЗЛОМ УСПЕШЕН" : "ВЗЛОМ ПРОВАЛЕН";
            statusText.color = success ? Color.green : Color.red;
        }
        if (success) OnSolved?.Invoke();
        else OnFailed?.Invoke();

        StartCoroutine(CloseAfterDelay());
    }

    IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSecondsRealtime(closeDelay);
        Close();
    }

    public void Close()
    {
        panelRoot.SetActive(false);
        if (pauseGameplay) Time.timeScale = prevTimeScale;
    }

    void RefreshHighlights()
    {
        foreach (var c in cells)
            c.SetAvailable(!finished && !c.Used && IsLegal(c.Pos));
    }

    void RefreshHud()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].color = i < progress ? slotDone
                           : i == progress && !finished ? slotCurrent
                           : slotPending;
        }

        if (timerText != null) timerText.text = timeLeft.ToString("0.0");
        if (timerFill != null) timerFill.fillAmount = timeLimit > 0f ? timeLeft / timeLimit : 0f;
        if (errorsText != null) errorsText.text = $"Ошибки: {errors}/{maxErrors}";
    }
}
