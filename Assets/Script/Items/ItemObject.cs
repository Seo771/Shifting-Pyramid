using UnityEngine;

public class ItemObject : MonoBehaviour, IInteractable
{
    public ItemData itemData;

    public void Interact()
    {
        // 최신 유니티 방식인 FindFirstObjectByType 사용!
        HotbarController hotbar = Object.FindFirstObjectByType<HotbarController>();

        if (hotbar != null)
        {
            bool added = hotbar.AddItemToHotbar(itemData);
            if (added)
            {
                Destroy(gameObject); // 핫바에 잘 들어갔으면 필드 아이템 삭제
            }
        }
    }
}