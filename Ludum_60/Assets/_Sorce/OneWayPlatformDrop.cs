using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class OneWayPlatformDrop : MonoBehaviour
{
    [Header("Игрок")]
    [SerializeField] private LayerMask playerLayer;

    [Header("Ввод")]
    [SerializeField] private Key[] dropKeys = { Key.S, Key.DownArrow };

    [Header("Проваливание")]
    [Tooltip("Страховочный таймер, если игрок почему-то не вышел из зоны")]
    [SerializeField] private float maxDropDuration = 1f;
    [Tooltip("Импульс вниз, чтобы падал резко, а не сползал. 0 — без импульса")]
    [SerializeField] private float downwardImpulse = 2f;

    [SerializeField]private Collider2D triggerCollider;
    [SerializeField]private EdgeCollider2D platformSolidCollider;
    private PlatformEffector2D effector;

    // Коллайдеры игроков, которые сейчас в зоне
    private readonly HashSet<Collider2D> playersInside = new HashSet<Collider2D>();
    private readonly Dictionary<Collider2D, Coroutine> activeDrops = new Dictionary<Collider2D, Coroutine>();

    private void Awake()
    {
        triggerCollider.isTrigger = true;

        effector = GetComponent<PlatformEffector2D>();
        if (effector == null)
            effector = GetComponentInParent<PlatformEffector2D>();

    }



    private void OnDisable()
    {
        StopAllCoroutines();

        // Сбросить все отключённые коллизии, чтобы не оставить «призраков»
        foreach (var playerCol in activeDrops.Keys)
        {
            if (playerCol != null && platformSolidCollider != null)
                Physics2D.IgnoreCollision(playerCol, platformSolidCollider, false);
        }

        activeDrops.Clear();
        playersInside.Clear();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        bool downPressed = false;
        foreach (var key in dropKeys)
        {
            if (kb[key].wasPressedThisFrame)
            {
                downPressed = true;
                break;
            }
        }

        if (!downPressed) return;

        // Для каждого игрока в зоне, который ещё не падает — запускаем проваливание
        foreach (var playerCol in playersInside)
        {
            if (playerCol == null) continue;
            if (activeDrops.ContainsKey(playerCol)) continue;

            var rb = playerCol.attachedRigidbody;
            if (rb == null) continue;

            var routine = StartCoroutine(DropRoutine(playerCol, rb));
            activeDrops[playerCol] = routine;
        }
    }

    private IEnumerator DropRoutine(Collider2D playerCol, Rigidbody2D rb)
    {
        if (platformSolidCollider == null)
            yield break;

        // 1. Отключаем столкновение именно с этой платформой
        Physics2D.IgnoreCollision(playerCol, platformSolidCollider, true);

        // 2. Толкаем вниз, чтобы не «сползал»
        if (downwardImpulse > 0f)
            rb.AddForce(Vector2.down * downwardImpulse, ForceMode2D.Impulse);

        // 3. Ждём, пока игрок выйдет из триггер-зоны ИЛИ сработает страховка
        float elapsed = 0f;
        while (elapsed < maxDropDuration)
        {
            // Если игрок вышел из зоны — восстановим коллизию и закончим
            if (!playersInside.Contains(playerCol) || playerCol == null)
                break;

            // Если игрок уже ниже платформы — тоже можно восстанавливать
            if (playerCol.bounds.max.y < platformSolidCollider.bounds.min.y)
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 4. Восстанавливаем
        if (playerCol != null && platformSolidCollider != null)
            Physics2D.IgnoreCollision(playerCol, platformSolidCollider, false);

        activeDrops.Remove(playerCol);
    }

    private bool IsPlayer(Collider2D other)
    {
        return (playerLayer.value & (1 << other.gameObject.layer)) != 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        playersInside.Add(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;
        playersInside.Remove(other);
    }
}