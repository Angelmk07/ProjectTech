using UnityEngine;
using UnityEngine.Events;

public class InterectibleSceneLoad : InteractableBase
{
    [SerializeField] private string enterPrompt = "Залесть";

    [Header("Что произойдёт после взлома именно этого терминала")]
    [SerializeField] private UnityEvent onInteract;

    [Header("Смена сцены")]
    [Tooltip("Имя сцены из Build Settings. Пусто = следующая сцена по списку.")]
    [SerializeField] private string sceneName;

    [Tooltip("Скорость затемнения: 1 = за 1 сек, 2 = за 0.5 сек, 0.5 = за 2 сек.")]
    [SerializeField] private float fadeOutSpeed = 2f;

    protected override void OnInteract(InteractionDetector who)
    {
        ScreenFader.LoadScene(sceneName, fadeOutSpeed);
    }
}