using UnityEngine;
using UnityEngine.UI;
using System;

namespace DragItem
{
    public class PuzzleSlot : Slot
    {
        [Header("Настройки головоломки")]
        [SerializeField] private ItemsState requiredItemState;

        [Header("Подсказка (необязательно)")]
        [Tooltip("Дочерний Image, показывающий, какая лампа нужна в этой ячейке. У него выключить Raycast Target")]
        [SerializeField] private Image hintImage;
        [SerializeField, Range(0f, 1f)] private float hintAlpha = 0.5f;

        [Header("Состояние слота")]
        [SerializeField] private bool isConditionMet = false;
        [SerializeField] private GameObject placedItem; // Ссылка на текущий предмет в слоте

        // СОБЫТИЕ: вызывается каждый раз, когда состояние слота меняется
        public static event Action OnSlotStateChanged;

        public bool IsConditionMet => isConditionMet;
        public ItemsState RequiredState => requiredItemState;

        private void Update()
        {
            // ЗАЩИТА: Если предмет физически утащили из слота руками,
            // а метод ClearSlot() не вызвался — сбрасываем состояние.
            if (placedItem != null && placedItem.transform.parent != transform)
            {
                ClearSlot();
            }
        }

        public override void PlaceItem(GameObject item)
        {
            base.PlaceItem(item);
            placedItem = item; // Запоминаем предмет

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
            if (isConditionMet || placedItem != null)
            {
                placedItem = null;
                isConditionMet = false;
                Debug.Log($"[{gameObject.name}] Слот очищен.");
                OnSlotStateChanged?.Invoke();
            }
        }

        public void SetRequiredState(ItemsState state)
        {
            requiredItemState = state;
            isConditionMet = false;
        }

        /// <summary>Показывает картинку нужной лампы в ячейке. null — скрыть подсказку.</summary>
        public void SetHint(Sprite sprite)
        {
            if (hintImage == null) return;

            hintImage.sprite = sprite;
            hintImage.enabled = sprite != null;

            Color c = hintImage.color;
            c.a = hintAlpha;
            hintImage.color = c;
        }
    }
}