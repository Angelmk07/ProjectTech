using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Полноэкранная записка/подсказка. Один экземпляр на сцену.
/// Скрипт повесьте на всегда активный объект (например, Canvas),
/// а полноэкранную панель (panel) сделайте дочерней: она включается и выключается.
/// Структура панели: фон на весь экран, TMP_Text для текста, Button для закрытия.
/// </summary>
public class NoteViewUI : MonoBehaviour
{
    public static NoteViewUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [Tooltip("Заголовок записки (необязательно)")]
    [SerializeField] private TMP_Text titleLabel;
    [SerializeField] private TMP_Text bodyLabel;
    [SerializeField] private Button closeButton;

    [Header("Дополнительно")]
    [Tooltip("Action закрытия с клавиатуры, например Esc (необязательно)")]
    [SerializeField] private InputActionReference closeAction;
    [Tooltip("Ставить игру на паузу, пока записка открыта")]
    [SerializeField] private bool pauseWhileOpen = false;

    public bool IsOpen { get; private set; }

    private Action onClosed;
    private int openedFrame;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }

    private void OnEnable()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (closeAction != null) closeAction.action.Enable();
    }

    private void OnDisable()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (IsOpen && pauseWhileOpen) Time.timeScale = previousTimeScale;
    }

    private void Update()
    {
        if (!IsOpen || closeAction == null) return;

        // Не закрываем в тот же кадр, когда открыли
        if (Time.frameCount == openedFrame) return;

        if (closeAction.action.WasPressedThisFrame()) Close();
    }

    /// <summary>Открыть записку. onClosed вызывается при закрытии.</summary>
    public void Show(string title, string body, Action onClosed = null)
    {
        if (IsOpen) return;

        IsOpen = true;
        openedFrame = Time.frameCount;
        this.onClosed = onClosed;

        if (titleLabel != null)
        {
            titleLabel.text = title;
            titleLabel.gameObject.SetActive(!string.IsNullOrEmpty(title));
        }

        if (bodyLabel != null) bodyLabel.text = body;
        if (panel != null) panel.SetActive(true);

        if (pauseWhileOpen)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        if (panel != null) panel.SetActive(false);
        if (pauseWhileOpen) Time.timeScale = previousTimeScale;

        var callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }
}
