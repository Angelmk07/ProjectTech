
using UnityEngine;
using System.Collections;

public class BridgeMover : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private Vector2 moveDirection = Vector2.right;
    [SerializeField] private float moveDistance = 3f;
    [SerializeField] private float moveSpeed = 2f;

    [Header("Gizmo")]
    [SerializeField] private Color gizmoColor = Color.green;

    private Vector3 _startPosition;
    private Coroutine _moveCoroutine;
    private bool _isMoved;

    private void Awake()
    {
        _startPosition = transform.position;
    }

    public void MoveBridge()
    {
        if (_moveCoroutine != null)
            return;

        Vector3 targetPosition = _isMoved
            ? _startPosition
            : _startPosition + (Vector3)(moveDirection.normalized * moveDistance);

        _moveCoroutine = StartCoroutine(MoveToPosition(targetPosition));
        _isMoved = !_isMoved;
    }

    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = targetPosition;
        _moveCoroutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 start = Application.isPlaying
            ? _startPosition
            : transform.position;

        Vector3 end = start +
            (Vector3)(moveDirection.normalized * moveDistance);

        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start, 0.15f);
        Gizmos.DrawWireSphere(end, 0.2f);
    }
}
