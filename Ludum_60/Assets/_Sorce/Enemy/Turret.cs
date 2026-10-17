
using UnityEngine;
using System.Collections;

public class Turret : MonoBehaviour
{
    [Header("Настройки поворота")]
    [SerializeField] private Transform _rotationPoint;
    [SerializeField] private float _leftAngle = -45f;
    [SerializeField] private float _rightAngle = 45f;
    [SerializeField] private float _rotationSpeed = 60f;

    [Header("Остановка на краях")]
    [SerializeField] private float _waitTime = 1f;

    private bool _movingRight = true;
    private bool _isWaiting;

    private void Update()
    {
        if (_isWaiting)
            return;

        float targetAngle = _movingRight ? _rightAngle : _leftAngle;

        float currentAngle = _rotationPoint.localEulerAngles.z;
        float nextAngle = Mathf.MoveTowardsAngle(
            currentAngle,
            targetAngle,
            _rotationSpeed * Time.deltaTime
        );

        _rotationPoint.localRotation = Quaternion.Euler(0f, 0f, nextAngle);

        if (Mathf.Abs(Mathf.DeltaAngle(nextAngle, targetAngle)) < 0.1f)
        {
            _rotationPoint.localRotation = Quaternion.Euler(0f, 0f, targetAngle);
            StartCoroutine(WaitAndChangeDirection());
        }
    }

    private IEnumerator WaitAndChangeDirection()
    {
        _isWaiting = true;

        yield return new WaitForSeconds(_waitTime);

        _movingRight = !_movingRight;
        _isWaiting = false;
    }
}
