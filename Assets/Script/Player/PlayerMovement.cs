using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    [Header("이동 설정")]
    // [public]으로 선언해서 유니티 화면(Inspector)에서 숫자를 바로 바꿀 수 있습니다!
    public float moveSpeed = 5f;
    public float runSpeedMultiplier = 1.8f; // 달리기 속도 배율 (기본 속도 x 배율)
    private CharacterController controller;
    private PlayerStamina stamina;
    private PlayerHP health;
    void Start()
    {
        controller = GetComponent<CharacterController>();
        stamina = GetComponent<PlayerStamina>();
        health = GetComponent<PlayerHP>();
    }

    // Update is called once per frame
    void Update()
    {
        if (health != null && health.isDead) return;

        // 1. WASD 및 화살표 키 입력 받기 (-1.0 ~ 1.0 범위값)
        float horizontal = Input.GetAxisRaw("Horizontal"); // A, D 키 (왼쪽/오른쪽)
        float vertical = Input.GetAxisRaw("Vertical");     // W, S 키 (앞/뒤)



        // 2. 현재 움직이고 있는지 체크 (키를 누르는 중인지)
        bool isMoving = (horizontal != 0 || vertical != 0);

        // 3. Shift 키를 누르고 + 움직이는 중이면 달리기 + 스테미나 보유여부
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && isMoving && stamina != null && stamina.CanRun();

        // 4. 스테미나 소모 / 회복 분기 처리
        if (stamina != null)
        {
            if (isRunning)
            {
                stamina.DrainStamina(); // 달리는 중이면 소모
            }
            else
            {
                stamina.RegenerateStamina(); // 달리지 않으면 (걷거나 가만히 있으면) 회복!
            }
        }

        // 4. 달리기 여부에 따라 속도 결정
        float currentSpeed = moveSpeed;
        if (isRunning)
        {
            currentSpeed *= runSpeedMultiplier; // 걷기 속도 x 1.8배
        }

        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);
    }
}
