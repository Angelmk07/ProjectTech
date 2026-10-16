using UnityEngine;
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
    [SerializeField] Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [SerializeField] LayerMask groundLayers;

    [Header("Урон и смерть")]
    [SerializeField] Vector2 knockback = new Vector2(6f, 6f);
    [SerializeField] float stunTime = 0.25f;
    [SerializeField] float respawnDelay = 1.5f;

    [Header("Разворот")]
    [Tooltip("Куда смотрит спрайт по умолчанию: вправо — включено, влево — выключено")]
    [SerializeField] bool facingRight = true;

    [Header("Ввод (Input System, можно менять в инспекторе)")]
    [SerializeField] InputAction moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Axis");
    [SerializeField] InputAction jumpAction = new InputAction("Jump", InputActionType.Button);

    Rigidbody2D body;
    Health health;

    float baseGravity;
    float moveInput;
    float coyoteCounter;
    float jumpBufferCounter;
    float stunTimer;
    bool isGrounded;
    bool alive = true;

    public bool IsGrounded => isGrounded;
    public bool FacingRight => facingRight;

    void Reset() => AddDefaultBindings();

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();

        baseGravity = body.gravityScale;
        body.freezeRotation = true;
        // Иначе стоя на шипах тело "засыпает" и урон перестаёт приходить
        body.sleepMode = RigidbodySleepMode2D.NeverSleep;

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

        // Земля + "время койота" (можно прыгнуть сразу после схода с края)
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayers);
        coyoteCounter = isGrounded ? coyoteTime : coyoteCounter - Time.deltaTime;

        // Буфер прыжка (нажали чуть раньше приземления)
        if (jumpAction.WasPressedThisFrame()) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0f && coyoteCounter > 0f && stunTimer <= 0f)
            Jump();

        // Отпустили кнопку в полёте вверх — прыжок ниже
        if (jumpAction.WasReleasedThisFrame() && body.linearVelocity.y > 0f)
            body.linearVelocity = new Vector2(body.linearVelocity.x, body.linearVelocity.y * jumpCutMultiplier);

        HandleFlip();
    }

    void FixedUpdate()
    {
        if (!alive) return;

        if (stunTimer > 0f)
        {
            stunTimer -= Time.fixedDeltaTime;
        }
        else
        {
            float target = moveInput * moveSpeed;
            float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;

            Vector2 v = body.linearVelocity;
            v.x = Mathf.MoveTowards(v.x, target, rate * Time.fixedDeltaTime);
            body.linearVelocity = v;
        }

        // Падаем быстрее, чем взлетаем — прыжок ощущается плотнее
        body.gravityScale = body.linearVelocity.y < 0f ? baseGravity * fallGravityMultiplier : baseGravity;
    }

    void Jump()
    {
        body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
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
        // сюда: анимация получения урона
    }

    void OnDied()
    {
        alive = false;
        moveAction.Disable();
        jumpAction.Disable();
        body.linearVelocity = Vector2.zero;
        // сюда: анимация смерти

        Invoke(nameof(Respawn), respawnDelay);
    }

    void Respawn() => GameManager.Instance.RestartLevel();

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}
