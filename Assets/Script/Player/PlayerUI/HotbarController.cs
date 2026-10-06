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
    // 사용 지연 관련 설정
    [Header("사용 지연 설정")]
    [Tooltip("아이템 사용에 필요한 시간(초)")]
    public float useDuration = 3f;
    [Header("사용 UI (선택 사항)")]
    [Tooltip("사용 진행도를 표시할 Slider를 연결하세요(선택). 빈값이면 UI 표시 안함)")]
    public Slider useProgressSlider;
    [Tooltip("사용 시간 텍스트(선택)")]
    public Text useProgressText;
    [Tooltip("사용 UI를 감쌀 CanvasGroup(선택)")]
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

        // 핫바 아이템 사용 키(F) 누르면 사용 시작
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

                // HotbarSlot 컴포넌트가 있으면 리스트에 추가
                HotbarSlot slotScript = child.GetComponent<HotbarSlot>();
                if (slotScript != null)
                {
                    hotbarSlots.Add(slotScript);
                }
            }
        }
    }

    // 핫바 사용 키는 마우스 우클릭으로 고정되어 있습니다.

    // 현재 선택된 슬롯의 아이템을 사용합니다.
    // RecoveryPotion 프리팹이면 플레이어의 HP를 회복시키고 슬롯에서 제거합니다.
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

        if (item.itemPrefab != null)
        {
            // RecoveryPotion인지 체크
            var potion = item.itemPrefab.GetComponent<RecoveryPotion>();
            if (potion != null)
            {
                // 이미 사용 중이면 새로 시작하지 않음
                if (useCoroutine != null)
                {
                    Debug.Log("[Hotbar] 이미 사용 중입니다.");
                    return;
                }

                // 시작 가능한지 상태 확인 (실제 효과 적용은 완료 시점에 함)
                var playerHP = Object.FindFirstObjectByType<PlayerHP>();
                if (playerHP == null || playerHP.isDead)
                {
                    Debug.LogWarning("[Hotbar] PlayerHP를 찾을 수 없거나 사망 상태입니다.");
                    return;
                }

                // 지연 사용 시작
                pendingSlot = slot;
                useCoroutine = StartCoroutine(UseRoutine(potion, slot));
                return;
            }

            // 다른 아이템 타입은 인스턴스화하여 동작시키거나 별도 처리 필요
            // 기본 동작: 프리팹을 인스턴스화하여 사용 가능한 컴포넌트가 있으면 호출
            GameObject inst = Instantiate(item.itemPrefab);
            // 만약 인스턴스가 핫바 사용 시 자동으로 소모되게 설계되었다면 슬롯을 비웁니다.
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

    private System.Collections.IEnumerator UseRoutine(RecoveryPotion potion, HotbarSlot slot)
    {
        float elapsed = 0f;

        // 초기 UI 표시
        if (useProgressGroup != null) useProgressGroup.alpha = 1f;
        if (useProgressSlider != null) useProgressSlider.value = 0f;
        if (useProgressText != null) useProgressText.text = $"{Mathf.CeilToInt(useDuration)}s";
        // 시작 프레임의 키 입력을 무시하기 위해 한 프레임 대기합니다.
        // Update()에서 F키로 코루틴을 시작한 그 프레임의 GetKeyDown이
        // 즉시 취소 신호로 인식되는 문제를 방지합니다.
        yield return null;

        while (elapsed < useDuration)
        {
            // 취소 조건: ESC 또는 F 재누름
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F))
            {
                Debug.Log("[Hotbar] 포션 사용 취소됨");
                useCoroutine = null;
                pendingSlot = null;
                if (useProgressGroup != null) useProgressGroup.alpha = 0f;
                yield break;
            }

            elapsed += Time.deltaTime;

            // UI 업데이트
            if (useProgressSlider != null) useProgressSlider.value = Mathf.Clamp01(elapsed / useDuration);
            if (useProgressText != null) useProgressText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, useDuration - elapsed))}s";

            yield return null;
        }

        // 사용 완료 시 효과 적용 (포션 힐)
        var playerHP = Object.FindFirstObjectByType<PlayerHP>();
        if (playerHP != null && !playerHP.isDead)
        {
            float before = playerHP.currentHealth;
            playerHP.currentHealth = Mathf.Min(playerHP.maxHealth, playerHP.currentHealth + potion.healAmount);
            Debug.Log($"[Hotbar] 포션 사용: HP {before} -> {playerHP.currentHealth}");
            if (slot != null)
            {
                slot.ClearSlot();
                ShowHotbar();
            }
        }
        else
        {
            Debug.LogWarning("[Hotbar] PlayerHP를 찾을 수 없거나 사망 상태입니다.");
        }

        useCoroutine = null;
        pendingSlot = null;
        if (useProgressGroup != null) useProgressGroup.alpha = 0f;
        Debug.Log("[Hotbar] 포션 사용 완료");
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
        // 1. 현재 선택된 슬롯이 비어있다면 거기에 먼저 넣음
        if (currentSlotIndex < hotbarSlots.Count && hotbarSlots[currentSlotIndex].IsEmpty())
        {
            hotbarSlots[currentSlotIndex].AddItem(item);
            ShowHotbar();
            return true;
        }

        // 2. 아니면 첫 번째로 비어있는 슬롯을 찾아 들어감
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
        return false; // 인벤토리 꽉 참
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