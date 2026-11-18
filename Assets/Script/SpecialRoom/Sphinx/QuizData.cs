using System;
using UnityEngine;

[Serializable]
public class QuizData
{
    [TextArea(2, 4)]
    public string question; // 문제 텍스트
    public string answer;   // 정답 텍스트

    public string[] alternativeAnswers = Array.Empty<string>();

    public bool MatchesAnswer(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        string trimmedInput = input.Trim();
        if (string.Equals(trimmedInput, answer?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (alternativeAnswers == null) return false;

        foreach (string alternative in alternativeAnswers)
        {
            if (string.Equals(trimmedInput, alternative?.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
