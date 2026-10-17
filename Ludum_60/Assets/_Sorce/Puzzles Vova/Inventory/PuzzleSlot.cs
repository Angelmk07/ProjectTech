using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DragItem
{
    public class PuzzleSlot : Slot
    {
        [Header("Настройки головоломки")]
        [SerializeField] private bool isTriggerSlot = true; // Влияет ли этот слот на головоломку?
        [SerializeField] private ItemsState requiredItemState; // Какая лампочка здесь должна быть

        [Header("Состояние слота")]
        [SerializeField] private bool isConditionMet = false; // Пройдена ли проверка в этом слоте

        // Свойство для получения статуса слота из других скриптов (например, из менеджера головоломки)
        public bool IsConditionMet
        {
            get
            {
                // Если слот не влияет на головоломку, он всегда возвращает true (не мешает победе)
                if (!isTriggerSlot) return true;
                return isConditionMet;
            }
        }

        // Переопределяем метод размещения предмета, чтобы сразу делать проверку
        public override void PlaceItem(GameObject item)
        {
            base.PlaceItem(item);

            // Ищем компонент определения предмета на пришедшем GameObject
            SubjectDefinition subject = item.GetComponent<SubjectDefinition>();

            if (subject != null)
            {
                // Сравниваем state лампочки с требуемым в этом слоте
                if (subject.state == requiredItemState)
                {
                    isConditionMet = true;
                    Debug.Log($"[{gameObject.name}] Правильная лампочка вставлена!");
                }
                else
                {
                    isConditionMet = false;
                    Debug.Log($"[{gameObject.name}] Лампочка вставлена, но она не подходит для решения.");
                }
            }
            else
            {
                // Если предмет без скрипта SubjectDefinition, условие не выполнено
                isConditionMet = false;
            }
        }

        // Переопределяем очистку слота (когда лампочку забрали)
        public override void ClearSlot()
        {
            base.ClearSlot();
            isConditionMet = false; // Лампочку забрали -> условие больше не выполнено
        }
    }
}