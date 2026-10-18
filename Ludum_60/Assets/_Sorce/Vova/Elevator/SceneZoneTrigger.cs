using UnityEngine;

public class SceneZoneTrigger : MonoBehaviour
{
    [SerializeField] private string sceneName;
    [SerializeField] private KeyCode key = KeyCode.E;

    private bool playerInside;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }

    private void Update()
    {
        if (playerInside && Input.GetKeyDown(key))
            SceneChanger.Instance.LoadSceneWithFade(sceneName);
    }
}