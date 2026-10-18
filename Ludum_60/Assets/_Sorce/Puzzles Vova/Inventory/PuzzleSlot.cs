using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace DragItem
{
    public class PuzzleSlot : Slot
    {
        [Header("Настройки головоломки")]
        [SerializeField] private ItemsState requiredItemState;

        [Header("Состояние слота")]
        [SerializeField] private bool isConditionMet = false;
        [SerializeField] private GameObject currentItem; // Ссылка на текущий предмет в слоте

        // СОБЫТИЕ: вызывается каждый раз, когда состояние слота меняется
        public static event Action OnSlotStateChanged;

        public bool IsConditionMet => isConditionMet;

        private void Update()
        {
            // ЗАЩИТА: Если предмет физически утащили из слота руками, 
            // а метод ClearSlot() не вызвался — сбрасываем состояние.
            if (currentItem != null && currentItem.transform.parent != transform)
            {
                ClearSlot();
            }
        }

        public override void PlaceItem(GameObject item)
        {
            base.PlaceItem(item);
            currentItem = item; // Запоминаем предмет

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

            OnSlotStateChanged?.Invoke();
        }

        public override void ClearSlot()
        {
            base.ClearSlot();
            if (isConditionMet || currentItem != null)
            {
                currentItem = null;
                isConditionMet = false;
                Debug.Log($"[{gameObject.name}] Слот очищен.");
                OnSlotStateChanged?.Invoke();
            }
        }
    }
}