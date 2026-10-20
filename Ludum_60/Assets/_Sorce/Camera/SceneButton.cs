using UnityEngine;

public class SceneButton : MonoBehaviour
{
    [Tooltip("Имя сцены из Build Settings. Пусто = следующая сцена по списку.")]
    [SerializeField] private string sceneName;

    [Tooltip("Скорость затемнения: 1 = за 1 сек, 2 = за 0.5 сек, 0.5 = за 2 сек.")]
    [SerializeField] private float fadeOutSpeed = 2f;

    public void OnClick()
    {
        ScreenFader.LoadScene(sceneName, fadeOutSpeed);
    }
}
