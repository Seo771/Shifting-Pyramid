using UnityEngine;

public class ItemObject : MonoBehaviour, IInteractable
{
    public ItemData itemData;

    public void Interact()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (itemData == null)
        {
            Debug.LogWarning($"[{nameof(ItemObject)}] 아이템 데이터가 비어 있습니다.", this);
            return;
        }

        if (itemData.itemType == ItemData.ItemType.PassiveSpeed)
        {
            PlayerMovement player = Object.FindFirstObjectByType<PlayerMovement>();
            if (player == null)
            {
                Debug.LogWarning($"[{nameof(ItemObject)}] PlayerMovement를 찾을 수 없습니다.", this);
                return;
            }

            player.AddPassiveItem(itemData);
            Destroy(gameObject);
            return;
        }

        HotbarController hotbar = Object.FindFirstObjectByType<HotbarController>();
        if (hotbar == null)
        {
            Debug.LogWarning($"[{nameof(ItemObject)}] HotbarController를 찾을 수 없습니다.", this);
            return;
        }

        bool added = hotbar.AddItemToHotbar(itemData);
        if (added)
        {
            Destroy(gameObject);
        }
    }
}
