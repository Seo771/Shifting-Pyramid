using UnityEngine;

[System.Serializable]
public class ItemData
{
    public string itemName;    // 아이템 이름
    public Sprite itemIcon;    // 핫바 UI에 표시될 2D 아이콘 이미지
    public GameObject itemPrefab; // 나중에 손에 들거나 버릴 때 쓸 3D 프리팹
}