
using System;
using UnityEngine;
using UnityEngine.UI;

public class SkillCheckController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject skillCheckPanel;
    [SerializeField] private RectTransform needle;

    [Header("Default Settings")]
    [SerializeField] private SkillCheckSettings defaultSettings;

    public event Action OnSuccess;
    public event Action OnGreat;
    public event Action OnFail;
    public event Action OnMiss;

    private SkillCheckSettings currentSettings;
    private Action successCallback;
    private Action greatCallback;
    private Action failCallback;
    private Action missCallback;

    private float elapsedAngle;
    private bool isRunning;

    public bool IsRunning => isRunning;

    private void Awake()
    {
        if (skillCheckPanel != null)
            skillCheckPanel.SetActive(false);
    }

    private void Update()
    {
        if (!isRunning)
            return;

        elapsedAngle += currentSettings.rotationSpeed * Time.deltaTime;

        needle.localRotation = Quaternion.Euler(
            0f, 0f, -elapsedAngle
        );

        if (Input.GetKeyDown(currentSettings.inputKey))
        {
            CheckInput();
            return;
        }

        if (!currentSettings.allowMultipleRotations &&
            elapsedAngle >= 360f)
        {
            FinishMiss();
        }
    }

    public void StartSkillCheck(
        SkillCheckSettings settings = null,
        Action onSuccess = null,
        Action onGreat = null,
        Action onFail = null,
        Action onMiss = null)
    {
        if (isRunning)
            return;

        currentSettings = settings != null
            ? settings
            : defaultSettings;

        if (currentSettings == null ||
            skillCheckPanel == null ||
            needle == null)
        {
            Debug.LogError(
                "Skill Check: не назначены необходимые ссылки или настройки."
            );
            return;
        }

        successCallback = onSuccess;
        greatCallback = onGreat;
        failCallback = onFail;
        missCallback = onMiss;

        elapsedAngle = 0f;
        needle.localRotation = Quaternion.identity;

        isRunning = true;
        skillCheckPanel.SetActive(true);
    }

    private void CheckInput()
    {
        float angle = Mathf.Repeat(elapsedAngle, 360f);

        float distance = Mathf.Abs(
            Mathf.DeltaAngle(
                angle,
                currentSettings.zoneCenterAngle
            )
        );

        float greatRadius = currentSettings.greatZoneSize / 2f;
        float successRadius = currentSettings.successZoneSize / 2f;

        if (distance <= greatRadius)
        {
            FinishGreat();
        }
        else if (distance <= successRadius)
        {
            FinishSuccess();
        }
        else if (currentSettings.failOnWrongPress)
        {
            FinishFail();
        }
    }

    private void FinishSuccess()
    {
        Finish();
        OnSuccess?.Invoke();
        successCallback?.Invoke();
    }

    private void FinishGreat()
    {
        Finish();
        OnGreat?.Invoke();
        greatCallback?.Invoke();
    }

    private void FinishFail()
    {
        Finish();
        OnFail?.Invoke();
        failCallback?.Invoke();
    }

    private void FinishMiss()
    {
        Finish();
        OnMiss?.Invoke();
        missCallback?.Invoke();
    }

    private void Finish()
    {
        isRunning = false;

        if (skillCheckPanel != null)
            skillCheckPanel.SetActive(false);

        successCallback = null;
        greatCallback = null;
        failCallback = null;
        missCallback = null;
    }

    public void CancelSkillCheck()
    {
        if (!isRunning)
            return;

        Finish();
    }
}
