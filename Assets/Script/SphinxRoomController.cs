using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SphinxRoomController : MonoBehaviour
{
    [Header("UI 연결")]
    [SerializeField] private SphinxQuizUI quizUI;

    [Header("설정")]
    [SerializeField] private float timeLimit = 10f; // 제한시간 (초)

    [Header("벌칙 설정")]
    [Tooltip("오답/시간 초과 시 증가할 미라화 수치")]
    [SerializeField] private float penaltyMummificationAmount = 25f;

    [Header("외부 이벤트 연동")]
    [Tooltip("정답을 맞췄을 때 호출 (보상 상자 스폰 등)")]
    public UnityEvent OnQuizSuccess;

    [Tooltip("오답이거나 시간 초과일 때 추가 호출 (필요 시 사용)")]
    public UnityEvent OnQuizFailure;

    [Header("문제 데이터")]
    [SerializeField] private List<QuizData> riddleList = new List<QuizData>();

    private QuizData currentRiddle;
    private bool isQuizActive = false;
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
        if (isQuizActive) return;

        if (riddleList == null || riddleList.Count == 0)
        {
            Debug.LogWarning("[SphinxRoom] 등록된 퀴즈가 없습니다!");
            return;
        }

        int randomIndex = Random.Range(0, riddleList.Count);
        currentRiddle = riddleList[randomIndex];

        currentTimer = timeLimit;
        isQuizActive = true;

        if (quizUI != null)
        {
            quizUI.ShowQuiz(currentRiddle.question);
            quizUI.UpdateTimerText(currentTimer);
        }

        Debug.Log($"[SphinxRoom] 퀴즈 시작! 출제된 문제: {currentRiddle.question}");
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
        EndQuiz();
        OnQuizSuccess?.Invoke();
    }

    private void OnWrongAnswer()
    {
        Debug.Log("<color=red>[SphinxRoom] 오답입니다! 미라화 벌칙을 부여합니다.</color>");
        ApplyMummificationPenalty();
        EndQuiz();
        OnQuizFailure?.Invoke();
    }

    private void OnTimeOut()
    {
        Debug.Log("<color=red>[SphinxRoom] 시간 초과! 미라화 벌칙을 부여합니다.</color>");
        ApplyMummificationPenalty();
        EndQuiz();
        OnQuizFailure?.Invoke();
    }

    // 씬의 Player 오브젝트에서 Mummification 컴포넌트를 찾아 수치 증가
    private void ApplyMummificationPenalty()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            Mummification mummification = player.GetComponent<Mummification>();
            if (mummification != null)
            {
                mummification.IncreaseMummification(penaltyMummificationAmount);
                Debug.Log($"<color=orange>[SphinxRoom] 미라화 수치 +{penaltyMummificationAmount}% 적용 완료!</color>");
            }
            else
            {
                Debug.LogWarning("[SphinxRoom] Player 오브젝트에서 Mummification 컴포넌트를 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogWarning("[SphinxRoom] 씬에서 'Player' 태그를 가진 오브젝트를 찾을 수 없습니다.");
        }
    }

    private void EndQuiz()
    {
        isQuizActive = false;
        if (quizUI != null)
        {
            quizUI.HideQuiz();
        }
    }
}