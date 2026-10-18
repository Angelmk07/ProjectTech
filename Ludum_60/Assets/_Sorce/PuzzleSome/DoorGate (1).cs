using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DragItem
{
    /// <summary>
    /// Связывает головоломку (или несколько) с целью: дверью, постом, финалом.
    /// Срабатывает, когда решены ВСЕ головоломки из списка.
    /// Для обычной двери — укажи Door2D. Для остального — подпишись на On Opened.
    /// </summary>
    public class DoorGate : MonoBehaviour
    {
        [Header("Цель")]
        [Tooltip("Необязательно. Если указана — дверь откроется/закроется сама")]
        [SerializeField] private Door2D door;

        [Header("Условие")]
        [Tooltip("Головоломки, которые нужно решить. Обычно одна")]
        [SerializeField] private List<SolutionsChecker> checkers = new List<SolutionsChecker>();
        [Tooltip("Если включено — открывшаяся цель больше не закроется, даже если вынуть лампы")]
        [SerializeField] private bool stayOpen = true;

        [Header("События (пост, финал и т.д.)")]
        [SerializeField] private UnityEvent onOpened;
        [SerializeField] private UnityEvent onClosed;

        private bool isOpen;

        private void OnEnable()
        {
            foreach (var c in checkers)
                if (c != null) c.StateChanged += Evaluate;
        }

        private void OnDisable()
        {
            foreach (var c in checkers)
                if (c != null) c.StateChanged -= Evaluate;
        }

        private void Evaluate()
        {
            if (checkers.Count == 0) return;

            bool allSolved = true;
            foreach (var c in checkers)
            {
                if (c == null || !c.IsSolved)
                {
                    allSolved = false;
                    break;
                }
            }

            if (allSolved && !isOpen)
            {
                isOpen = true;
                if (door != null) door.OpenDoor();
                onOpened?.Invoke();
            }
            else if (!allSolved && isOpen && !stayOpen)
            {
                isOpen = false;
                if (door != null) door.CloseDoor();
                onClosed?.Invoke();
            }
        }
    }
}
