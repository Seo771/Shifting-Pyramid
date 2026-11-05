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

    private void Awake()
    {
        HideQuiz();

        // InputField에서 엔터(EndEdit)를 눌렀을 때 실행될 이벤트 등록
        if (answerInput != null)
        {
            answerInput.onEndEdit.AddListener(OnInputSubmit);
        }
    }

    public void ShowQuiz(string question)
    {
        if (quizPanel != null) quizPanel.SetActive(true);
        if (questionText != null) questionText.text = question;
        if (answerInput != null)
        {
            answerInput.text = "";
            answerInput.ActivateInputField(); // 바로 입력 가능하게 포커스
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

    // 엔터 입력 시 호출되는 내부 메서드
    private void OnInputSubmit(string text)
    {
        // Enter 키를 눌러서 끝낸 경우에만 제출 처리
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (roomController != null)
            {
                roomController.SubmitAnswer(text);
            }
        }
    }
}