using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Одна клетка поля. Вешается на префаб с Button + Image + TMP_Text.</summary>
[RequireComponent(typeof(Button))]
public class BreachCell : MonoBehaviour
{
    [SerializeField] Image background;
    [SerializeField] TMP_Text label;

    [Header("Цвета")]
    [SerializeField] Color normalColor = new Color(0.08f, 0.12f, 0.12f, 0.9f);
    [SerializeField] Color availableColor = new Color(0.85f, 0.95f, 0.2f, 0.35f);
    [SerializeField] Color usedColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
    [SerializeField] Color textColor = new Color(0.7f, 1f, 0.3f, 1f);
    [SerializeField] Color usedTextColor = new Color(0.3f, 0.35f, 0.3f, 1f);

    Button button;

    public Vector2Int Pos { get; private set; } // x = столбец, y = строка
    public string Code { get; private set; }
    public bool Used { get; private set; }

    public void Init(Vector2Int pos, string code, Action<BreachCell> onClick)
    {
        button = GetComponent<Button>();
        Pos = pos;
        Code = code;
        label.text = code;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick(this));

        ResetState();
    }

    public void ResetState()
    {
        Used = false;
        label.color = textColor;
        background.color = normalColor;
    }

    public void MarkUsed()
    {
        Used = true;
        label.color = usedTextColor;
        background.color = usedColor;
    }

    public void SetAvailable(bool available)
    {
        if (Used) return;
        background.color = available ? availableColor : normalColor;
    }
}
