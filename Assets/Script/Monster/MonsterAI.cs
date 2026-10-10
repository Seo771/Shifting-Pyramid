using UnityEngine;
using UnityEngine.AI;

// 1. ISensoryReceiver 인터페이스 상속 추가
public class MonsterAI : MonoBehaviour, ISensoryReceiver
{
    // AI 상태 정의
    public enum AIState { Wander, Chase, Investigate } // Investigate(수색) 추가

    [Header("상태 확인 (확인용)")]
    [SerializeField] private AIState currentState = AIState.Wander;

    [Header("배회(Wander) 설정")]
    [SerializeField] private float wanderRadius = 18f; // 최근에 조정한 값
    [SerializeField] private float wanderInterval = 4f;

    [Header("추적(Chase) 설정")]
    [SerializeField] private Transform targetPlayer;
    [SerializeField] private float detectRange = 8f;     // 감지 거리
    [SerializeField] private float loseTargetRange = 12f; // 포기 거리

    private NavMeshAgent agent;
    private float wanderTimer;
    private Vector3 lastStimulusPosition; // 소리/상호작용 자극 위치 기억용

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
        // 기존거리 기반 감지 유지
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
        }
    }

    #region ISensoryReceiver 구현 (외부 자극 수신)
    // 소리, 시야, 상호작용 자극이 들어왔을 때 실행됨
    public void OnReceiveStimulus(StimulusData stimulus)
    {
        switch (stimulus.type)
        {
            case StimulusType.Sight:
                // 눈으로 봄 ➔ 즉시 추적 상태로 전환
                if (stimulus.source != null)
                {
                    targetPlayer = stimulus.source.transform;
                    currentState = AIState.Chase;
                }
                break;

            case StimulusType.Sound:
            case StimulusType.Interaction:
                // 소리/상호작용 감지 ➔ 추적 중이 아니면 해당 위치로 수색하러 이동
                if (currentState != AIState.Chase)
                {
                    lastStimulusPosition = stimulus.position;
                    agent.SetDestination(lastStimulusPosition);
                    currentState = AIState.Investigate;
                }
                break;
        }
    }
    #endregion

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
        agent.SetDestination(targetPlayer.position);
    }

    private void HandleInvestigate()
    {
        // 소리 난 곳까지 걸어가서 도착하면 다시 배회(Wander)로 복귀
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            currentState = AIState.Wander;
            wanderTimer = wanderInterval; // 즉시 다음 목적지 잡도록 초기화
        }
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

        if (currentState == AIState.Wander && distanceToPlayer <= detectRange)
        {
            currentState = AIState.Chase;
        }
        else if (currentState == AIState.Chase && distanceToPlayer > loseTargetRange)
        {
            currentState = AIState.Wander;
            wanderTimer = wanderInterval;
        }
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
    }
}