using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;
    public float runSpeedMultiplier = 1.8f;

    [Header("중력 설정")]
    public float gravity = -9.81f;
    private Vector3 velocity;

    [Header("점프 설정")]
    public float jumpHeight = 1.5f; // 내가 뛰어오르고 싶은 높이 (미터 단위)

    private CharacterController controller;
    private PlayerStamina stamina;
    private PlayerHP health;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        stamina = GetComponent<PlayerStamina>();
        health = GetComponent<PlayerHP>();
    }

    void Update()
    {
        if (health != null && health.isDead) return;

        // 1. 접지 판정
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // 땅에 착지 시 y속도 리셋
        }

        // 2. WASD 키 입력
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.D)) horizontal += 1f;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.W)) vertical += 1f;
        if (Input.GetKey(KeyCode.S)) vertical -= 1f;

        bool isMoving = (horizontal != 0 || vertical != 0);

        // 3. 달리기 & 스테미나
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && isMoving && stamina != null && stamina.CanRun();

        if (stamina != null)
        {
            if (isRunning) stamina.DrainStamina();
            else stamina.RegenerateStamina();
        }

        float currentSpeed = moveSpeed;
        if (isRunning) currentSpeed *= runSpeedMultiplier;

        // 4. 수평 이동 방향 계산 (플레이어가 바라보는 방향 기준)
        Vector3 moveDirection = (transform.right * horizontal + transform.forward * vertical).normalized;

        // ----------------------------------------------------
        // 5. 점프 처리 (스페이스바 + 땅에 닿아있을 때만)
        // ----------------------------------------------------
        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded)
        {
            // 원하는 높이(jumpHeight)만큼 점프하기 위한 y축 속도 계산
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 6. 중력 누적
        velocity.y += gravity * Time.deltaTime;

        // 7. 수평 이동 + 수직(점프/중력) 이동합성
        Vector3 finalMove = (moveDirection * currentSpeed) + new Vector3(0f, velocity.y, 0f);

        // 8. 최종 이동 실행
        controller.Move(finalMove * Time.deltaTime);
    }
}