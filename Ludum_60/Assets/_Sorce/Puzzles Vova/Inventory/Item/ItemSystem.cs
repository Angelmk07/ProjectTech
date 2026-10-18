using System.Collections.Generic;
using UnityEngine;

public enum ItemsState
{
    NoLightBulb,
    BulbRed,
    BulbBlue,
    PurpleLightBulb,
    GreenLightBulb,
    Yellowlightbulb,
    Bluelightbulb
}
public class ItemSystem : MonoBehaviour
{
    private List<Item> items = new List<Item>
    {
        new Item("НетЛампочки", "NoLightBulb"),
        new Item("ЛампочкаКрасная", "BulbRed"),
        new Item("ЛампочкаСиняя", "BulbBlue"),
        new Item("ЛампочкаФиолетовая","PurpleLightBulb"),
        new Item("ЛампочкаЗелённаяя","GreenLightBulb"),
        new Item("ЛампочкаЖёлтая", "Yellowlightbulb"),
        new Item("ЛампочкаГолубая", "Bluelightbulb")
    };
}
