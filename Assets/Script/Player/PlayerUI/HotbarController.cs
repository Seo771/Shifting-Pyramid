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