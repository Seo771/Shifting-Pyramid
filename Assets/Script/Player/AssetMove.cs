using UnityEngine;

public class AssetMove : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;
    public float runSpeedMultiplier = 1.8f;

    [Header("중력 및 점프 설정")]
    public float gravity = -20f;
    public float jumpHeight = 1.5f;
    private Vector3 velocity; // ★ CS0103 에러 방지용 변수 선언

    [Header("애니메이션 설정")]
    public Animator animator;

    private CharacterController controller;
    private PlayerStamina stamina;
    private PlayerHP health;

    // Y좌표 변화량 체크용 변수
    private float lastYPosition;
    private bool isYStationary; 

    void Start()
    {
        controller = GetComponent<CharacterController>();
        stamina = GetComponent<PlayerStamina>();
        health = GetComponent<PlayerHP>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // 초기 Y위치 기록
        lastYPosition = transform.position.y;
    }

    void Update()
    {
        if (health != null && health.isDead)
        {
            if (animator != null)
                animator.SetFloat("Speed", 0f);
            return;
        }

        // 1. 현재 Y좌표와 이전 프레임 Y좌표 비교 (Y 변화량 계산)
        float currentY = transform.position.y;
        float yDelta = Mathf.Abs(currentY - lastYPosition);

        // 한 프레임 동안 Y축 변화가 0.001f 미만이면 "Y좌표 변화 없음" 상태로 판정
        isYStationary = yDelta < 0.001f;

        // 점프 및 지면 판정 보완
        bool canJump = controller.isGrounded || isYStationary;

        if (canJump && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 2. 이동 입력 처리 (WASD)
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.D)) horizontal += 1f;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.W)) vertical += 1f;
        if (Input.GetKey(KeyCode.S)) vertical -= 1f;

        bool isMoving = horizontal != 0 || vertical != 0;

        // 달리기 및 스태미나
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && isMoving && stamina != null && stamina.CanRun();
        if (stamina != null)
        {
            if (isRunning) stamina.DrainStamina();
            else stamina.RegenerateStamina();
        }

        float currentSpeed = moveSpeed;
        if (isRunning) currentSpeed *= runSpeedMultiplier;

        Vector3 moveDirection = (transform.right * horizontal + transform.forward * vertical).normalized;

        // 3. 점프 실행 (Y좌표 변화가 없거나 isGrounded일 때)
        if (Input.GetButtonDown("Jump") && canJump)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (animator != null)
            {
                animator.ResetTrigger("Jump");
                animator.SetTrigger("Jump");
            }
        }

        // 4. 중력 적용 및 최종 이동
        velocity.y += gravity * Time.deltaTime;
        Vector3 finalMove = (moveDirection * currentSpeed) + new Vector3(0f, velocity.y, 0f);
        controller.Move(finalMove * Time.deltaTime);

        // 5. 다음 프레임 비교를 위한 Y위치 갱신
        lastYPosition = transform.position.y;

        // 6. 애니메이션 Speed 조절
        if (animator != null)
        {
            float animationSpeed = 0f;

            // 이동 입력이 있고 moving 상태일 때만 speed 설정
            if (isMoving)
            {
                animationSpeed = isRunning ? 2f : 1f;
            }
            else
            {
                // 이동 입력이 없거나 제자리에 서 있을 때는 즉시 완벽한 0f로 고정
                animationSpeed = 0f;
            }

            animator.SetFloat("Speed", animationSpeed);
            animator.SetBool("isGrounded", canJump);
        }
    }
}