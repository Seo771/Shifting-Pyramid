using UnityEngine;

public class AssetMove : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;
    public float runSpeedMultiplier = 1.8f;

    [Header("중력 및 점프 설정")]
    public float gravity = -20f;
    public float jumpHeight = 1.5f;
    private Vector3 velocity;

    [Header("애니메이션 설정")]
    public Animator animator;

    private CharacterController controller;
    private PlayerStamina stamina;
    private PlayerHP health;

    private bool isJumping = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        stamina = GetComponent<PlayerStamina>();
        health = GetComponent<PlayerHP>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // 사망 시 정지
        if (health != null && health.isDead)
        {
            if (animator != null)
                animator.SetFloat("Speed", 0f);
            return;
        }

        // 1. 지면 감지 및 중력 초기화
        // controller.isGrounded를 직접 사용하며, 하강 중(velocity.y <= 0)일 때만 중력을 착지용(-2f)으로 고정
        if (controller.isGrounded && velocity.y <= 0f)
        {
            velocity.y = -2f;
            isJumping = false;
        }

        // 2. WASD 이동 입력
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.D)) horizontal += 1f;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.W)) vertical += 1f;
        if (Input.GetKey(KeyCode.S)) vertical -= 1f;

        bool isMoving = horizontal != 0 || vertical != 0;
        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift);

        // 3. Shift 달리기 및 스태미나 상태 처리
        bool canRunStamina = (stamina != null) && stamina.CanRun();
        
        // 이동 중이고, Shift를 눌렀으며, 스태미나가 있을 때만 달리기 시도
        bool isRunning = isShiftPressed && isMoving && canRunStamina;

        if (stamina != null)
        {
            if (isRunning)
            {
                stamina.DrainStamina(); // 스태미나 소모

                // 스태미나 소모 후 탈진했는지 즉시 재검사하여 그 즉시 isRunning 차단
                if (!stamina.CanRun())
                {
                    isRunning = false;
                }
            }
            else
            {
                // Shift 키를 누르고 있는 동안에는 걷더라도 스태미나 회복 차단!
                if (!isShiftPressed)
                {
                    stamina.RegenerateStamina();
                }
            }
        }

        // 최종 속도 결정
        float currentSpeed = moveSpeed;
        if (isRunning)
        {
            currentSpeed *= runSpeedMultiplier;
        }

        Vector3 moveDirection = (transform.right * horizontal + transform.forward * vertical).normalized;

        // 4. 점프 실행 (controller.isGrounded만 확실하게 체크)
        if (Input.GetButtonDown("Jump") && controller.isGrounded && !isJumping)
        {
            isJumping = true;
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (animator != null)
            {
                animator.ResetTrigger("Jump");
                animator.SetTrigger("Jump");
            }
        }

        // 5. 중력 적용 및 최종 이동
        velocity.y += gravity * Time.deltaTime;
        Vector3 finalMove = (moveDirection * currentSpeed) + new Vector3(0f, velocity.y, 0f);
        controller.Move(finalMove * Time.deltaTime);

        // 6. 애니메이션 파라미터 전달
        if (animator != null)
        {
            float targetAnimationSpeed = 0f;

            if (isMoving)
            {
                targetAnimationSpeed = isRunning ? 2f : 1f;
            }
            else
            {
                targetAnimationSpeed = 0f;
            }

            animator.SetFloat("Speed", targetAnimationSpeed);
        }
    }
}