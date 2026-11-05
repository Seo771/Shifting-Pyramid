using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SphinxRoomController : MonoBehaviour
{
    [Header("UI 연동")]
    [SerializeField] private SphinxQuizUI quizUI;

    [Header("제한시간 설정")]
    [SerializeField] private float timeLimit = 10f;

    [Header("벌칙 설정")]
    [Tooltip("오답/시간 초과 시 증가할 미라화 수치")]
    [SerializeField] private float penaltyMummificationAmount = 25f;

    [Header("보상 상자 스폰 설정")]
    [Tooltip("정답 시 생성할 아이템 상자 프리팹 (Project 창의 프리팹 끌어다 놓기)")]
    [SerializeField] private GameObject rewardChestPrefab;
    [Tooltip("상자가 생성될 위치 (Hierarchy의 RewardSpawnPoint 끌어다 놓기)")]
    [SerializeField] private Transform rewardSpawnPoint;

    [Header("이벤트 연동")]
    public UnityEvent OnQuizSuccess;
    public UnityEvent OnQuizFailure;

    [Header("문제 목록")]
    [SerializeField] private List<QuizData> riddleList = new List<QuizData>();

    private QuizData currentRiddle;
    private bool isQuizActive = false;
    private bool isRoomCleared = false; // 방 클리어 여부 (중복 실행 방지)
    private float currentTimer = 0f;

    private void Update()
    {
        if (!isQuizActive) return;

        currentTimer -= Time.deltaTime;

        if (quizUI != null)
        {
            quizUI.UpdateTimerText(currentTimer);
        }

        if (currentTimer <= 0f)
        {
            OnTimeOut();
        }
    }

    public void StartQuiz()
    {
        // 이미 방을 클리어했거나 퀴즈 진행 중이면 실행 안 함
        if (isQuizActive || isRoomCleared) return;

        if (riddleList == null || riddleList.Count == 0)
        {
            Debug.LogWarning("[SphinxRoom] 등록된 퀴즈가 없습니다!");
            return;
        }

        int randomIndex = Random.Range(0, riddleList.Count);
        currentRiddle = riddleList[randomIndex];

        currentTimer = timeLimit;
        isQuizActive = true;

        // 플레이어 이동 멈춤
        SetPlayerInputEnabled(false);

        if (quizUI != null)
        {
            quizUI.ShowQuiz(currentRiddle.question);
            quizUI.UpdateTimerText(currentTimer);
        }

        Debug.Log("[SphinxRoom] 퀴즈 시작! 플레이어 이동이 제한됩니다.");
    }

    public void SubmitAnswer(string inputAnswer)
    {
        if (!isQuizActive || currentRiddle == null) return;

        string formattedInput = inputAnswer.Trim();
        string correctAnswer = currentRiddle.answer.Trim();

        if (formattedInput == correctAnswer)
        {
            OnCorrectAnswer();
        }
        else
        {
            OnWrongAnswer();
        }
    }

    private void OnCorrectAnswer()
    {
        Debug.Log("<color=green>[SphinxRoom] 정답입니다!</color>");
        isRoomCleared = true; // 클리어 처리 (재방문 시 퀴즈 안 뜸)
        SpawnReward();
        EndQuiz();
        OnQuizSuccess?.Invoke();
    }

    private void OnWrongAnswer()
    {
        Debug.Log("<color=red>[SphinxRoom] 오답입니다!</color>");
        ApplyMummificationPenalty();
        EndQuiz();
        OnQuizFailure?.Invoke();
    }

    private void OnTimeOut()
    {
        Debug.Log("<color=red>[SphinxRoom] 시간 초과!</color>");
        ApplyMummificationPenalty();
        EndQuiz();
        OnQuizFailure?.Invoke();
    }

    private void EndQuiz()
    {
        isQuizActive = false;

        if (quizUI != null)
        {
            quizUI.HideQuiz();
        }

        // 플레이어 이동 다시 허용
        SetPlayerInputEnabled(true);
    }

    // 아이템 상자 스폰 메서드
    private void SpawnReward()
    {
        if (rewardChestPrefab != null && rewardSpawnPoint != null)
        {
            Instantiate(rewardChestPrefab, rewardSpawnPoint.position, rewardSpawnPoint.rotation);
            Debug.Log("<color=cyan>[SphinxRoom] 보상 상자 스폰 완료!</color>");
        }
        else
        {
            Debug.LogWarning("[SphinxRoom] rewardChestPrefab 또는 rewardSpawnPoint 연결을 확인해 주세요!");
        }
    }

    // 미라화 벌칙 적용
    private void ApplyMummificationPenalty()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Mummification mummification = player.GetComponent<Mummification>();
            if (mummification != null)
            {
                mummification.IncreaseMummification(penaltyMummificationAmount);
            }
        }
    }

    // 플레이어 이동 제어
    private void SetPlayerInputEnabled(bool isEnabled)
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.enabled = isEnabled;

            if (!isEnabled && movement.animator != null)
            {
                movement.animator.SetFloat("Speed", 0f);
            }
        }
    }
}