using System;
using UnityEngine;

// 감각 자극 종류
public enum StimulusType
{
    Sight,          // 시야 감지 (눈으로 봄)
    Sound,          // 소리 감지 (발소리, 아이템 소음)
    Interaction     // 상호작용 감지 (문 열기, 레버 등)
}

// 전달될 자극 데이터 구조체
public struct StimulusData
{
    public StimulusType type;      // 자극 종류
    public Vector3 position;       // 발생 위치
    public GameObject source;      // 자극을 일으킨 주체
    public float intensity;        // 자극 강도

    public StimulusData(StimulusType type, Vector3 position, GameObject source, float intensity = 1f)
    {
        this.type = type;
        this.position = position;
        this.source = source;
        this.intensity = intensity;
    }
}

// 자극을 수신할 수 있는 클래스가 구현하는 인터페이스
public interface ISensoryReceiver
{
    void OnReceiveStimulus(StimulusData stimulus);
}