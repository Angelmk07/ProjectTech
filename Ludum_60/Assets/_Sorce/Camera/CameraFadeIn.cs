using UnityEngine;

/// <summary>
/// Повесить на камеру. При входе в сцену после затемнения (ScreenFader) чёрный экран
/// плавно становится прозрачным и выключается.
/// </summary>
[DisallowMultipleComponent]
public class CameraFadeIn : MonoBehaviour
{
    [Header("Осветление при входе в сцену")]
    [Tooltip("Скорость осветления: 1 = за 1 сек, 2 = за 0.5 сек, 0.5 = за 2 сек")]
    [SerializeField] private float fadeSpeed = 1.5f;

    [Tooltip("Сколько секунд держать чёрный экран перед осветлением (прячет подвисание сразу после загрузки)")]
    [SerializeField] private float holdBlackTime = 0.15f;

    [Tooltip("Осветлять сцену при запуске, даже если в неё пришли без затемнения (например, самый первый запуск игры)")]
    [SerializeField] private bool fadeInOnEveryStart = false;

    private void Start()
    {
        if (!ScreenFader.IsDark && !fadeInOnEveryStart) return;

        ScreenFader.FadeInFromBlack(fadeSpeed, holdBlackTime);
    }
}
