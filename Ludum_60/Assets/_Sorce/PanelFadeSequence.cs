using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Включает канвас (с первой панелью), затем плавно проявляет вторую панель-картинку.
/// Вызов: GetComponent<CanvasPanelSequence>().Show();
/// </summary>
public class CanvasPanelSequence : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private GameObject canvasObject; // объект Canvas, который включается
    [SerializeField] private Image secondPanel;       // вторая панель (обычный Image)

    [Header("Тайминги")]
    [SerializeField] private float delayBeforeSecond = 0.5f; // пауза перед появлением
    [SerializeField] private float fadeDuration = 1.5f;      // длительность проявления

    private Coroutine routine;
    private float targetAlpha;

    private void Awake()
    {
        // Запоминаем альфу, которая стоит у картинки в редакторе
        targetAlpha = secondPanel.color.a;

        if (canvasObject != null) canvasObject.SetActive(false);
    }

    public void Show()
    {
        if (routine != null) StopCoroutine(routine);

        canvasObject.SetActive(true); // первая панель появляется сразу

        SetAlpha(0f);                 // вторая стартует невидимой
        routine = StartCoroutine(FadeInSecondPanel());
    }

    public void Hide()
    {
        if (routine != null) StopCoroutine(routine);
        canvasObject.SetActive(false);
    }

    private IEnumerator FadeInSecondPanel()
    {
        // Realtime — работает и при Time.timeScale = 0
        yield return new WaitForSecondsRealtime(delayBeforeSecond);

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(0f, targetAlpha, t / fadeDuration));
            yield return null;
        }

        SetAlpha(targetAlpha);
        routine = null;
    }

    private void SetAlpha(float a)
    {
        Color c = secondPanel.color;
        c.a = a;
        secondPanel.color = c;
    }
}