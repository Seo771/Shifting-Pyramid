using System.Collections.Generic;
using UnityEngine;

// PlayerInteractor
// - 주변의 IInteractable 감지
// - G키로 상호작용
// - 상호작용 가능한 대상이 가까이 있으면 Interaction UI 표시
// - Interaction UI는 현재 대상의 아래쪽에 표시됨
// - 보물의 Collider 크기와 관계없이 Transform 위치를 기준으로 UI 위치 결정

[RequireComponent(typeof(Collider))]
public class PlayerInteractor : MonoBehaviour
{
    private readonly List<IInteractable> nearbyInteractables =
        new List<IInteractable>();

    [Header("상호작용 설정")]
    [Tooltip("플레이어 주변에서 상호작용할 수 있는 거리")]
    public float interactionRange = 2.5f;

    [Tooltip("상호작용 검사에 사용할 레이어")]
    public LayerMask interactionMask = ~0;

    [Header("상호작용 UI")]
    [Tooltip("F 상호작용 팝업 UI")]
    public GameObject interactionUI;

    [Tooltip("보물 중심에서 아래쪽으로 떨어뜨릴 거리")]
    public float uiVerticalOffset = 0.5f;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        if (interactionUI != null)
        {
            interactionUI.SetActive(false);
        }
    }

    private void Update()
    {
        CleanUpNulls();

        // 가장 가까운 상호작용 대상 찾기
        IInteractable target = FindClosestInteractable();

        if (target != null)
        {
            ShowInteractionUI(target);

            // G키로 상호작용
            if (Input.GetKeyDown(KeyCode.G))
            {
                target.Interact();

                nearbyInteractables.Remove(target);
            }
        }
        else
        {
            HideInteractionUI();
        }
    }

    // ==========================================
    // 가장 가까운 상호작용 대상 찾기
    // ==========================================

    private IInteractable FindClosestInteractable()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            interactionRange,
            interactionMask,
            QueryTriggerInteraction.Collide
        );

        IInteractable closest = null;
        float closestDistance = float.MaxValue;

        foreach (Collider col in colliders)
        {
            MonoBehaviour[] components =
                col.GetComponentsInParent<MonoBehaviour>();

            foreach (MonoBehaviour component in components)
            {
                if (component is IInteractable interactable)
                {
                    float distance = Vector3.Distance(
                        transform.position,
                        component.transform.position
                    );

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closest = interactable;
                    }
                }
            }
        }

        return closest;
    }

    // ==========================================
    // 상호작용 UI 표시
    // ==========================================

    private void ShowInteractionUI(IInteractable target)
    {
        if (interactionUI == null || mainCamera == null)
            return;

        MonoBehaviour interactableObject =
            target as MonoBehaviour;

        if (interactableObject == null)
            return;

        // 보물의 Transform 위치
        Vector3 worldPosition =
            interactableObject.transform.position;

        // 보물 중심보다 아래쪽에 UI 배치
        worldPosition +=
            Vector3.down * uiVerticalOffset;

        // 월드 좌표 → 화면 좌표
        Vector3 screenPosition =
            mainCamera.WorldToScreenPoint(worldPosition);

        // 카메라 뒤에 있으면 숨김
        if (screenPosition.z <= 0)
        {
            interactionUI.SetActive(false);
            return;
        }

        interactionUI.SetActive(true);

        // UI 위치 이동
        interactionUI.transform.position =
            screenPosition;
    }

    // ==========================================
    // 상호작용 UI 숨기기
    // ==========================================

    private void HideInteractionUI()
    {
        if (interactionUI != null)
        {
            interactionUI.SetActive(false);
        }
    }

    // ==========================================
    // Trigger 감지
    // ==========================================

    private void OnTriggerEnter(Collider other)
    {
        MonoBehaviour[] components =
            other.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component is IInteractable interactable)
            {
                if (!nearbyInteractables.Contains(interactable))
                {
                    nearbyInteractables.Add(interactable);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        MonoBehaviour[] components =
            other.GetComponentsInParent<MonoBehaviour>();

        foreach (MonoBehaviour component in components)
        {
            if (component is IInteractable interactable)
            {
                nearbyInteractables.Remove(interactable);
            }
        }
    }

    // ==========================================
    // 외부에서 상호작용 대상 추가
    // ==========================================

    public void AddInteractable(IInteractable interactable)
    {
        if (interactable != null &&
            !nearbyInteractables.Contains(interactable))
        {
            nearbyInteractables.Add(interactable);
        }
    }

    // ==========================================
    // 외부에서 상호작용 대상 제거
    // ==========================================

    public void RemoveInteractable(IInteractable interactable)
    {
        if (interactable != null)
        {
            nearbyInteractables.Remove(interactable);
        }
    }

    // ==========================================
    // 파괴된 오브젝트 정리
    // ==========================================

    private void CleanUpNulls()
    {
        nearbyInteractables.RemoveAll(
            i => i == null || (i as Object) == null
        );
    }

    // ==========================================
    // Scene에서 상호작용 범위 표시
    // ==========================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            interactionRange
        );
    }
}