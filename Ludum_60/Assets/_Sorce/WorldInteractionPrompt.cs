
using TMPro;
using UnityEngine;

public class WorldInteractionPrompt : MonoBehaviour
{
    [SerializeField] private InteractionDetector detector;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1f, 0f);

    private void Awake()
    {
        if (detector == null)
            detector = GetComponent<InteractionDetector>();

        if (promptText != null)
            promptText.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (detector == null || promptText == null)
            return;

        IInteractable target = detector.Current;

        if (target == null)
        {
            promptText.gameObject.SetActive(false);
            return;
        }

        promptText.gameObject.SetActive(true);
        promptText.text = $"[E] {target.PromptText}";
        promptText.transform.position =
            target.PromptAnchor.position + worldOffset;
    }
}
