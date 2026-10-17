using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private InteractionDetector detector;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text keyLabel;
    [SerializeField] private TMP_Text actionLabel;
    [Tooltip("Image типа Filled для прогресса удержания (необязательно)")]
    [SerializeField] private Image holdFill;
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 40f);

    private IInteractable target;

    private void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        SetVisible(false);
        SetHold(0f);
    }

    private void OnEnable()
    {
        detector.TargetChanged += OnTargetChanged;
        detector.HoldProgressChanged += SetHold;
    }

    private void OnDisable()
    {
        detector.TargetChanged -= OnTargetChanged;
        detector.HoldProgressChanged -= SetHold;
    }

    private void OnTargetChanged(IInteractable newTarget)
    {
        target = newTarget;
        SetVisible(target != null);

        if (target != null && keyLabel != null && detector.Action != null)
            keyLabel.text = detector.Action.GetBindingDisplayString();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (actionLabel != null && actionLabel.text != target.PromptText)
            actionLabel.text = target.PromptText;

        Vector3 screen = worldCamera.WorldToScreenPoint(target.PromptAnchor.position);
        root.position = screen + (Vector3)screenOffset;
    }

    private void SetVisible(bool value)
    {
        if (root != null) root.gameObject.SetActive(value);
    }

    private void SetHold(float value)
    {
        if (holdFill != null) holdFill.fillAmount = value;
    }
}
