using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneButton : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public void OnClick()
    {
        Debug.Log(SceneManager.loadedSceneCount);
        SceneManager.LoadScene(SceneManager.loadedSceneCount);
    }
}