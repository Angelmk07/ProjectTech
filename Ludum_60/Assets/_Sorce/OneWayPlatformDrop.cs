
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlatformEffector2D))]
public class OneWayPlatformDrop : MonoBehaviour
{
    [Header("Игрок")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Ввод")]
    [SerializeField, Range(0.1f, 1f)] private float downThreshold = 0.5f;

    [Header("Проваливание")]
    [SerializeField] private float dropDuration = 0.4f;

    private PlatformEffector2D effector;
    private bool playerInside;
    private bool isDropping;
    private bool downWasHeld;

    private void Awake()
    {
        effector = GetComponent<PlatformEffector2D>();
    }

    private void OnDisable()
    {
        if (effector != null)
            effector.rotationalOffset = 0f;

        StopAllCoroutines();

        playerInside = false;
        isDropping = false;
        downWasHeld = false;
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;

        if (kb == null)
            return;

        bool downHeld = kb.sKey.isPressed || kb.downArrowKey.isPressed;
        bool downPressed = downHeld && !downWasHeld;

        downWasHeld = downHeld;

        if (downPressed && playerInside && !isDropping)
            StartCoroutine(DropRoutine());
    }

    private IEnumerator DropRoutine()
    {
        isDropping = true;
        effector.rotationalOffset = 180f;

        yield return new WaitForSeconds(dropDuration);

        effector.rotationalOffset = 0f;
        isDropping = false;
    }

    private bool IsPlayer(Collider2D other)
    {
        return (playerLayer.value & (1 << other.gameObject.layer)) != 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other))
            playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsPlayer(other))
            playerInside = false;
    }
}
