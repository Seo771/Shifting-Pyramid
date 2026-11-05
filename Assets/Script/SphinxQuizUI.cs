using UnityEngine;
using TMPro;

public class SphinxQuizUI : MonoBehaviour
{
    [Header("컨트롤러 연결")]
    [SerializeField] private SphinxRoomController roomController;

    [Header("UI 요소 연결")]
    [SerializeField] private GameObject quizPanel;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_InputField answerInput;
    [SerializeField] private TMP_Text timerText;

    private void Start()
    {
        // Start 시점에 InputField 이벤트 안전하게 등록
        if (answerInput != null)
        {
            // onSubmit은 Enter(Return/KeypadEnter) 키를 눌렀을 때 실행되는 TMP 전용 이벤트입니다.
            answerInput.onSubmit.AddListener(OnInputSubmit);
        }

        HideQuiz();
    }

    public void ShowQuiz(string question)
    {
        if (quizPanel != null) quizPanel.SetActive(true);
        if (questionText != null) questionText.text = question;
        if (answerInput != null)
        {
            answerInput.text = "";
            answerInput.Select();
            answerInput.ActivateInputField(); // 바로 입력 가능하게 포커스 지정
        }
    }

    public void HideQuiz()
    {
        if (quizPanel != null) quizPanel.SetActive(false);
    }

    public void UpdateTimerText(float seconds)
    {
        if (timerText != null)
            timerText.text = $"남은 시간: {Mathf.CeilToInt(seconds)}초";
    }

    // 엔터 입력 시 실행되는 제출 메서드
    private void OnInputSubmit(string text)
    {
        // 빈 문자열 제출 방지
        if (string.IsNullOrWhiteSpace(text)) return;

        if (roomController != null)
        {
            roomController.SubmitAnswer(text);
        }
    }
}