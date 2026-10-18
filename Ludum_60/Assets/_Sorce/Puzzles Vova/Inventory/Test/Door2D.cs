using System.Collections;
using UnityEngine;

namespace DragItem
{
    public class Door2D : MonoBehaviour
    {
        [Header("Настройки движения")]
        [Tooltip("На сколько единиц вверх поднимется дверь при открытии")]
        [SerializeField] private float moveDistance = 2f;

        [Tooltip("Скорость открытия/закрытия двери")]
        [SerializeField] private float speed = 3f;

        [Header("Компоненты")]
        [SerializeField] private Collider2D doorCollider;

        private Vector3 closedPosition;
        private Vector3 openedPosition;
        private Coroutine moveCoroutine;

        private void Start()
        {
            // Запоминаем стартовую позицию (закрыто)
            closedPosition = transform.position;
            // Вычисляем целевую позицию (открыто)
            openedPosition = closedPosition + Vector3.up * moveDistance;

            // Если коллайдер не назначен вручную, пытаемся найти его на этом же объекте
            if (doorCollider == null)
            {
                doorCollider = GetComponent<Collider2D>();
            }
        }

        // Этот метод мы привяжем к OnSolved
        public void OpenDoor()
        {
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(MoveDoorRoutine(openedPosition, false));
        }

        // Этот метод мы привяжем к OnFailed
        public void CloseDoor()
        {
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(MoveDoorRoutine(closedPosition, true));
        }

        // Корутина для плавного перемещения
        private IEnumerator MoveDoorRoutine(Vector3 targetPosition, bool shouldEnableCollider)
        {
            // Если мы открываем дверь, отключаем коллайдер сразу, чтобы игрок мог пройти
            if (!shouldEnableCollider && doorCollider != null)
            {
                doorCollider.enabled = false;
            }

            // Плавно двигаем кубик к цели
            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
                yield return null;
            }

            // Устанавливаем точную позицию в конце движения
            transform.position = targetPosition;

            // Если мы закрыли дверь, включаем коллайдер обратно
            if (shouldEnableCollider && doorCollider != null)
            {
                doorCollider.enabled = true;
            }
        }
    }
}
