using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace DragItem
{

    public class PuzzleCanvas : MonoBehaviour
    {
        public static PuzzleCanvas Instance { get; private set; }

        [Header("Ссылки")]
        [Tooltip("Окно головоломки (дочерний объект, включается/выключается)")]
        [SerializeField] private GameObject panelRoot;
        [Tooltip("Контейнер сетки — тот же, что указан в PuzzleGridGenerator")]
        [SerializeField] private Transform gridRoot;
        [SerializeField] private PuzzleGridGenerator generator;
        [SerializeField] private SolutionsChecker checker;

        [Header("Настройки")]
        [Tooltip("Пауза после решения, чтобы игрок увидел результат")]
        [SerializeField] private float closeDelayAfterSolved = 0.8f;
        [SerializeField] private bool closeOnEscape = true;
        [Tooltip("Показывать в нужных ячейках картинку лампы (если у PuzzleSlot есть Hint Image)")]
        [SerializeField] private bool showHints = true;

        [Header("События (например, блокировка управления игроком)")]
        [SerializeField] private UnityEvent onOpened;
        [SerializeField] private UnityEvent onClosed;

        private Action onSolvedCallback;
        private bool isOpen;
        private bool solved;
        private int session;

        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[PuzzleCanvas] На сцене уже есть PuzzleCanvas.", this);
                Destroy(this);
                return;
            }
            Instance = this;

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (checker != null) checker.StateChanged += OnCheckerChanged;
        }

        private void OnDisable()
        {
            if (checker != null) checker.StateChanged -= OnCheckerChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (!isOpen || !closeOnEscape) return;

            Keyboard kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
                Close();
        }

        /// <summary>
        /// Открывает головоломку. onSolved вызывается один раз — уже после закрытия панели,
        /// и только если головоломка решена.
        /// </summary>
        public void StartPuzzle(int rows, int columns, IEnumerable<CellRequirement> requirements, Action onSolved)
        {
            if (isOpen) return;

            if (panelRoot == null || gridRoot == null || generator == null)
            {
                Debug.LogError("[PuzzleCanvas] Не назначены Panel Root / Grid Root / Generator.", this);
                return;
            }

            isOpen = true;
            solved = false;
            onSolvedCallback = onSolved;
            session++;

            panelRoot.SetActive(true);
            generator.Generate(rows, columns, requirements ?? Array.Empty<CellRequirement>());
            ApplyHints();

            onOpened?.Invoke();
        }

        /// <summary>Вешается на кнопку «Закрыть».</summary>
        public void Close()
        {
            if (!isOpen || IsDragging()) return;
            CloseInternal();
        }

        private void OnCheckerChanged()
        {
            if (!isOpen || solved || !checker.IsSolved) return;

            solved = true;
            StartCoroutine(CloseAfterSolved(session));
        }

        private IEnumerator CloseAfterSolved(int forSession)
        {
            yield return new WaitForSecondsRealtime(closeDelayAfterSolved);

            // Не закрываем, пока игрок тащит лампу
            while (isOpen && session == forSession && IsDragging())
                yield return null;

            if (isOpen && session == forSession)
                CloseInternal();
        }

        private void CloseInternal()
        {
            isOpen = false; // с этого момента события чекера игнорируются

            Action callback = solved ? onSolvedCallback : null;
            onSolvedCallback = null;

            ReturnLampsToInventory();
            generator.ClearGrid();
            panelRoot.SetActive(false);

            onClosed?.Invoke();
            callback?.Invoke(); // дверь открывается, когда игрок уже видит уровень
        }

        private void ReturnLampsToInventory()
        {
            foreach (DraggableItem lamp in gridRoot.GetComponentsInChildren<DraggableItem>(true))
            {
                if (LampInventory.Instance == null || !LampInventory.Instance.ReturnLamp(lamp))
                    Debug.LogError("[PuzzleCanvas] Не удалось вернуть лампу в инвентарь — она будет потеряна.", this);
            }
        }

        private void ApplyHints()
        {
            PuzzleSlot[] puzzleSlots = gridRoot.GetComponentsInChildren<PuzzleSlot>(true);
            if (puzzleSlots.Length == 0)
                Debug.LogWarning("[PuzzleCanvas] В сетке нет ячеек головоломки — проверь Requirements у терминала.", this);

            foreach (PuzzleSlot slot in puzzleSlots)
            {
                Sprite sprite = null;
                if (showHints && LampInventory.Instance != null)
                    sprite = LampInventory.Instance.GetSprite(slot.RequiredState);
                slot.SetHint(sprite);
            }
        }

        // Пока лампу тащат, DraggableItem делает её прямым потомком корня сцены
        private bool IsDragging()
        {
            foreach (Transform child in transform.root)
                if (child.GetComponent<DraggableItem>() != null) return true;
            return false;
        }
    }
}
