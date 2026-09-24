using System.Collections.Generic;
using UnityEngine;

// PlayerInteractor는 플레이어(예: CharacterController)에 붙여 사용합니다.
// 역할:
// - 주변의 IInteractable을 트리거 콜백으로 감지하여 리스트에 보관
// - Update에서 G키를 누르면 가장 가까운(또는 먼저 감지된) IInteractable의 Interact()를 호출
// 에디터 설정(권장):
// 1) 플레이어에 자식 오브젝트를 만들고 SphereCollider(또는 BoxCollider)를 추가한 후 Is Trigger 체크
// 2) 같은 자식 오브젝트에 Rigidbody를 추가하고 "Is Kinematic" 체크(또는 Use Gravity 해제)
//    -> CharacterController 자체는 Rigidbody가 아니므로 트리거 이벤트가 발생하려면 Rigidbody가 필요합니다.
// 3) PlayerInteractor 스크립트를 플레이어(또는 상호작용을 담당할 객체)에 붙입니다.

[RequireComponent(typeof(Collider))]
public class PlayerInteractor : MonoBehaviour
{
    // 감지된 상호작용 가능한 객체를 보관
    private readonly List<IInteractable> nearbyInteractables = new List<IInteractable>();

    // 현재 선택된(우선순위로 사용할) 상호작용 대상
    // 간단하게 nearbyInteractables[0]을 사용하므로 이 변수는 편의용입니다.
    private IInteractable currentInteractable => nearbyInteractables.Count > 0 ? nearbyInteractables[0] : null;

    private void Reset()
    {
        // RequireComponent로 Collider가 붙어야 한다는 안내용. 실제로는 자식의 Trigger Collider를 권장합니다.
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            if (currentInteractable != null)
            {
                currentInteractable.Interact();
            }
            else
            {
                Debug.Log("[PlayerInteractor] 상호작용 가능한 대상이 없습니다.");
            }
        }
    }

    // 다른 객체의 Collider가 플레이어의 트리거에 들어왔을 때 호출
    private void OnTriggerEnter(Collider other)
    {
        // 인터페이스를 구현한 컴포넌트를 찾음
        var comps = other.GetComponents<MonoBehaviour>();
        foreach (var c in comps)
        {
            if (c is IInteractable interactable)
            {
                if (!nearbyInteractables.Contains(interactable))
                {
                    nearbyInteractables.Add(interactable);
                }
                // 하나의 오브젝트에 여러 IInteractable이 있더라도 모두 추가됩니다.
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var comps = other.GetComponents<MonoBehaviour>();
        foreach (var c in comps)
        {
            if (c is IInteractable interactable)
            {
                nearbyInteractables.Remove(interactable);
            }
        }
    }

    // 외부에서 강제로 대상 추가/제거가 필요할 때 호출 가능
    public void AddInteractable(IInteractable interactable)
    {
        if (interactable != null && !nearbyInteractables.Contains(interactable))
            nearbyInteractables.Add(interactable);
    }

    public void RemoveInteractable(IInteractable interactable)
    {
        if (interactable != null)
            nearbyInteractables.Remove(interactable);
    }
}
