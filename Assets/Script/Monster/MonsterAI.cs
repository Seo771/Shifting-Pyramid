using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using ShiftingPyramid.Maze.View;

public class MonsterAI : MonoBehaviour, ISensoryReceiver
{
    // AI 상태 정의 (Attack 추가)
    public enum AIState { Wander, Chase, Investigate, Attack }

    [Header("상태 확인 (확인용)")]
    [SerializeField] private AIState currentState = AIState.Wander;

    [Header("배회(Wander) 설정")]
    [SerializeField] private float wanderInterval = 4f;        // 다음 목적지 갱신 타이머
    [SerializeField] private float minWanderDistance = 10f;     // 최소 이동 거리 및 주변 수색 반경
    [SerializeField] private Vector2 mazeSize = new Vector2(30f, 30f); // 미로 전체 크기 (가로, 세로)

    [Header("추적(Chase) 설정")]
    [SerializeField] private Transform targetPlayer;
    [SerializeField] private float detectRange = 8f;       // 감지 거리
    [SerializeField] private float loseTargetRange = 12f;   // 포기 거리

    [Header("공격(Attack) 설정")]
    [SerializeField] private float attackRange = 2f;        // 공격 사거리
    [SerializeField] private float attackCooldown = 2f;     // 공격 딜레이 (쿨타임)
    [SerializeField] private float attackDamage = 20f;      // 공격 데미지
    [SerializeField] private float attackHitDelay = 0.5f;   // 공격 모션 중 실제 타격이 들어가는 시간

    [Header("미로 내비게이션")]
    [SerializeField] private MazeTestBootstrapper mazeBootstrapper;
    [SerializeField, Min(0.1f)] private float navigationSnapDistance = 3f;

    private NavMeshAgent agent;
    private Animator animator;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private float wanderTimer;
    private float lastAttackTime;
    private bool isAttacking = false;
    private bool isFrozen;
    private Coroutine attackCoroutine;
    private Coroutine freezeCoroutine;
    private Vector3 lastStimulusPosition;
    private bool investigationDestinationPending;
    private PlayerHP playerHP; // PlayerHP 연동
    private bool awaitingNavigation;

    private bool IsMazeAvailable => mazeBootstrapper == null || mazeBootstrapper.IsMazeReady;
    private bool HasBoundAgent => agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
    public bool IsNavigationReady => IsMazeAvailable && HasBoundAgent;
    public bool IsFrozen => isFrozen;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        // Animator가 MON의 자식 모델에 있어도 찾을 수 있도록 자식까지 검색한다.
        animator = GetComponentInChildren<Animator>(true);
        // 생성형 미로용 프리팹은 Agent를 꺼 둔 채 로드한다.
        awaitingNavigation = agent != null && !agent.enabled;
        if (mazeBootstrapper == null) mazeBootstrapper = FindFirstObjectByType<MazeTestBootstrapper>();
    }

    private void Start()
    {
        wanderTimer = wanderInterval;
        FindPlayer();
    }

    // NavMesh Agent의 실제 이동 속도를 Blend Tree의 Speed 파라미터에 전달한다.
    private void UpdateAnimatorMovement()
    {
        if (animator == null) return;

        float moveSpeed = 0f;
        if (agent != null && agent.enabled && agent.isOnNavMesh && !agent.isStopped)
        {
            moveSpeed = agent.velocity.magnitude;
        }

        // 미세한 물리 흔들림으로 Idle/Walk가 반복 전환되는 것을 방지한다.
        if (moveSpeed < 0.05f) moveSpeed = 0f;

        animator.SetFloat(SpeedHash, moveSpeed, 0.1f, Time.deltaTime);
    }

    private void FindPlayer()
    {
        if (targetPlayer == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                targetPlayer = playerObj.transform;
                playerHP = playerObj.GetComponent<PlayerHP>();
            }
        }
    }

    private void Update()
    {
        UpdateAnimatorMovement();

        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            StopActions();
            return;
        }
        if (!IsMazeAvailable)
        {
            if (HasBoundAgent) agent.isStopped = true;
            return;
        }
        if (isFrozen)
        {
            if (HasBoundAgent)
            {
                agent.isStopped = true;
            }
            return;
        }

        // 미로 생성/변경 중에는 NavMesh가 잠시 없을 수 있다.
        if (!TryPrepareAgent()) return;

        if (targetPlayer == null)
        {
            FindPlayer();
            return;
        }

        // 플레이어가 이미 사망했다면 추적/공격 중단 후 배회 모드로 전환
        if (playerHP != null && playerHP.isDead)
        {
            if (currentState == AIState.Chase || currentState == AIState.Attack)
            {
                currentState = AIState.Wander;
                if (agent.isOnNavMesh) agent.isStopped = false;
            }
            return;
        }

        // 공격 모션 수행 중일 때는 이동 및 상태 전환을 잠시 멈춤
        if (isAttacking) return;

        // 플레이어 거리 감지 및 상태 전환 체크
        CheckPlayerDetection();

        // 상태별 행동
        switch (currentState)
        {
            case AIState.Wander:
                HandleWander();
                break;

            case AIState.Chase:
                HandleChase();
                break;

            case AIState.Investigate:
                HandleInvestigate();
                break;

            case AIState.Attack:
                HandleAttack();
                break;
        }
    }

    #region ISensoryReceiver 구현 (외부 자극 수신)
    public void OnReceiveStimulus(StimulusData stimulus)
    {
        if (isAttacking) return;

        switch (stimulus.type)
        {
            case StimulusType.Sight:
                if (stimulus.source != null)
                {
                    targetPlayer = stimulus.source.transform;
                    playerHP = targetPlayer.GetComponent<PlayerHP>();
                    currentState = AIState.Chase;
                }
                break;

            case StimulusType.Sound:
            case StimulusType.Interaction:
                if (currentState != AIState.Chase && currentState != AIState.Attack)
                {
                    lastStimulusPosition = stimulus.position;
                    investigationDestinationPending = true;
                    currentState = AIState.Investigate;
                }
                break;
        }
    }
    #endregion

    private bool TryPrepareAgent()
    {
        if (!IsMazeAvailable || agent == null || !gameObject.activeInHierarchy) return false;
        if (!agent.enabled) return awaitingNavigation && TryAttachAgent();
        if (agent.isOnNavMesh) return true;

        if (!NavMesh.SamplePosition(transform.position, out var hit, navigationSnapDistance, NavigationFilter())) return false;
        return agent.Warp(hit.position) && agent.isOnNavMesh;
    }

    // 준비 단계에서 멈추고 기존 경로와 타격 예약을 취소한다. 수동 비활성 Agent는 유지한다.
    public void SuspendMazeNavigation(MazeTestBootstrapper source)
    {
        if (!BindMaze(source) || agent == null) return;
        awaitingNavigation |= agent.enabled;
        agent.enabled = false;
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        attackCoroutine = null;
        isAttacking = false;
        if (currentState == AIState.Attack) currentState = AIState.Chase;
        if (currentState == AIState.Investigate) investigationDestinationPending = true;
    }

    public void StopActions()
    {
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        attackCoroutine = null;
        isAttacking = false;
        if (!HasBoundAgent) return;
        if (agent.hasPath) agent.ResetPath();
        agent.isStopped = true;
    }

    // NavMesh 생성이 끝난 뒤에만 호출한다. 빙결 상태와 남은 지속 시간은 보존한다.
    public void RefreshMazeNavigation(MazeTestBootstrapper source)
    {
        if (!BindMaze(source) || !awaitingNavigation) return;
        if (!TryAttachAgent())
            Debug.LogWarning($"[MonsterAI] '{name}' is waiting for a navigable spawn position.", this);
    }

    private bool BindMaze(MazeTestBootstrapper source)
    {
        if (source == null) return false;
        if (mazeBootstrapper == null) mazeBootstrapper = source;
        return mazeBootstrapper == source;
    }

    private NavMeshQueryFilter NavigationFilter()
    {
        return new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
    }

    private bool TryAttachAgent()
    {
        if (agent == null || !isActiveAndEnabled
            || !NavMesh.SamplePosition(transform.position, out var hit, navigationSnapDistance, NavigationFilter()))
            return false;

        // 비활성 상태에서 먼저 옮겨야 NavMesh가 없는 위치에서 활성화되는 경고를 피한다.
        transform.position = hit.position;
        agent.enabled = true;
        if (!agent.isOnNavMesh)
        {
            agent.enabled = false;
            return false;
        }
        awaitingNavigation = false;
        agent.ResetPath();
        agent.isStopped = isFrozen;
        return true;
    }

    #region AI 상태별 로직
    private void HandleWander()
    {
        if (agent.isOnNavMesh) agent.isStopped = false;
        wanderTimer += Time.deltaTime;

        if (wanderTimer >= wanderInterval || (agent.hasPath && agent.remainingDistance <= agent.stoppingDistance))
        {
            SetRandomDestination();
            wanderTimer = 0f;
        }
    }

    private void HandleChase()
    {
        if (targetPlayer == null) return;
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(targetPlayer.position);
        }
    }

    private void HandleInvestigate()
    {
        if (agent.isOnNavMesh) agent.isStopped = false;

        if (investigationDestinationPending)
        {
            agent.SetDestination(lastStimulusPosition);
            investigationDestinationPending = false;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            currentState = AIState.Wander;
            wanderTimer = wanderInterval;
        }
    }

    private void HandleAttack()
    {
        // 공격 중에는 제자리에 정지 후 플레이어 조준
        if (agent.isOnNavMesh) agent.isStopped = true;

        Vector3 lookDir = (targetPlayer.position - transform.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
        }

        // 공격 쿨타임 체크
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            attackCoroutine = StartCoroutine(PerformAttackRoutine());
        }
    }

    public void FreezeForDuration(float duration)
    {
        if (!isActiveAndEnabled || duration <= 0f)
        {
            return;
        }

        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
        }

        freezeCoroutine = StartCoroutine(FreezeRoutine(duration));
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        isFrozen = true;
        isAttacking = false;

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }

        if (HasBoundAgent)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        yield return new WaitForSeconds(duration);

        isFrozen = false;
        if (HasBoundAgent)
        {
            agent.isStopped = false;
        }

        freezeCoroutine = null;
    }

    private IEnumerator PerformAttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        // TODO: 미라 공격 애니메이션 실행 시 주석 해제
        // GetComponent<Animator>()?.SetTrigger("Attack");

        Debug.Log("[MonsterAI] 미라가 공격을 시작합니다!");

        // 실제 타격이 발생하는 프레임까지 대기
        yield return new WaitForSeconds(attackHitDelay);

        // 타격 순간 플레이어가 사거리 내에 있고 살아있는지 검증
        if ((GameManager.Instance == null || GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            && IsNavigationReady && !isFrozen && targetPlayer != null
            && Vector3.Distance(transform.position, targetPlayer.position) <= attackRange + 0.5f)
        {
            if (playerHP != null && !playerHP.isDead)
            {
                playerHP.TakeDamage(attackDamage);
            }
        }

        // 후속 모션 대기
        yield return new WaitForSeconds(0.3f);

        isAttacking = false;
    }

    private void SetRandomDestination()
    {
        bool isUturnOrLocal = Random.value < 0.2f;
        float halfX = mazeSize.x * 0.5f;
        float halfY = mazeSize.y * 0.5f;

        for (int i = 0; i < 10; i++)
        {
            Vector3 targetPos;

            if (isUturnOrLocal)
            {
                Vector3 randomDir = Random.insideUnitSphere * minWanderDistance;
                targetPos = transform.position + randomDir;
            }
            else
            {
                targetPos = new Vector3(
                    Random.Range(-halfX, halfX),
                    0f,
                    Random.Range(-halfY, halfY)
                );
            }

            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPos, out hit, 5f, NavMesh.AllAreas))
            {
                if (!isUturnOrLocal && Vector3.Distance(transform.position, hit.position) < minWanderDistance)
                {
                    continue;
                }

                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    private void CheckPlayerDetection()
    {
        if (targetPlayer == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

        // 1. 공격 사거리 진입 -> Attack
        if (distanceToPlayer <= attackRange)
        {
            currentState = AIState.Attack;
        }
        // 2. 공격 사거리 밖이지만 감지 범위 이내 -> Chase
        else if (distanceToPlayer <= detectRange)
        {
            currentState = AIState.Chase;
        }
        // 3. 포기 거리 벗어남 -> Wander
        else if (distanceToPlayer > loseTargetRange && currentState == AIState.Chase)
        {
            currentState = AIState.Wander;
            wanderTimer = wanderInterval;
        }
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        // 감지 거리 (노랑)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        // 포기 거리 (빨강)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);

        // 공격 사거리 (파랑)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
