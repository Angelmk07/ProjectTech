using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DragItem
{
    /// <summary>
    /// Единственный инвентарь ламп. Состоит из обычных Slot-ячеек, поэтому лампы
    /// свободно перетаскиваются между инвентарём и ячейками головоломки.
    /// Повесь на ВСЕГДА активный объект (корневой Canvas). Сами ячейки могут лежать
    /// внутри выключаемой панели — лампы подбираются, даже когда панель скрыта.
    /// </summary>
    public class LampInventory : MonoBehaviour
    {
        [Serializable]
        public struct LampVisual
        {
            public ItemsState state;
            public Sprite sprite;
        }

        public static LampInventory Instance { get; private set; }

        [Header("Инвентарь")]
        [Tooltip("Ячейки инвентаря (обычные Slot). Не меньше, чем всего ламп в игре")]
        [SerializeField] private List<Slot> slots = new List<Slot>();
        [Tooltip("Префаб лампы: Image + DraggableItem + SubjectDefinition. Картинка и тип подставляются при подборе")]
        [SerializeField] private DraggableItem lampPrefab;

        [Header("Картинки ламп")]
        [Tooltip("Нужны для подсказок в ячейках головоломки и как запасная картинка, если пикап её не передал")]
        [SerializeField] private List<LampVisual> lampVisuals = new List<LampVisual>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[LampInventory] На сцене уже есть инвентарь ламп.", this);
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public Sprite GetSprite(ItemsState state)
        {
            foreach (var v in lampVisuals)
                if (v.state == state) return v.sprite;
            return null;
        }

        /// <summary>
        /// Создаёт лампу нужного типа с нужной картинкой и кладёт в первую свободную ячейку.
        /// false — если свободных ячеек нет.
        /// </summary>
        public bool AddLamp(ItemsState state, Sprite icon = null)
        {
            if (lampPrefab == null)
            {
                Debug.LogError("[LampInventory] Не назначен Lamp Prefab.", this);
                return false;
            }

            Slot free = FindFreeSlot();
            if (free == null)
            {
                Debug.Log("[LampInventory] Инвентарь полон.");
                return false;
            }

            DraggableItem lamp = Instantiate(lampPrefab, free.transform);
            lamp.name = $"Lamp_{state}";

            SubjectDefinition subject = lamp.GetComponent<SubjectDefinition>();
            if (subject == null) subject = lamp.gameObject.AddComponent<SubjectDefinition>();
            subject.state = state; // тип всегда берётся из пикапа, а не из префаба

            Sprite sprite = icon != null ? icon : GetSprite(state);
            if (sprite != null)
            {
                Image image = lamp.GetComponent<Image>();
                if (image != null) image.sprite = sprite;
            }

            free.PlaceItem(lamp.gameObject);
            lamp.SetNewSlot(free);
            return true;
        }

        /// <summary>Возвращает существующую лампу (например, из закрываемой головоломки) в инвентарь.</summary>
        public bool ReturnLamp(DraggableItem lamp)
        {
            if (lamp == null) return false;

            Slot free = FindFreeSlot();
            if (free == null)
            {
                Debug.LogError("[LampInventory] Нет места, чтобы вернуть лампу. Увеличь число ячеек инвентаря.", this);
                return false;
            }

            free.PlaceItem(lamp.gameObject);
            lamp.SetNewSlot(free);
            return true;
        }

        private Slot FindFreeSlot()
        {
            foreach (var s in slots)
                if (s != null && s.CanPlaceItem()) return s;
            return null;
        }
    }
}