using System;
using UnityEngine;

[Serializable]
public class QuizData
{
    [TextArea(2, 4)]
    public string question; // 문제 텍스트
    public string answer;   // 정답 텍스트
}
