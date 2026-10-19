
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldInteractionPrompt : MonoBehaviour
{
    [SerializeField] private InteractionDetector detector;

    [SerializeField] private TMP_Text promptText;
    [SerializeField] private Image Image;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1f, 0f);

    private void Awake()
    {
        if (detector == null)
            detector = GetComponent<InteractionDetector>();

        if (Image != null)
            Image.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (detector == null || Image == null)
            return;

        IInteractable target = detector.Current;

        if (target == null)
        {
            Image.gameObject.SetActive(false);
            return;
        }

        Image.gameObject.SetActive(true);
        promptText.text = $"[E] {target.PromptText}";
        Image.transform.position = target.PromptAnchor.position + worldOffset;
    }
}
