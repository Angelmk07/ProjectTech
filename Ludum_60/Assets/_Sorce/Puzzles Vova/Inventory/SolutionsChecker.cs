using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace DragItem
{
    public class SolutionsChecker : MonoBehaviour
    {
        [Header("Настройки головоломки")]
        [Tooltip("Заполняется автоматически генератором сетки. Вручную — только если не используешь генератор")]
        [SerializeField] private List<PuzzleSlot> requiredSlots = new List<PuzzleSlot>();

        [Header("События")]
        [SerializeField] private UnityEvent OnSolved;
        [SerializeField] private UnityEvent OnFailed;

        private bool isPuzzleSolved = false;

        /// <summary>Решена ли головоломка прямо сейчас.</summary>
        public bool IsSolved => isPuzzleSolved;

        /// <summary>Вызывается при каждой смене решено/не решено. Слушает DoorGate.</summary>
        public event Action StateChanged;

        private void OnEnable()
        {
            PuzzleSlot.OnSlotStateChanged += CheckPuzzleStatus;
        }

        private void OnDisable()
        {
            PuzzleSlot.OnSlotStateChanged -= CheckPuzzleStatus;
        }

        private void CheckPuzzleStatus()
        {
            if (requiredSlots.Count == 0) return;

            int currentCorrectSlots = 0;
            foreach (var slot in requiredSlots)
            {
                if (slot != null && slot.IsConditionMet)
                    currentCorrectSlots++;
            }

            if (currentCorrectSlots == requiredSlots.Count)
            {
                if (!isPuzzleSolved)
                {
                    isPuzzleSolved = true;
                    Debug.Log($"<color=green>[SolutionsChecker] {name}: головоломка РЕШЕНА!</color>");
                    OnSolved?.Invoke();
                    StateChanged?.Invoke();
                }
            }
            else
            {
                if (isPuzzleSolved)
                {
                    isPuzzleSolved = false;
                    Debug.Log($"<color=red>[SolutionsChecker] {name}: головоломка СЛОМАНА!</color>");
                    OnFailed?.Invoke();
                    StateChanged?.Invoke();
                }
            }
        }

        public void SetRequiredSlots(List<PuzzleSlot> slots)
        {
            requiredSlots = slots;
            isPuzzleSolved = false;
        }
    }
}