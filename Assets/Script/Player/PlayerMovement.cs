using System.Collections.Generic;
using UnityEngine;
using ShiftingPyramid.Game;

public class PlayerMovement : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f;
    public float runSpeedMultiplier = 1.8f;

    [Header("패시브 아이템")]
    public List<ItemData> passiveItems = new List<ItemData>();

    [Header("중력 및 점프 설정")]
    public float gravity = -20f;
    public float jumpHeight = 1.5f;
    private Vector3 velocity;
    private bool isJumping = false;

    [Header("애니메이션 설정")]
    public Animator animator;

    private CharacterController controller;
    private PlayerStamina stamina;
    private PlayerHP health;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        stamina = GetComponent<PlayerStamina>();
        health = GetComponent<PlayerHP>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    // PassiveSpeed 아이템 획득 시 이동 속도 증가
    public void AddPassiveItem(ItemData item)
    {
        if (item == null || item.itemType != ItemData.ItemType.PassiveSpeed)
            return;

        passiveItems.Add(item);
        moveSpeed += item.speedBoostAmount;

        Debug.Log($"[PlayerMovement] 패시브 아이템 '{item.itemName}' 획득: 이동 속도 +{item.speedBoostAmount}", this);
    }

    void Update()
    {
        if (GameplayInput.IsBlocked)
        {
            if (animator != null) animator.SetFloat("Speed", 0f);
            return;
        }

        // 사망 시 이동 및 이동 애니메이션 정지
        if (health != null && health.isDead)
        {
            if (animator != null)
                animator.SetFloat("Speed", 0f);
            return;
        }

        // 1. 지면 감지 및 착지 처리
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

        bool isMoving = horizontal != 0f || vertical != 0f;
        bool isShiftPressed = Input.GetKey(KeyCode.LeftShift);

        // 3. 달리기 및 스태미나 처리
        bool canRunStamina = stamina != null && stamina.CanRun();
        bool isRunning = isShiftPressed && isMoving && canRunStamina;

        if (stamina != null)
        {
            if (isRunning)
            {
                stamina.DrainStamina();

                // 스태미나 소모 직후 탈진 상태인지 재검사
                if (!stamina.CanRun())
                    isRunning = false;
            }
            else if (!isShiftPressed)
            {
                // Shift를 누르고 있는 동안에는 스태미나 회복을 막음
                stamina.RegenerateStamina();
            }
        }

        // 4. 최종 이동 속도 및 방향 계산
        float currentSpeed = moveSpeed;
        if (isRunning)
            currentSpeed *= runSpeedMultiplier;

        Vector3 moveDirection =
            (transform.right * horizontal + transform.forward * vertical).normalized;

        // 5. 점프 처리
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

        // 6. 중력 적용 및 이동 실행
        velocity.y += gravity * Time.deltaTime;
        Vector3 finalMove = moveDirection * currentSpeed + new Vector3(0f, velocity.y, 0f);
        controller.Move(finalMove * Time.deltaTime);

        // 7. 이동 애니메이션 파라미터 전달
        if (animator != null)
        {
            float targetAnimationSpeed = 0f;

            if (isMoving)
                targetAnimationSpeed = isRunning ? 2f : 1f;

            animator.SetFloat("Speed", targetAnimationSpeed);
        }
    }
}
