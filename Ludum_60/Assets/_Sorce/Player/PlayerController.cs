using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    [SerializeField] float moveSpeed = 7f;
    [SerializeField] float acceleration = 60f;
    [SerializeField] float deceleration = 80f;

    [Header("Прыжок")]
    [SerializeField] float jumpForce = 14f;
    [Tooltip("Множитель скорости при отпускании кнопки в полёте вверх (низкий прыжок)")]
    [SerializeField] float jumpCutMultiplier = 0.5f;
    [SerializeField] float coyoteTime = 0.1f;
    [SerializeField] float jumpBufferTime = 0.1f;
    [SerializeField] float fallGravityMultiplier = 1.6f;

    [Header("Земля (только слои)")]
    [SerializeField] Transform groundCheck;
    [SerializeField] Vector2 groundCheckSize = new Vector2(0.5f, 0.15f);
    [SerializeField] Vector3 Offset = new Vector2(0f, 0f);
    [SerializeField] LayerMask groundLayers;

    [Header("Склоны и лестницы")]
    [Tooltip("Максимальный угол склона, по которому можно ходить. Круче — игрок скользит")]
    [SerializeField] float maxSlopeAngle = 50f;
    [Tooltip("Как далеко вниз от ног искать поверхность для определения наклона")]
    [SerializeField] float slopeCheckDistance = 0.6f;

    [Header("Урон и смерть")]
    [SerializeField] Vector2 knockback = new Vector2(6f, 6f);
    [SerializeField] float stunTime = 0.25f;
    [Tooltip("Задержка между смертью и телепортом (здесь играет анимация смерти)")]
    [SerializeField] float respawnDelay = 1.5f;
    [Tooltip("Точка возрождения. Пусто = место, где игрок стоял при старте сцены")]
    [SerializeField] Transform respawnPoint;
    [Tooltip("Вызывается при смерти — например Animator.SetTrigger со строкой Die")]
    [SerializeField] UnityEvent onDied;
    [Tooltip("Вызывается после телепорта — например Animator.SetTrigger со строкой Idle")]
    [SerializeField] UnityEvent onRespawned;

    [Header("Дыхание (если игрок бездействует)")]
    [Tooltip("Сколько секунд бездействия до анимации дыхания")]
    [SerializeField] float idleBreathDelay = 4f;
    [Tooltip("Animator игрока. Пусто = ищется на этом объекте или в дочерних")]
    [SerializeField] Animator animator;
    [Tooltip("Имя Bool-параметра в Animator: true — пора дышать, false — любое действие игрока")]
    [SerializeField] string breathingParameter = "Breathing";
    [Tooltip("Дополнительно: звук, эффект и т.п. при начале дыхания")]
    [SerializeField] UnityEvent onBreathStart;
    [SerializeField] UnityEvent onBreathEnd;

    [Header("Разворот")]
    [Tooltip("Куда смотрит спрайт по умолчанию: вправо — включено, влево — выключено")]
    [SerializeField] bool facingRight = true;

    [Header("Ввод (Input System, можно менять в инспекторе)")]
    [SerializeField] InputAction moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Axis");
    [SerializeField] InputAction jumpAction = new InputAction("Jump", InputActionType.Button);

    const float JumpLockTime = 0.15f; // после прыжка/удара не "прилипаем" к земле

    Rigidbody2D body;
    Health health;

    float baseGravity;
    float moveInput;
    float coyoteCounter;
    float jumpBufferCounter;
    float stunTimer;
    float groundLockTimer;
    Vector2 groundNormal = Vector2.up;
    Vector2 respawnPosition;
    readonly Collider2D[] overlapBuffer = new Collider2D[4];
    readonly RaycastHit2D[] rayBuffer = new RaycastHit2D[4];
    float idleTimer;
    bool isBreathing;
    bool hasBreathParam;
    bool isGrounded;
    bool alive = true;

    public bool IsGrounded => isGrounded;
    public bool IsBreathing => isBreathing;
    public bool FacingRight => facingRight;

    void Reset() => AddDefaultBindings();

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();

        baseGravity = body.gravityScale;
        respawnPosition = respawnPoint != null ? (Vector2)respawnPoint.position : (Vector2)transform.position;
        body.freezeRotation = true;
        // Иначе стоя на шипах тело "засыпает" и урон перестаёт приходить
        body.sleepMode = RigidbodySleepMode2D.NeverSleep;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        hasBreathParam = HasBoolParameter(breathingParameter);

        if (moveAction.bindings.Count == 0 && jumpAction.bindings.Count == 0)
            AddDefaultBindings();
    }

    void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Died += OnDied;

        if (alive)
        {
            moveAction.Enable();
            jumpAction.Enable();
        }
    }

    void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;

        moveAction.Disable();
        jumpAction.Disable();
    }

    void AddDefaultBindings()
    {
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/rightArrow");
        moveAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Gamepad>/dpad/left")
            .With("Positive", "<Gamepad>/dpad/right");
        moveAction.AddBinding("<Gamepad>/leftStick/x");

        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Keyboard>/w");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
    }

    void Update()
    {
        if (!alive) return;

        moveInput = moveAction.ReadValue<float>();
        groundLockTimer -= Time.deltaTime;

        // Земля + "время койота" (можно прыгнуть сразу после схода с края)
        isGrounded = Physics2D.OverlapBox(groundCheck.position + Offset, groundCheckSize, 0f, GroundFilter(), overlapBuffer) > 0;
        coyoteCounter = isGrounded ? coyoteTime : coyoteCounter - Time.deltaTime;

        // Буфер прыжка (нажали чуть раньше приземления)
        if (jumpAction.WasPressedThisFrame()) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0f && coyoteCounter > 0f && stunTimer <= 0f)
            Jump();

        // Отпустили кнопку в полёте вверх — прыжок ниже.
        // Только в воздухе: на подъёме по склону скорость вверх — это нормально.
        bool inAir = !isGrounded || groundLockTimer > 0f;
        if (inAir && jumpAction.WasReleasedThisFrame() && body.linearVelocity.y > 0f)
            body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * jumpCutMultiplier);

        HandleFlip();
        UpdateIdleBreathing();
    }

    void FixedUpdate()
    {
        if (!alive) return;

        if (stunTimer > 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
            ApplyAirGravity();
            return;
        }

        float target = moveInput * moveSpeed;
        float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;

        bool onWalkableGround = isGrounded && groundLockTimer <= 0f && UpdateGroundNormal();

        if (onWalkableGround)
        {
            // Идём ВДОЛЬ поверхности: вверх по склону, вниз по склону или по ровному.
            // Скорость задаём напрямую и отключаем гравитацию — игрок не скатывается
            // в покое и не отрывается от земли при спуске.
            Vector2 tangent = new Vector2(groundNormal.y, -groundNormal.x);
            float current = Vector2.Dot(body.linearVelocity, tangent);
            current = Mathf.MoveTowards(current, target, rate * Time.fixedDeltaTime);

            body.linearVelocity = tangent * current;
            body.gravityScale = 0f;
        }
        else
        {
            Vector2 v = body.linearVelocity;
            v.x = Mathf.MoveTowards(v.x, target, rate * Time.fixedDeltaTime);
            body.linearVelocity = v;

            ApplyAirGravity();
        }
    }

    // Падаем быстрее, чем взлетаем — прыжок ощущается плотнее
    void ApplyAirGravity()
    {
        body.gravityScale = body.linearVelocity.y < 0f ? baseGravity * fallGravityMultiplier : baseGravity;
    }

    // Определяет наклон поверхности под ногами. true — по ней можно ходить.
    bool UpdateGroundNormal()
    {
        Vector2 origin = (Vector2)groundCheck.position + Vector2.up * 0.1f;
        int count = Physics2D.Raycast(origin, Vector2.down, GroundFilter(), rayBuffer, slopeCheckDistance + 0.1f);

        groundNormal = count > 0 ? rayBuffer[0].normal : Vector2.up;
        return Vector2.Angle(groundNormal, Vector2.up) <= maxSlopeAngle;
    }

    // Землёй считаются только обычные коллайдеры на слоях groundLayers.
    // Триггеры (зоны, двери, пикапы, лестничные области) игнорируются.
    ContactFilter2D GroundFilter()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayers);
        filter.useTriggers = false;
        return filter;
    }

    // Любое действие обнуляет таймер; после idleBreathDelay секунд покоя включается дыхание.
    void UpdateIdleBreathing()
    {
        bool active = Mathf.Abs(moveInput) > 0.01f
                      || !isGrounded
                      || stunTimer > 0f
                      || jumpAction.IsPressed()
                      || body.linearVelocity.sqrMagnitude > 0.01f;

        if (active)
        {
            ResetIdleTimer();
            return;
        }

        idleTimer += Time.deltaTime;
        if (!isBreathing && idleTimer >= idleBreathDelay)
            SetBreathing(true);
    }

    // Можно вызывать из других скриптов (взаимодействие, инвентарь и т.п.),
    // если их действия тоже должны прерывать дыхание.
    public void ResetIdleTimer()
    {
        idleTimer = 0f;
        if (isBreathing) SetBreathing(false);
    }

    void SetBreathing(bool value)
    {
        isBreathing = value;

        if (hasBreathParam) animator.SetBool(breathingParameter, value);

        if (value) onBreathStart?.Invoke();
        else onBreathEnd?.Invoke();
    }

    bool HasBoolParameter(string parameterName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        if (string.IsNullOrEmpty(parameterName)) return false;

        foreach (var p in animator.parameters)
            if (p.type == AnimatorControllerParameterType.Bool && p.name == parameterName)
                return true;

        return false;
    }

    void Jump()
    {
        body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        groundLockTimer = JumpLockTime;
        // сюда: звук / анимация прыжка
    }

    void HandleFlip()
    {
        if (stunTimer > 0f) return;

        if (moveInput > 0.01f && !facingRight) Flip();
        else if (moveInput < -0.01f && facingRight) Flip();
    }

    void Flip()
    {
        facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x = -scale.x;
        transform.localScale = scale;
    }

    void OnDamaged(int amount, Vector2 sourcePosition)
    {
        // Отбрасываем от источника урона
        float dir = transform.position.x >= sourcePosition.x ? 1f : -1f;
        body.linearVelocity = new Vector2(dir * knockback.x, knockback.y);
        stunTimer = stunTime;
        groundLockTimer = stunTime + JumpLockTime;
        // сюда: анимация получения урона
    }

    void OnDied()
    {
        alive = false;
        moveAction.Disable();
        jumpAction.Disable();
        body.linearVelocity = Vector2.zero;
        ResetIdleTimer();
        onDied?.Invoke();

        Invoke(nameof(Respawn), respawnDelay);
    }

    // Для чекпоинтов: следующая смерть вернёт игрока в эту точку
    public void SetRespawnPoint(Vector2 position) => respawnPosition = position;

    void Respawn()
    {
        transform.position = respawnPosition;
        body.position = respawnPosition;
        body.linearVelocity = Vector2.zero;
        body.gravityScale = baseGravity;

        moveInput = 0f;
        stunTimer = 0f;
        groundLockTimer = 0f;
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;

        health.Revive();
        alive = true;
        moveAction.Enable();
        jumpAction.Enable();

        onRespawned?.Invoke();
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheck.position + Offset, groundCheckSize);

        Gizmos.color = Color.yellow;
        Vector3 origin = groundCheck.position + Vector3.up * 0.1f;
        Gizmos.DrawLine(origin, origin + Vector3.down * (slopeCheckDistance + 0.1f));
    }
}