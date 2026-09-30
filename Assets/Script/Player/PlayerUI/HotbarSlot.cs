using UnityEngine;
using UnityEngine.UI;

public class HotbarSlot : MonoBehaviour
{
    public Image itemIconImage; // 아이콘을 띄울 UI Image (슬롯 자식으로 넣을 예정)
    private ItemData currentItem;

    void Awake()
    {
        // 아이콘 이미지가 할당되어 있다면 기본은 투명하게(비어있게) 처리
        if (itemIconImage != null)
        {
            itemIconImage.enabled = false;
        }
    }

    // 슬롯에 아이템 추가
    public void AddItem(ItemData newItem)
    {
        currentItem = newItem;

        if (itemIconImage != null && newItem.itemIcon != null)
        {
            itemIconImage.sprite = newItem.itemIcon;
            itemIconImage.color = Color.white; // 색상 원래대로
            itemIconImage.enabled = true;      // 이미지 보이기
        }
    }

    // 슬롯 비우기
    public void ClearSlot()
    {
        currentItem = null;
        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            itemIconImage.enabled = false; // 이미지 숨기기
        }
    }

    // 슬롯이 비어있는지 확인
    public bool IsEmpty()
    {
        return currentItem == null;
    }

    public ItemData GetItem()
    {
        return currentItem;
    }
}