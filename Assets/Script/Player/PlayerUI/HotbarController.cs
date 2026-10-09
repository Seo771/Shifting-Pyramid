using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class HotbarController : MonoBehaviour
{
    [Header("UI 구성요소")]
    public Transform slotParent;
    public RectTransform highlightUI;

    [Header("슬롯 설정")]
    public int currentSlotIndex = 0;

    [Header("자동 페이드아웃 설정")]
    public float visibleDuration = 2.0f;
    public float fadeSpeed = 3.0f;

    private List<RectTransform> slots = new List<RectTransform>();
    private List<HotbarSlot> hotbarSlots = new List<HotbarSlot>(); // 슬롯 스크립트 리스트
    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    // 사용 중인 아이템 지연 처리 관련
    private Coroutine useCoroutine;
    private HotbarSlot pendingSlot;

    [Header("사용 지연 설정 (기본)")]
    [Tooltip("기본 사용 시간(초). ItemData.useDuration이 0이면 이 값 사용")]
    public float defaultUseDuration = 3f;

    [Header("사용 UI (선택 사항)")]
    public Slider useProgressSlider;
    public Text useProgressText;
    public CanvasGroup useProgressGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        RefreshSlots();
        LayoutRebuilder.ForceRebuildLayoutImmediate(slotParent.GetComponent<RectTransform>());
        SelectSlot(0);
        ShowHotbar();
    }

    void Update()
    {
        bool inputDetected = false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
                inputDetected = true;
                break;
            }
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            int newIndex = currentSlotIndex - 1;
            if (newIndex < 0) newIndex = slots.Count - 1;
            SelectSlot(newIndex);
            inputDetected = true;
        }
        else if (scroll < 0f)
        {
            int newIndex = currentSlotIndex + 1;
            if (newIndex >= slots.Count) newIndex = 0;
            SelectSlot(newIndex);
            inputDetected = true;
        }

        if (inputDetected)
        {
            ShowHotbar();
        }

        // 핫바 아이템 사용 키(F) 누르면 사용 시작 또는 취소
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (useCoroutine == null)
                UseCurrentSlot();
            else
                CancelUse();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (useCoroutine != null) CancelUse();
        }
    }

    public void RefreshSlots()
    {
        slots.Clear();
        hotbarSlots.Clear();

        if (slotParent == null) return;

        foreach (Transform child in slotParent)
        {
            if (highlightUI != null && child.gameObject != highlightUI.gameObject)
            {
                slots.Add(child.GetComponent<RectTransform>());

                HotbarSlot slotScript = child.GetComponent<HotbarSlot>();
                if (slotScript != null)
                {
                    hotbarSlots.Add(slotScript);
                }
            }
        }
    }

    // 현재 선택된 슬롯의 아이템을 사용합니다. 데이터 중심 방식(ItemData)으로 처리합니다.
    public void UseCurrentSlot()
    {
        if (hotbarSlots.Count == 0) return;

        HotbarSlot slot = hotbarSlots[currentSlotIndex];
        if (slot == null) return;

        ItemData item = slot.GetItem();
        if (item == null)
        {
            Debug.Log("[Hotbar] 선택된 슬롯에 아이템이 없습니다.");
            return;
        }

        // 소비형(ItemType) 처리
        if (item.itemType == ItemData.ItemType.Recovery || item.itemType == ItemData.ItemType.Purify)
        {
            if (useCoroutine != null)
            {
                Debug.Log("[Hotbar] 이미 사용 중입니다.");
                return;
            }

            // 상태 검증
            if (item.itemType == ItemData.ItemType.Recovery)
            {
                var playerHP = Object.FindFirstObjectByType<PlayerHP>();
                if (playerHP == null || playerHP.isDead)
                {
                    Debug.LogWarning("[Hotbar] PlayerHP를 찾을 수 없거나 사망 상태입니다.");
                    return;
                }
            }
            else if (item.itemType == ItemData.ItemType.Purify)
            {
                var mumi = Object.FindFirstObjectByType<Mummification>();
                if (mumi == null || mumi.IsMummified)
                {
                    Debug.LogWarning("[Hotbar] Mummification을 찾을 수 없거나 이미 미라화 상태입니다.");
                    return;
                }
            }

            // item.useDuration == 0이면 즉시 적용(지연 없음), 양수면 해당 시간만큼 지연
            if (item.useDuration <= 0f)
            {
                ApplyConsumableEffect(item, slot);
                return;
            }

            float duration = item.useDuration;
            pendingSlot = slot;
            useCoroutine = StartCoroutine(UseConsumableRoutine(item, slot, duration));
            return;
        }

        // 그 외: 기존 프리팹 인스턴스화 동작(이전 동작 유지)
        if (item.itemPrefab != null)
        {
            GameObject inst = Instantiate(item.itemPrefab);
            slot.ClearSlot();
            ShowHotbar();
        }
    }

    private void CancelUse()
    {
        if (useCoroutine == null) return;
        StopCoroutine(useCoroutine);
        useCoroutine = null;
        pendingSlot = null;
        if (useProgressGroup != null) useProgressGroup.alpha = 0f;
        if (useProgressSlider != null) useProgressSlider.value = 0f;
        if (useProgressText != null) useProgressText.text = "";
        Debug.Log("[Hotbar] 사용 취소 처리됨");
    }

    private IEnumerator UseConsumableRoutine(ItemData item, HotbarSlot slot, float duration)
    {
        float elapsed = 0f;

        if (useProgressGroup != null) useProgressGroup.alpha = 1f;
        if (useProgressSlider != null) useProgressSlider.value = 0f;
        if (useProgressText != null) useProgressText.text = $"{Mathf.CeilToInt(duration)}s";

        // 시작 프레임 입력 무시
        yield return null;

        while (elapsed < duration)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log("[Hotbar] 사용 취소됨");
                useCoroutine = null;
                pendingSlot = null;
                if (useProgressGroup != null) useProgressGroup.alpha = 0f;
                yield break;
            }

            elapsed += Time.deltaTime;
            if (useProgressSlider != null) useProgressSlider.value = Mathf.Clamp01(elapsed / duration);
            if (useProgressText != null) useProgressText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, duration - elapsed))}s";
            yield return null;
        }

        // 사용 완료: 데이터 기반 효과 적용
        if (item.itemType == ItemData.ItemType.Recovery)
        {
            var playerHP = Object.FindFirstObjectByType<PlayerHP>();
            if (playerHP != null && !playerHP.isDead)
            {
                float before = playerHP.currentHealth;
                playerHP.currentHealth = Mathf.Min(playerHP.maxHealth, playerHP.currentHealth + item.healAmount);
                Debug.Log($"[Hotbar] 포션 사용: HP {before} -> {playerHP.currentHealth}");
            }
        }
        else if (item.itemType == ItemData.ItemType.Purify)
        {
            var mumi = Object.FindFirstObjectByType<Mummification>();
            if (mumi != null && !mumi.IsMummified)
            {
                mumi.DecreaseMummification(item.purifyAmount);
                Debug.Log($"[Hotbar] 정화 사용: 미라화 -{item.purifyAmount}");
            }
        }

        if (slot != null)
        {
            slot.ClearSlot();
            ShowHotbar();
        }

        useCoroutine = null;
        pendingSlot = null;
        if (useProgressGroup != null) useProgressGroup.alpha = 0f;
        Debug.Log("[Hotbar] 사용 완료");
    }

    // 즉시(지연 없이) 효과 적용
    private void ApplyConsumableEffect(ItemData item, HotbarSlot slot)
    {
        if (item.itemType == ItemData.ItemType.Recovery)
        {
            var playerHP = Object.FindFirstObjectByType<PlayerHP>();
            if (playerHP != null && !playerHP.isDead)
            {
                float before = playerHP.currentHealth;
                playerHP.currentHealth = Mathf.Min(playerHP.maxHealth, playerHP.currentHealth + item.healAmount);
                Debug.Log($"[Hotbar] 즉시 포션 사용: HP {before} -> {playerHP.currentHealth}");
            }
        }
        else if (item.itemType == ItemData.ItemType.Purify)
        {
            var mumi = Object.FindFirstObjectByType<Mummification>();
            if (mumi != null && !mumi.IsMummified)
            {
                mumi.DecreaseMummification(item.purifyAmount);
                Debug.Log($"[Hotbar] 즉시 정화 사용: 미라화 -{item.purifyAmount}");
            }
        }

        if (slot != null)
        {
            slot.ClearSlot();
            ShowHotbar();
        }
    }

    public void SelectSlot(int index)
    {
        if (slots.Count == 0 || index < 0 || index >= slots.Count) return;

        currentSlotIndex = index;

        if (highlightUI != null)
        {
            highlightUI.position = slots[index].position;
        }
    }

    // ★ 아이템 획득 시 빈 슬롯에 아이템 추가하는 함수
    public bool AddItemToHotbar(ItemData item)
    {
        if (currentSlotIndex < hotbarSlots.Count && hotbarSlots[currentSlotIndex].IsEmpty())
        {
            hotbarSlots[currentSlotIndex].AddItem(item);
            ShowHotbar();
            return true;
        }

        for (int i = 0; i < hotbarSlots.Count; i++)
        {
            if (hotbarSlots[i].IsEmpty())
            {
                hotbarSlots[i].AddItem(item);
                ShowHotbar();
                return true;
            }
        }

        Debug.Log("인벤토리가 가득 찼습니다!");
        return false;
    }

    public void ShowHotbar()
    {
        canvasGroup.alpha = 1.0f;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        yield return new WaitForSeconds(visibleDuration);
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        canvasGroup.alpha = 0f;
    }
}
