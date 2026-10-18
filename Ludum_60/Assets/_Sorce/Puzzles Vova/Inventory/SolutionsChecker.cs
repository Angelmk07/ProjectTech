using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace DragItem
{
    public class SolutionsChecker : MonoBehaviour
    {
        [Header("Настройки головоломки")]
        [Tooltip("Перетащи сюда только те слоты, в которые ДОЛЖНЫ быть вставлены правильные лампочки")]
        [SerializeField] private List<PuzzleSlot> requiredSlots = new List<PuzzleSlot>();

        [Header("События")]
        [SerializeField] private UnityEvent OnSolved;
        [SerializeField] private UnityEvent OnFailed;

        private bool isPuzzleSolved = false;

        private void OnEnable()
        {
            // Подписываемся на изменения в слотах
            PuzzleSlot.OnSlotStateChanged += CheckPuzzleStatus;
        }

        private void OnDisable()
        {
            // Отписываемся при уничтожении/отключении объекта, чтобы избежать утечек памяти
            PuzzleSlot.OnSlotStateChanged -= CheckPuzzleStatus;
        }

        // Главный метод проверки
        private void CheckPuzzleStatus()
        {
            int currentCorrectSlots = 0;

            // Считаем, сколько обязательных слотов сейчас имеют статус true
            foreach (var slot in requiredSlots)
            {
                if (slot != null && slot.IsConditionMet)
                {
                    currentCorrectSlots++;
                }
            }

            // Если количество совпало с размером списка обязательных слотов
            if (currentCorrectSlots == requiredSlots.Count)
            {
                // Проверяем, чтобы событие не вызывалось повторно, если головоломка уже была решена
                if (!isPuzzleSolved)
                {
                    isPuzzleSolved = true;
                    Debug.Log("<color=green>[SolutionsChecker] Головоломка РЕШЕНА!</color>");
                    OnSolved?.Invoke();
                }
            }
            else
            {
                // Если было решено, но лампочку вытащили
                if (isPuzzleSolved)
                {
                    isPuzzleSolved = false;
                    Debug.Log("<color=red>[SolutionsChecker] Головоломка СЛОМАНА!</color>");
                    OnFailed?.Invoke();
                }
            }
        }
    }
}