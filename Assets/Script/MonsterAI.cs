using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
{
    // AI 상태 정의 (추후 감지 시스템 확장용)
    public enum AIState { Wander, Chase }

    [Header("상태 확인 (확인용)")]
    [SerializeField] private AIState currentState = AIState.Wander;

    [Header("배회(Wander) 설정")]
    [SerializeField] private float wanderRadius = 10f;
    [SerializeField] private float wanderInterval = 4f;

    [Header("추적(Chase) 설정")]
    [SerializeField] private Transform targetPlayer;
    [SerializeField] private float detectRange = 8f;     // 감지 거리 (이 안에 들어오면 추적)
    [SerializeField] private float loseTargetRange = 12f; // 포기 거리 (이보다 멀어지면 다시 배회)

    private NavMeshAgent agent;
    private float wanderTimer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        wanderTimer = wanderInterval;

        // 플레이어 자동 탐색 (태그 기준)
        if (targetPlayer == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                targetPlayer = playerObj.transform;
            }
        }
    }

    private void Update()
    {
        // 1. 거리 기반 기본 감지 (3단계에서 시야/소리 감지로 확장될 부분)
        CheckPlayerDetection();

        // 2. 현재 상태에 따른 행동 수행
        switch (currentState)
        {
            case AIState.Wander:
                HandleWander();
                break;

            case AIState.Chase:
                HandleChase();
                break;
        }
    }

    #region AI 상태별 로직
    private void HandleWander()
    {
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

        // 플레이어의 현재 위치로 계속 이동 명령
        agent.SetDestination(targetPlayer.position);
    }

    private void SetRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    private void CheckPlayerDetection()
    {
        if (targetPlayer == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

        // 배회 중 플레이어가 감지 거리 안에 들어오면 ➔ 추적 모드
        if (currentState == AIState.Wander && distanceToPlayer <= detectRange)
        {
            currentState = AIState.Chase;
        }
        // 추적 중 플레이어가 포기 거리보다 멀어지면 ➔ 다시 배회 모드
        else if (currentState == AIState.Chase && distanceToPlayer > loseTargetRange)
        {
            currentState = AIState.Wander;
            wanderTimer = wanderInterval; // 바로 새 목적지 잡도록 초기화
        }
    }
    #endregion

    // Scene 뷰에서 감지 거리를 눈으로 쉽게 확인하기 위한 기즈모
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
    }
}