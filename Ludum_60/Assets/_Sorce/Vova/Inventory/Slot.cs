using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // ОБЯЗАТЕЛЬНО ДЛЯ IDropHandler

namespace DragItem
{
    // Теперь Collider2D НЕ НУЖЕН. Нужен только Image с включенным Raycast Target!
    public class Slot : MonoBehaviour, IDropHandler
    {
        [Header("Компоненты и настройки")]
        public Image slotImage;
        [SerializeField] private int index;

        private GameObject currentItem = null;

        public bool CanPlaceItem() => currentItem == null;

        // Этот метод вызывается автоматически Canvas-системой, когда на слот БРОСАЮТ предмет
        public void OnDrop(PointerEventData eventData)
        {
            // Проверяем, тащили ли мы вообще что-то
            if (eventData.pointerDrag != null)
            {
                DraggableItem draggedItem = eventData.pointerDrag.GetComponent<DraggableItem>();

                // Если это наш предмет и слот свободен
                if (draggedItem != null && CanPlaceItem())
                {
                    draggedItem.SetNewSlot(this); // Передаем слоту управление
                }
            }
        }

        public virtual void PlaceItem(GameObject item)
        {
            if (CanPlaceItem())
            {
                currentItem = item;
                currentItem.transform.SetParent(transform);
                currentItem.transform.localPosition = Vector3.zero;
            }
        }

        public virtual void ClearSlot() => currentItem = null;
        public int GetIndex() => index;
        public void SetIndex(int value) => index = value;
    }
}
