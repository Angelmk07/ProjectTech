using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public static SceneChanger Instance { get; private set; }

    [SerializeField] private CanvasGroup fadeGroup;      // CanvasGroup на чёрной панели
    [SerializeField] private float fadeDuration = 1f;

    private bool isChanging;

    private void Awake()
    {
        // Если из другой сцены уже пришёл такой же объект — удаляем дубликат
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
    }

    // Обычное переключение (кнопка Play)
    public void LoadScene(string sceneName)
    {
        if (isChanging) return;
        SceneManager.LoadScene(sceneName);
    }

    // Переключение с затемнением (зона на уровне)
    public void LoadSceneWithFade(string sceneName)
    {
        if (isChanging) return;
        StartCoroutine(FadeRoutine(sceneName));
    }

    // Один общий метод, если нужен параметр
    public void ChangeScene(string sceneName, bool useFade)
    {
        if (useFade) LoadSceneWithFade(sceneName);
        else LoadScene(sceneName);
    }

    private IEnumerator FadeRoutine(string sceneName)
    {
        isChanging = true;
        fadeGroup.blocksRaycasts = true;   // блокируем клики во время перехода

        yield return Fade(0f, 1f);         // затемнение

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;

        yield return Fade(1f, 0f);         // плавное появление

        fadeGroup.blocksRaycasts = false;
        isChanging = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;   // работает и при Time.timeScale = 0
            fadeGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = to;
    }
}