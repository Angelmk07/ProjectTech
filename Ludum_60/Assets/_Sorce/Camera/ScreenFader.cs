using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Чёрный экран поверх всей игры и переходы: смена сцены, телепорт внутри сцены.
/// Создаётся сам при первом обращении и живёт между сценами — в сцену добавлять ничего не нужно.
///
/// Скорость: 1 = полное затемнение за 1 сек, 2 = за 0.5 сек, 0.5 = за 2 сек.
/// Время не зависит от паузы (Time.timeScale = 0).
/// </summary>
public class ScreenFader : MonoBehaviour
{
    private const float FallbackFadeInSpeed = 1.5f;
    private const float MinSpeed = 0.05f;
    private const float MaxStep = 0.05f; // защита от скачка кадра после тяжёлой загрузки сцены

    private static ScreenFader instance;

    /// <summary>Экран сейчас затемнён хотя бы частично.</summary>
    public static bool IsDark => instance != null && instance.canvasGroup.alpha > 0.01f;

    /// <summary>Идёт переход (затемнение, телепорт или осветление). Новые переходы в это время игнорируются.</summary>
    public static bool IsBusy { get; private set; }

    private static ScreenFader Instance
    {
        get
        {
            if (instance == null) new GameObject("ScreenFader").AddComponent<ScreenFader>();
            return instance;
        }
    }

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Coroutine current;
    private bool sceneLoadPending;
    private bool fadeInClaimed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        IsBusy = false;
    }

    // ───────────── Команды ─────────────

    /// <summary>
    /// Затемнить экран и загрузить сцену. Пустое имя = следующая сцена из Build Settings.
    /// Осветляет экран CameraFadeIn на камере новой сцены.
    /// </summary>
    public static bool LoadScene(string sceneName, float fadeOutSpeed)
    {
        if (IsBusy) return false;

        Action load;

        if (string.IsNullOrEmpty(sceneName))
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError("[ScreenFader] Следующей сцены нет в Build Settings.");
                return false;
            }

            load = () => SceneManager.LoadScene(nextIndex);
        }
        else
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[ScreenFader] Сцены '{sceneName}' нет в Build Settings.");
                return false;
            }

            load = () => SceneManager.LoadScene(sceneName);
        }

        ScreenFader fader = Instance;
        fader.Run(fader.SceneRoutine(load, fadeOutSpeed));
        return true;
    }

    /// <summary>Затемнить → выполнить atBlack на чёрном экране → осветлить. Возвращает false, если переход уже идёт.</summary>
    public static bool Transition(float fadeOutSpeed, float fadeInSpeed, Action atBlack)
    {
        if (IsBusy) return false;

        ScreenFader fader = Instance;
        fader.Run(fader.TransitionRoutine(fadeOutSpeed, fadeInSpeed, atBlack));
        return true;
    }

    /// <summary>
    /// Вызывает камера новой сцены (CameraFadeIn): держит чёрный экран holdTime сек,
    /// затем плавно делает его прозрачным и выключает.
    /// </summary>
    public static void FadeInFromBlack(float speed, float holdTime)
    {
        ScreenFader fader = Instance;
        fader.fadeInClaimed = true;
        fader.Run(fader.FadeInRoutine(speed, holdTime));
    }

    // ───────────── Внутренности ─────────────

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        BuildOverlay();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance != this) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
        IsBusy = false;
    }

    private void BuildOverlay()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        gameObject.AddComponent<GraphicRaycaster>(); // чёрный экран гасит клики по UI во время перехода
        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        var blackObject = new GameObject("Black", typeof(RectTransform));
        blackObject.transform.SetParent(transform, false);

        var image = blackObject.AddComponent<Image>();
        image.color = Color.black;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        SetAlpha(0f);
    }

    private void SetAlpha(float alpha)
    {
        canvasGroup.alpha = alpha;
        canvasGroup.blocksRaycasts = alpha > 0f;
        canvas.enabled = alpha > 0f; // при полной прозрачности чёрный экран выключен совсем
    }

    private void Run(IEnumerator routine)
    {
        if (current != null) StopCoroutine(current);
        current = StartCoroutine(routine);
    }

    private IEnumerator Fade(float target, float speed)
    {
        speed = Mathf.Max(speed, MinSpeed);
        float alpha = canvasGroup.alpha;

        while (!Mathf.Approximately(alpha, target))
        {
            alpha = Mathf.MoveTowards(alpha, target, speed * Mathf.Min(Time.unscaledDeltaTime, MaxStep));
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(target);
    }

    private IEnumerator TransitionRoutine(float fadeOutSpeed, float fadeInSpeed, Action atBlack)
    {
        IsBusy = true;
        yield return Fade(1f, fadeOutSpeed);

        try { atBlack?.Invoke(); }
        catch (Exception e) { Debug.LogException(e); }

        // Даём физике обработать перенос, чтобы триггеры в точке прибытия не сработали сразу.
        if (Time.timeScale > 0f)
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }

        yield return Fade(0f, fadeInSpeed);
        IsBusy = false;
    }

    private IEnumerator SceneRoutine(Action load, float fadeOutSpeed)
    {
        IsBusy = true;
        yield return Fade(1f, fadeOutSpeed);

        sceneLoadPending = true;
        load(); // экран остаётся чёрным — осветлит камера новой сцены
    }

    private IEnumerator FadeInRoutine(float speed, float holdTime)
    {
        IsBusy = true;
        SetAlpha(1f);

        if (holdTime > 0f) yield return new WaitForSecondsRealtime(holdTime);

        yield return Fade(0f, speed);
        IsBusy = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!sceneLoadPending) return;

        sceneLoadPending = false;
        fadeInClaimed = false;
        StartCoroutine(FallbackFadeIn());
    }

    // Страховка: если на камере новой сцены нет CameraFadeIn, экран не должен остаться чёрным навсегда.
    private IEnumerator FallbackFadeIn()
    {
        yield return null;
        yield return null;

        if (fadeInClaimed) yield break;

        Debug.LogWarning("[ScreenFader] На камере новой сцены нет CameraFadeIn — осветляю экран автоматически.");
        Run(FadeInRoutine(FallbackFadeInSpeed, 0f));
    }
}
