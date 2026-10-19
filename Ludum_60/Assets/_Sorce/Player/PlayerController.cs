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
    [Tooltip("Слои, на которых можно стоять. Определяется по реальному касанию коллайдера игрока, триггеры игнорируются")]
    [SerializeField] LayerMask groundLayers;

    [Header("Склоны и лестницы")]
    [Tooltip("Максимальный угол поверхности, по которой можно ходить. Круче — игрок скользит")]
    [SerializeField] float maxSlopeAngle = 50f;
    [Tooltip("Рисует контакты с землёй в Scene (зелёный — земля, красный — слишком круто/стена, жёлтый — выше центра, голубой — слишком глубоко) и пишет в консоль угол при смене состояния")]
    [SerializeField] bool debugGround;

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

    [Header("Анимации")]
    [SerializeField] Animator animator;
    [SerializeField] string speedParameter = "Speed";
    [SerializeField] string altParameter = "Alt";
    [SerializeField] string idleParameter = "Idle";
    [SerializeField] string inAirParameter = "InAir";
    [SerializeField] string jumpParameter = "Jump";
    [SerializeField] InputAction altAction = new InputAction("Alt", InputActionType.Button);

    [Header("Периодический выдох при бездействии")]
    [Tooltip("Время неподвижности до первого выдоха")]
    [SerializeField, Min(0f)] float idleBreathDelay = 4f;
    [Tooltip("Интервал между последующими выдохами")]
    [SerializeField, Min(0.1f)] float breathRepeatInterval = 5f;
    [SerializeField] UnityEvent onBreathStart;
    [SerializeField] UnityEvent onBreathEnd;

    [Header("Разворот")]
    [Tooltip("Куда смотрит спрайт по умолчанию: вправо — включено, влево — выключено")]
    [SerializeField] bool facingRight = true;

    [Header("Ввод (Input System, можно менять в инспекторе)")]
    [SerializeField] InputAction moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Axis");
    [SerializeField] InputAction jumpAction = new InputAction("Jump", InputActionType.Button);

    const float JumpLockTime = 0.15f;    // после прыжка/удара не "прилипаем" к земле
    const float GroundGraceTime = 0.05f; // пережидаем потерю контакта на стыках
    const float MaxPenetration = 0.08f;  // глубже — это проход сквозь односторонку, а не стояние
    const float SnapDistance = 0.15f;    // на таком расстоянии от земли подхватываем игрока на спуске
    const float SnapPullSpeed = 3f;      // скорость "притягивания" к поверхности при подхвате

    Rigidbody2D body;
    Health health;

    float baseGravity;
    float moveInput;
    float coyoteCounter;
    float jumpBufferCounter;
    float stunTimer;
    float groundLockTimer;
    float groundedGrace;
    Vector2 groundNormal = Vector2.up;
    Vector2 respawnPosition;
    readonly ContactPoint2D[] contactBuffer = new ContactPoint2D[16];
    readonly RaycastHit2D[] castBuffer = new RaycastHit2D[8];
    float idleTimer;
    bool isBreathing;
    bool isGrounded;
    bool stuckToGround;   // есть контакт/подхват именно в этом физическом шаге
    bool snapped;         // земля найдена не контактом, а подхватом сверху
    bool alive = true;

    // отладка
    int debugContactCount;
    float debugClosestAngle;

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
        if (altAction.bindings.Count == 0)
            altAction.AddBinding("<Keyboard>/leftAlt");

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
            altAction.Enable();
        }
    }

    void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;

        moveAction.Disable();
        jumpAction.Disable();
        altAction.Disable();
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

        // "Время койота": можно прыгнуть сразу после схода с края.
        // isGrounded обновляется в FixedUpdate по физическим контактам.
        bool canCoyote = isGrounded && groundLockTimer <= 0f;
        coyoteCounter = canCoyote ? coyoteTime : coyoteCounter - Time.deltaTime;

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
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (!alive) return;

        UpdateGrounded();

        if (stunTimer > 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
            ApplyAirGravity();
            return;
        }

        float target = moveInput * moveSpeed;
        float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;

        if (stuckToGround && groundLockTimer <= 0f)
        {
            // Идём ВДОЛЬ поверхности: вверх по склону, вниз по склону или по ровному.
            // Скорость задаём напрямую и отключаем гравитацию — игрок не скатывается
            // в покое и не отрывается от земли при спуске.
            Vector2 tangent = new Vector2(groundNormal.y, -groundNormal.x);
            float current = Vector2.Dot(body.linearVelocity, tangent);
            current = Mathf.MoveTowards(current, target, rate * Time.fixedDeltaTime);

            Vector2 velocity = tangent * current;
            if (snapped) velocity -= groundNormal * SnapPullSpeed; // подтягиваем к склону

            body.linearVelocity = velocity;
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

    void UpdateGrounded()
    {
        bool wasGrounded = isGrounded;
        bool touching = false;
        snapped = false;

        if (groundLockTimer <= 0f)
        {
            touching = ReadGroundContacts();

            // Контакт на миг пропал (стык, спуск по склону), но только что мы стояли на земле —
            // ищем поверхность чуть ниже. Не срабатывает после прыжка и удара.
            if (!touching && isGrounded)
            {
                touching = SnapToGround();
                snapped = touching;
            }
        }

        stuckToGround = touching;
        groundedGrace = touching ? GroundGraceTime : groundedGrace - Time.fixedDeltaTime;
        isGrounded = groundedGrace > 0f;

        if (debugGround && wasGrounded != isGrounded)
        {
            string angle = debugClosestAngle >= 0f ? $"{debugClosestAngle:0}°" : "нет контактов";
            Debug.Log($"[Player] isGrounded={isGrounded}, контактов={debugContactCount}, " +
                      $"ближайший угол поверхности={angle}, maxSlopeAngle={maxSlopeAngle}°", this);
        }
    }

    // Земля = реальный физический контакт коллайдера игрока с поверхностью,
    // нормаль которой смотрит вверх (не круче maxSlopeAngle).
    // Поэтому не считаются: стены и боковины ступенек, потолок, триггеры,
    // а также односторонние платформы, пока игрок проходит сквозь них снизу.
    bool ReadGroundContacts()
    {
        int count = body.GetContacts(GroundFilter(), contactBuffer);

        float minUp = Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);
        float centerY = body.worldCenterOfMass.y;
        float bestUp = 0f;
        Vector2 bestNormal = Vector2.up;
        bool found = false;

        debugContactCount = count;
        debugClosestAngle = -1f;

        for (int i = 0; i < count; i++)
        {
            ContactPoint2D c = contactBuffer[i];

            Vector2 n = c.normal;
            if (n.y < 0f) n = -n;                          // нормаль всегда вверх

            if (c.point.y > centerY)                       // потолок или верхняя часть тела
            {
                if (debugGround) Debug.DrawRay(c.point, n * 0.5f, Color.yellow);
                continue;
            }

            if (c.separation < -MaxPenetration)            // глубокое перекрытие — проход сквозь платформу
            {
                if (debugGround) Debug.DrawRay(c.point, n * 0.5f, Color.cyan);
                continue;
            }

            float angle = Vector2.Angle(n, Vector2.up);
            if (debugClosestAngle < 0f || angle < debugClosestAngle) debugClosestAngle = angle;

            if (n.y < minUp)                               // стена или слишком крутой склон
            {
                if (debugGround) Debug.DrawRay(c.point, n * 0.5f, Color.red);
                continue;
            }

            if (debugGround) Debug.DrawRay(c.point, n * 0.5f, Color.green);

            if (n.y > bestUp)
            {
                bestUp = n.y;
                bestNormal = n;
                found = true;
            }
        }

        if (found) groundNormal = bestNormal;
        return found;
    }

    // Короткий подхват земли под игроком вдоль предыдущей нормали.
    bool SnapToGround()
    {
        int count = body.Cast(-groundNormal, GroundFilter(), castBuffer, SnapDistance);
        float minUp = Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);

        for (int i = 0; i < count; i++)
        {
            Vector2 n = castBuffer[i].normal;
            if (n.y < minUp) continue;

            groundNormal = n;
            return true;
        }

        return false;
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

        if (!isBreathing)
        {
            if (idleTimer >= idleBreathDelay)
            {
                isBreathing = true;
                idleTimer = 0f;
                PlayBreath();
                onBreathStart?.Invoke();
            }
        }
        else if (idleTimer >= breathRepeatInterval)
        {
            idleTimer = 0f;
            PlayBreath();
        }
    }

    public void ResetIdleTimer()
    {
        idleTimer = 0f;

        if (isBreathing)
        {
            isBreathing = false;
            onBreathEnd?.Invoke();
        }
    }

    void PlayBreath()
    {
        if (animator != null)
            animator.SetTrigger(idleParameter);
    }

    void UpdateAnimations()
    {
        if (animator == null)
            return;

        animator.SetFloat(speedParameter, Mathf.Abs(body.linearVelocity.x));
        animator.SetBool(altParameter, altAction.IsPressed());
        animator.SetBool(inAirParameter, !isGrounded);
    }

    void Jump()
    {
        body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        groundLockTimer = JumpLockTime;
        groundedGrace = 0f;
        isGrounded = false;
        stuckToGround = false;
        if (animator != null)
        {
            animator.ResetTrigger(idleParameter);
            animator.SetTrigger(jumpParameter);
        }
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
        groundedGrace = 0f;
        isGrounded = false;
        stuckToGround = false;
        // сюда: анимация получения урона
    }

    void OnDied()
    {
        alive = false;
        moveAction.Disable();
        jumpAction.Disable();
        altAction.Disable();
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
        groundedGrace = 0f;
        isGrounded = false;
        stuckToGround = false;
        snapped = false;
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;

        health.Revive();
        alive = true;
        moveAction.Enable();
        jumpAction.Enable();
        altAction.Enable();

        onRespawned?.Invoke();
    }
}