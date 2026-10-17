using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Лестница зигзаг: игрок жмёт E -> проигрывается анимация от художников ->
/// игрок оказывается в точке выхода (на вершине/внизу лестницы).
/// Для длинного спуска из двух лестниц подряд укажи в первой лестнице поле Next Stairs.
/// Повесить на объект лестницы вместе с Collider2D (Is Trigger) и Animator.
/// </summary>
public class ZigzagStairs : MonoBehaviour, IInteractable
{
    [Header("Точка выхода")]
    [SerializeField] private Transform exitPoint;

    [Header("Подсказка")]
    [SerializeField] private string promptText = "Использовать лестницу";
    [Tooltip("Над какой точкой показывать подсказку. Если пусто — над самой лестницей.")]
    [SerializeField] private Transform promptAnchor;
    [Tooltip("0 = обычное нажатие, больше 0 = удерживать кнопку столько секунд.")]
    [SerializeField] private float holdDuration = 0f;

    [Header("Анимация")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animationTrigger = "Use";
    [Tooltip("Если включено — ждём вызова AnimationFinished() из Animation Event в конце клипа. Иначе ждём Animation Duration.")]
    [SerializeField] private bool waitForAnimationEvent = false;
    [SerializeField] private float animationDuration = 2f;

    [Header("Поведение")]
    [Tooltip("Прятать спрайты игрока на время анимации (если персонаж нарисован внутри анимации лестницы).")]
    [SerializeField] private bool hidePlayerDuringAnimation = true;
    [SerializeField] private bool snapCameraAfterTeleport = true;

    [Header("Длинная лестница (необязательно)")]
    [Tooltip("Следующая лестница, которая запускается автоматически сразу после этой.")]
    [SerializeField] private ZigzagStairs nextStairs;

    private bool busy;
    private bool animationDone;


    public string PromptText => promptText;
    public Transform PromptAnchor => promptAnchor != null ? promptAnchor : transform;
    public float HoldDuration => holdDuration;

    public bool CanInteract(InteractionDetector who)
    {
        return !busy && exitPoint != null;
    }

    public void OnFocus(InteractionDetector who) { }

    public void OnUnfocus(InteractionDetector who) { }

    public void Interact(InteractionDetector who)
    {
        if (!CanInteract(who)) return;

        Rigidbody2D rb = who.GetComponentInParent<Rigidbody2D>();
        GameObject player = rb != null ? rb.gameObject : who.gameObject;

        StartCoroutine(UseRoutine(player, rb));
    }

    public void AnimationFinished()
    {
        animationDone = true;
    }


    private IEnumerator UseRoutine(GameObject player, Rigidbody2D rb)
    {
        busy = true;

        var disabledBehaviours = new List<MonoBehaviour>();
        foreach (var mb in player.GetComponentsInChildren<MonoBehaviour>())
        {
            if (mb != null && mb.enabled)
            {
                mb.enabled = false;
                disabledBehaviours.Add(mb);
            }
        }

        bool rbWasSimulated = false;
        if (rb != null)
        {
            StopBody(rb);
            rbWasSimulated = rb.simulated;
            rb.simulated = false;
        }

        var hiddenRenderers = new List<Renderer>();
        if (hidePlayerDuringAnimation)
        {
            foreach (var r in player.GetComponentsInChildren<Renderer>())
            {
                if (r.enabled)
                {
                    r.enabled = false;
                    hiddenRenderers.Add(r);
                }
            }
        }

        ZigzagStairs current = this;
        int safety = 0;
        while (current != null && safety++ < 16)
        {
            current.busy = true;
            yield return current.PlayAnimation();

            if (current.exitPoint != null)
                Teleport(player, rb, current.exitPoint.position);

            if (current.snapCameraAfterTeleport && CameraFollow2D.Instance != null)
                CameraFollow2D.Instance.SnapToTarget();

            ZigzagStairs finished = current;
            current = current.nextStairs;
            if (finished != this) finished.busy = false;
        }

        foreach (var r in hiddenRenderers)
            if (r != null) r.enabled = true;

        if (rb != null)
        {
            rb.simulated = rbWasSimulated;
            StopBody(rb);
        }

        foreach (var mb in disabledBehaviours)
            if (mb != null) mb.enabled = true;

        busy = false;
    }

    private IEnumerator PlayAnimation()
    {
        animationDone = false;

        if (animator != null && !string.IsNullOrEmpty(animationTrigger))
            animator.SetTrigger(animationTrigger);

        if (waitForAnimationEvent)
            yield return new WaitUntil(() => animationDone);
        else
            yield return new WaitForSeconds(animationDuration);
    }

    private static void Teleport(GameObject player, Rigidbody2D rb, Vector3 position)
    {
        position.z = player.transform.position.z;
        player.transform.position = position;
        if (rb != null) rb.position = position;
    }

    private static void StopBody(Rigidbody2D rb)
    {
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void OnDrawGizmos()
    {
        if (exitPoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, exitPoint.position);
        Gizmos.DrawWireSphere(exitPoint.position, 0.25f);
    }
}