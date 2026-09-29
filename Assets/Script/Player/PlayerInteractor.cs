using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public float interactionDistance = 3.0f; // 상호작용 가능 거리

    void Update()
    {
        // E 키를 눌렀을 때 정면에 있는 물체와 상호작용
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    void TryInteract()
    {
        // 카메라 중앙에서 정면으로 레이(빛)를 쏩니다.
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            // 부딪힌 물체에 IInteractable 인터페이스가 있는지 확인
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // 아이템이면 획득, 문이면 열림 등 해당 오브젝트의 Interact()가 알아서 실행됨!
                interactable.Interact();
            }
        }
    }
}