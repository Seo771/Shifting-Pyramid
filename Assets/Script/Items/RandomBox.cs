using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RandomBoxItemEntry
{
    public ItemData itemData;

    [Range(0f, 100f)]
    [Tooltip("이 아이템이 당첨될 확률(%)입니다. 전체 확률의 합은 100%가 되도록 설정하세요.")]
    public float probability;
}

public class RandomBox : MonoBehaviour, IInteractable
{
    [Header("랜덤 상자 아이템 설정")]
    [Tooltip("상호작용 시 당첨 후보가 될 아이템과 각 아이템의 확률입니다.")]
    [SerializeField]
    private List<RandomBoxItemEntry> itemEntries = new List<RandomBoxItemEntry>();

    [Header("상태")]
    [SerializeField]
    private bool isOpened;

    public bool IsOpened => isOpened;

    public void Interact()
    {
        if (isOpened)
        {
            return;
        }

        // 상호작용을 시작한 순간 열린 상태로 만들어 중복 상호작용을 차단합니다.
        isOpened = true;

        ItemData reward = SelectReward();
        if (reward == null)
        {
            Debug.LogWarning($"[{nameof(RandomBox)}] 유효한 당첨 아이템을 선택하지 못했습니다.", this);
            return;
        }

        HotbarController hotbar = FindFirstObjectByType<HotbarController>();
        if (hotbar == null)
        {
            Debug.LogWarning($"[{nameof(RandomBox)}] HotbarController를 찾을 수 없어 아이템을 지급하지 못했습니다.", this);
            return;
        }

        if (hotbar.AddItemToHotbar(reward))
        {
            Debug.Log($"[{nameof(RandomBox)}] '{reward.itemName}' 아이템을 획득했습니다.", this);
        }
        else
        {
            Debug.LogWarning($"[{nameof(RandomBox)}] 핫바가 가득 차 '{reward.itemName}' 아이템을 지급하지 못했습니다.", this);
        }
    }

    private ItemData SelectReward()
    {
        float totalProbability = 0f;

        foreach (RandomBoxItemEntry entry in itemEntries)
        {
            if (entry == null || entry.itemData == null || entry.probability <= 0f)
            {
                continue;
            }

            totalProbability += entry.probability;
        }

        if (totalProbability <= 0f)
        {
            return null;
        }

        float roll = UnityEngine.Random.Range(0f, totalProbability);
        float cumulativeProbability = 0f;

        foreach (RandomBoxItemEntry entry in itemEntries)
        {
            if (entry == null || entry.itemData == null || entry.probability <= 0f)
            {
                continue;
            }

            cumulativeProbability += entry.probability;
            if (roll < cumulativeProbability)
            {
                return entry.itemData;
            }
        }

        // 부동소수점 오차로 마지막 항목에 도달하지 못하는 경우를 방지합니다.
        for (int i = itemEntries.Count - 1; i >= 0; i--)
        {
            RandomBoxItemEntry entry = itemEntries[i];
            if (entry != null && entry.itemData != null && entry.probability > 0f)
            {
                return entry.itemData;
            }
        }

        return null;
    }

    private void OnValidate()
    {
        float totalProbability = 0f;

        foreach (RandomBoxItemEntry entry in itemEntries)
        {
            if (entry != null)
            {
                totalProbability += Mathf.Max(0f, entry.probability);
            }
        }

        if (itemEntries.Count > 0 && !Mathf.Approximately(totalProbability, 100f))
        {
            Debug.LogWarning(
                $"[{nameof(RandomBox)}] 확률의 합은 100%를 권장합니다. 현재 합계: {totalProbability:0.##}%",
                this
            );
        }
    }
}
