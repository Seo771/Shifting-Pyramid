using UnityEngine;

[System.Serializable]
public class ItemData
{
    public string itemName;    // 아이템 이름
    public Sprite itemIcon;    // 핫바 UI에 표시될 2D 아이콘 이미지
    public GameObject itemPrefab; // 나중에 손에 들거나 버릴 때 쓸 3D 프리팹

    // 데이터 중심 소모형 아이템 설정
    public enum ItemType { None, Recovery, Purify }
    [Header("데이터형 아이템 설정")]
    public ItemType itemType = ItemType.None;
    [Tooltip("아이템 사용에 필요한 시간(초). 0이면 즉시 적용")]
    public float useDuration = 0f;
    [Tooltip("회복량(HP)")]
    public float healAmount = 0f;
    [Tooltip("정화량(미라화 수치 감소)")]
    public float purifyAmount = 0f;
}