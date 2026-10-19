using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class InterectibleSceneLoad : InteractableBase
{
    [SerializeField] private string enterPrompt = "Залесть";
    [Header("Что произойдёт после взлома именно этого терминала")]
    [SerializeField] private UnityEvent onInteract;
    protected override void OnInteract(InteractionDetector who)
    {
         Debug.Log(SceneManager.loadedSceneCount);
         SceneManager.LoadScene(SceneManager.loadedSceneCount+1);
    }
}