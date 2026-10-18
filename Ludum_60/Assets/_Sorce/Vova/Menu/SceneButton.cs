using UnityEngine;

public class SceneButton : MonoBehaviour
{
    [SerializeField] private string sceneName;

    // Повесить на OnClick кнопки
    public void OnClick()
    {
        SceneChanger.Instance.LoadScene(sceneName);
    }
}