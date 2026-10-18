using UnityEngine;
using UnityEngine.EventSystems;

namespace DragItem
{
    public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;

        private Slot oldSlot;
        private Slot currentSlot;
        private Vector3 startPosition;

        private void Start()
        {
            rectTransform = GetComponent<RectTransform>();

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            oldSlot = GetComponentInParent<Slot>();
            if (oldSlot != null)
            {
                startPosition = transform.position;
                oldSlot.ClearSlot();
            }

            canvasGroup.blocksRaycasts = false;
            transform.SetParent(transform.root);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint
            );
            rectTransform.localPosition = localPoint;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.blocksRaycasts = true;

            if (currentSlot != null && currentSlot.CanPlaceItem())
            {
                currentSlot.PlaceItem(gameObject);
            }
            else
            {
                ReturnToOldSlot();
            }
        }

        public void SetNewSlot(Slot slot)
        {
            currentSlot = slot;
        }

        private void ReturnToOldSlot()
        {
            currentSlot = null;
            if (oldSlot != null)
            {
                oldSlot.PlaceItem(gameObject);
            }
            else
            {
                transform.position = startPosition;
            }
        }
    }
}