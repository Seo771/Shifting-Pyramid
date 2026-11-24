using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.View;

public class MonsterAI : MonoBehaviour, ISensoryReceiver
{
    // AI 상태 정의 (Attack 추가)
    public enum AIState { Wander, Chase, Investigate, Attack }

    [Header("상태 확인 (확인용)")]
    [SerializeField] private AIState currentState = AIState.Wander;

    [Header("배회(Wander) 설정")]
    [SerializeField, Min(0.1f)] private float wanderInterval = 4f; // 목적지를 찾지 못했을 때 재시도 간격
    [SerializeField, Min(0f)] private float minWanderDistance = 10f; // 다음 목적지까지의 최소 거리
    [Tooltip("생성형 미로가 없는 씬에서 사용할 배회 범위입니다. 생성형 미로에서는 실제 타일 좌표를 사용합니다.")]
    [SerializeField] private Vector2 mazeSize = new Vector2(30f, 30f);

    [Header("추적(Chase) 설정")]
    [SerializeField] private Transform targetPlayer;
    [SerializeField] private float detectRange = 8f;       // 감지 거리
    [SerializeField] private float loseTargetRange = 12f;   // 포기 거리

    [Header("시야 설정")]
    [SerializeField, Range(1f, 360f)] private float viewAngle = 90f;
    [Tooltip("미라 루트 위치에서 눈까지의 높이. 필요하면 Eye Point로 직접 지정합니다.")]
    [SerializeField] private float eyeHeight = 0.6f;
    [SerializeField] private Transform eyePoint;
    [Tooltip("시야를 가리는 오브젝트의 레이어. Trigger는 시야를 가리지 않습니다.")]
    [SerializeField] private LayerMask sightBlockingLayers = Physics.DefaultRaycastLayers;

    [Header("공격(Attack) 설정")]
    [SerializeField] private float attackRange = 2f;        // 공격 사거리
    [SerializeField] private float attackCooldown = 2f;     // 공격 딜레이 (쿨타임)
    [SerializeField] private float attackDamage = 20f;      // 공격 데미지
    [SerializeField] private float attackHitDelay = 0.5f;   // 공격 모션 중 실제 타격이 들어가는 시간

    [Header("미로 내비게이션")]
    [SerializeField] private MazeTestBootstrapper mazeBootstrapper;
    [SerializeField, Min(0.1f)] private float navigationSnapDistance = 3f;

    private NavMeshAgent agent;
    private NavMeshPath wanderPath;
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
    private Collider playerCollider;
    private bool awaitingNavigation;

    private bool IsMazeAvailable => mazeBootstrapper == null || mazeBootstrapper.IsMazeReady;
    private bool HasBoundAgent => agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;
    public bool IsNavigationReady => IsMazeAvailable && HasBoundAgent;
    public bool IsFrozen => isFrozen;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        wanderPath = new NavMeshPath();
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
            }
        }

        if (targetPlayer != null)
        {
            playerHP = targetPlayer.GetComponent<PlayerHP>();
            playerCollider = targetPlayer.GetComponent<Collider>();
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

        // 플레이어 시야 감지 및 상태 전환 체크
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
        if (!isActiveAndEnabled
            || (GameManager.Instance != null && GameManager.Instance.IsGameFinished)) return;
        if (isAttacking) return;

        switch (stimulus.type)
        {
            case StimulusType.Sight:
                if (stimulus.source != null)
                {
                    targetPlayer = stimulus.source.transform;
                    playerHP = targetPlayer.GetComponent<PlayerHP>();
                    playerCollider = targetPlayer.GetComponent<Collider>();
                    CheckPlayerDetection();
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
        if (agent.pathPending) return;

        if (agent.hasPath)
        {
            // 이동 중에는 목적지를 유지하고, 도착하거나 경로가 끊겼을 때 다시 선택한다.
            if (agent.pathStatus == NavMeshPathStatus.PathComplete
                && agent.remainingDistance > agent.stoppingDistance) return;

            agent.ResetPath();
            wanderTimer = wanderInterval;
        }

        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderInterval)
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
            investigationDestinationPending = false;
            // 상자나 단지가 벽 가까이에 있어도 실제로 걸을 수 있는 위치로 수색한다.
            if (!NavMesh.SamplePosition(lastStimulusPosition, out var hit, navigationSnapDistance, NavigationFilter())
                || !agent.SetDestination(hit.position))
            {
                agent.ResetPath();
                currentState = AIState.Wander;
                wanderTimer = wanderInterval;
            }
            return;
        }

        if (!agent.pathPending && (!agent.hasPath || agent.pathStatus == NavMeshPathStatus.PathInvalid
            || agent.remainingDistance <= agent.stoppingDistance))
        {
            agent.ResetPath();
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

        // 공격 애니메이션 3종 중 하나를 무작위로 실행
        if (animator != null)
        {
            int randomAttack = Random.Range(0, 3);

            switch (randomAttack)
            {
                case 0:
                    animator.SetTrigger("AttackLeft");
                    break;

                case 1:
                    animator.SetTrigger("AttackRight");
                    break;

                case 2:
                    animator.SetTrigger("Attack2");
                    break;
            }
        }

        Debug.Log("[MonsterAI] 미라가 공격을 시작합니다!");

        // 실제 타격이 발생하는 프레임까지 대기
        yield return new WaitForSeconds(attackHitDelay);

        // 타격 순간 플레이어가 사거리 내에 있고 살아있는지 검증
        if ((GameManager.Instance == null || GameManager.Instance.CurrentState == GameManager.GameState.Playing)
            && IsNavigationReady && !isFrozen && targetPlayer != null
            && CanSeePlayer(attackRange + 0.5f)
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
        var grid = mazeBootstrapper != null ? mazeBootstrapper.CurrentGrid : null;
        var renderer = mazeBootstrapper != null ? mazeBootstrapper.Renderer : null;

        for (int i = 0; i < 10; i++)
        {
            Vector3 targetPos;

            if (grid != null && renderer != null)
            {
                var coordinate = new MazeCoordinate(Random.Range(0, grid.Width), Random.Range(0, grid.Height));
                if (!renderer.TryGetTileWorldPosition(coordinate, out targetPos)) continue;
            }
            else
            {
                // 수동 테스트 씬은 기존의 원점 중심 범위를 사용한다.
                targetPos = new Vector3(
                    Random.Range(-mazeSize.x * 0.5f, mazeSize.x * 0.5f),
                    transform.position.y,
                    Random.Range(-mazeSize.y * 0.5f, mazeSize.y * 0.5f)
                );
            }

            if (!NavMesh.SamplePosition(targetPos, out var hit, navigationSnapDistance, NavigationFilter())) continue;
            Vector3 offset = Vector3.ProjectOnPlane(hit.position - transform.position, Vector3.up);
            if (offset.sqrMagnitude < minWanderDistance * minWanderDistance) continue;

            // 다른 구역이나 막힌 방의 부분 경로는 배회 목적지로 사용하지 않는다.
            if (!agent.CalculatePath(hit.position, wanderPath)
                || wanderPath.status != NavMeshPathStatus.PathComplete) continue;

            if (agent.SetPath(wanderPath)) return;
        }
    }

    private void CheckPlayerDetection()
    {
        if (targetPlayer == null) return;

        bool isTracking = currentState == AIState.Chase || currentState == AIState.Attack;
        float sightRange = isTracking ? Mathf.Max(detectRange, loseTargetRange) : detectRange;

        if (CanSeePlayer(sightRange))
        {
            lastStimulusPosition = targetPlayer.position;
            currentState = Vector3.Distance(transform.position, targetPlayer.position) <= attackRange
                ? AIState.Attack
                : AIState.Chase;
        }
        else if (isTracking)
        {
            // 시야에서 놓치면 현재 플레이어 위치 대신 마지막으로 본 위치를 수색한다.
            investigationDestinationPending = true;
            currentState = AIState.Investigate;
        }
    }

    private Vector3 EyePosition => eyePoint != null
        ? eyePoint.position
        : transform.position + Vector3.up * eyeHeight;

    private bool CanSeePlayer(float range)
    {
        if (targetPlayer == null) return false;

        Vector3 toPlayer = targetPlayer.position - transform.position;
        if (toPlayer.sqrMagnitude > range * range) return false;

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        Vector3 direction = Vector3.ProjectOnPlane(toPlayer, Vector3.up);
        if (Vector3.Angle(forward, direction) > viewAngle * 0.5f) return false;

        Vector3 targetPosition = playerCollider != null
            ? playerCollider.bounds.center
            : targetPlayer.position;
        Vector3 sightLine = targetPosition - EyePosition;

        // 자신과 플레이어의 Collider는 제외하고 사이에 있는 장애물을 검사한다.
        RaycastHit[] hits = Physics.RaycastAll(EyePosition, sightLine.normalized,
            sightLine.magnitude, sightBlockingLayers, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            Transform hitTransform = hit.collider.transform;
            if (hitTransform.IsChildOf(transform) || hitTransform.IsChildOf(targetPlayer)) continue;
            return false;
        }

        return true;
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        // 정면 감지 범위 (노랑). 눈높이에서 수평 부채꼴로 표시한다.
        Gizmos.color = Color.yellow;
        DrawSightSector(detectRange);

        // 추적 중 시야 유지 범위 (빨강)
        Gizmos.color = Color.red;
        DrawSightSector(Mathf.Max(detectRange, loseTargetRange));

        // 공격 사거리 (파랑)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    private void DrawSightSector(float range)
    {
        const int segments = 32;
        Vector3 origin = EyePosition;
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 previous = origin + Quaternion.AngleAxis(-viewAngle * 0.5f, Vector3.up) * forward * range;
        Gizmos.DrawLine(origin, previous);

        for (int i = 1; i <= segments; i++)
        {
            float angle = -viewAngle * 0.5f + viewAngle * i / segments;
            Vector3 next = origin + Quaternion.AngleAxis(angle, Vector3.up) * forward * range;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }

        Gizmos.DrawLine(origin, previous);
    }
}
