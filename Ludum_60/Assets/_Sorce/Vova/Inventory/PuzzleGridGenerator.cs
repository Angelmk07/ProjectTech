using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DragItem
{
    [Serializable]
    public struct CellRequirement
    {
        [Tooltip("Адрес ячейки: буква = строка, цифра = столбец. Пример: A3")]
        public string cell;
        public ItemsState requiredState;

        public CellRequirement(string cell, ItemsState requiredState)
        {
            this.cell = cell;
            this.requiredState = requiredState;
        }
    }

    public class PuzzleGridGenerator : MonoBehaviour
    {
        [Header("Ссылки")]
        [Tooltip("Контейнер сетки (на нём должен быть GridLayoutGroup — добавится сам, если нет)")]
        [SerializeField] private RectTransform gridContainer;
        [Tooltip("Префаб обычной (пустой, декоративной) ячейки — компонент Slot + Image")]
        [SerializeField] private Slot emptySlotPrefab;
        [Tooltip("Префаб ячейки головоломки — компонент PuzzleSlot + Image")]
        [SerializeField] private PuzzleSlot puzzleSlotPrefab;
        [SerializeField] private SolutionsChecker checker;

        [Tooltip("Куда вернуть лампочки, если сетку пересоздают, а они лежат в ячейках (например, контейнер инвентаря)")]
        [SerializeField] private Transform itemsReturnParent;

        [Header("Тест из инспектора (необязательно)")]
        [SerializeField] private bool generateOnStart = false;
        [SerializeField] private int testRows = 2;
        [SerializeField] private int testColumns = 4;
        [SerializeField] private List<CellRequirement> testRequirements = new List<CellRequirement>();

        private readonly List<GameObject> spawned = new List<GameObject>();

        private void Start()
        {
            if (generateOnStart)
                Generate(testRows, testColumns, testRequirements);
        }

 
        public void Generate(int rows, int columns, IEnumerable<CellRequirement> requirements)
        {
            var map = new Dictionary<string, ItemsState>();
            foreach (var r in requirements) map[r.cell] = r.requiredState;
            Generate(rows, columns, map);
        }

        public void Generate(int rows, int columns, Dictionary<string, ItemsState> requirements)
        {
            if (rows <= 0 || columns <= 0)
            {
                Debug.LogError("[PuzzleGrid] Размер сетки должен быть больше нуля.");
                return;
            }

            ClearGrid();


            var layout = gridContainer.GetComponent<GridLayoutGroup>();
            if (layout == null) layout = gridContainer.gameObject.AddComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columns;


            var required = new Dictionary<(int row, int col), ItemsState>();
            foreach (var pair in requirements)
            {
                if (!TryParseCell(pair.Key, out int r, out int c))
                {
                    Debug.LogError($"[PuzzleGrid] Не удалось разобрать адрес ячейки \"{pair.Key}\". Ожидается формат вроде A3.");
                    continue;
                }
                if (r >= rows || c >= columns)
                {
                    Debug.LogError($"[PuzzleGrid] Ячейка {pair.Key} выходит за пределы сетки {rows}x{columns}.");
                    continue;
                }
                required[(r, c)] = pair.Value;
            }

            var puzzleSlots = new List<PuzzleSlot>();
            int index = 0;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Slot slot;
                    if (required.TryGetValue((r, c), out ItemsState state))
                    {
                        PuzzleSlot puzzleSlot = Instantiate(puzzleSlotPrefab, gridContainer);
                        puzzleSlot.SetRequiredState(state);
                        puzzleSlots.Add(puzzleSlot);
                        slot = puzzleSlot;
                    }
                    else
                    {
                        slot = Instantiate(emptySlotPrefab, gridContainer);
                    }

                    slot.SetIndex(index++);
                    slot.gameObject.name = $"Cell_{ToCellName(r, c)}";
                    spawned.Add(slot.gameObject);
                }
            }

            if (checker != null)
                checker.SetRequiredSlots(puzzleSlots);
            else
                Debug.LogWarning("[PuzzleGrid] SolutionsChecker не назначен — решение проверяться не будет.");
        }

        public void ClearGrid()
        {
            foreach (var go in spawned)
            {
                if (go == null) continue;

                // Спасаем лампочки, чтобы они не удалились вместе со слотом
                if (itemsReturnParent != null)
                {
                    foreach (var item in go.GetComponentsInChildren<DraggableItem>(true))
                        item.transform.SetParent(itemsReturnParent, false);
                }

                go.transform.SetParent(null); // чтобы GridLayout сразу перестал считать ячейку
                Destroy(go);
            }
            spawned.Clear();
        }


        private static bool TryParseCell(string address, out int row, out int col)
        {
            row = col = -1;
            if (string.IsNullOrWhiteSpace(address)) return false;

            address = address.Trim().ToUpperInvariant();

            int i = 0;
            int letters = 0;
            while (i < address.Length && address[i] >= 'A' && address[i] <= 'Z')
            {
                letters = letters * 26 + (address[i] - 'A' + 1);
                i++;
            }
            if (i == 0 || i == address.Length) return false;

            if (!int.TryParse(address.Substring(i), out int number) || number < 1) return false;

            row = letters - 1;
            col = number - 1;
            return true;
        }

        private static string ToCellName(int row, int col)
        {
            string letters = "";
            int n = row + 1;
            while (n > 0)
            {
                n--;
                letters = (char)('A' + n % 26) + letters;
                n /= 26;
            }
            return letters + (col + 1);
        }
    }
}