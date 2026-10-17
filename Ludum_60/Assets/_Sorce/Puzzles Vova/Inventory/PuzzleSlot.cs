using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System; // Добавили для Action

namespace DragItem
{
    public class PuzzleSlot : Slot
    {
        [Header("Настройки головоломки")]
        [SerializeField] private bool isTriggerSlot = true;
        [SerializeField] private ItemsState requiredItemState;

        [Header("Состояние слота")]
        [SerializeField] private bool isConditionMet = false;

        // СОБЫТИЕ: вызывается каждый раз, когда состояние слота меняется
        public static event Action OnSlotStateChanged;

        public bool IsConditionMet
        {
            get
            {
                if (!isTriggerSlot) return true;
                return isConditionMet;
            }
        }

        public override void PlaceItem(GameObject item)
        {
            base.PlaceItem(item);

            SubjectDefinition subject = item.GetComponent<SubjectDefinition>();

            if (subject != null)
            {
                if (subject.state == requiredItemState)
                {
                    isConditionMet = true;
                    Debug.Log($"[{gameObject.name}] Правильная лампочка вставлена!");
                }
                else
                {
                    isConditionMet = false;
                    Debug.Log($"[{gameObject.name}] Лампочка не подходит.");
                }
            }
            else
            {
                isConditionMet = false;
            }

            // Оповещаем менеджер изменений
            OnSlotStateChanged?.Invoke();
        }

        public override void ClearSlot()
        {
            base.ClearSlot();
            isConditionMet = false;

            // Оповещаем менеджер изменений
            OnSlotStateChanged?.Invoke();
        }
    }
}
