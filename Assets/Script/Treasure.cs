using UnityEngine;

// Treasure 스크립트는 보물 프리팹에 붙입니다.
// 작동 개요:
// - 프리팹에 Collider를 추가하고 "Is Trigger"는 끕니다(비활성화).
//   이렇게 하면 보물이 물체로 동작하여 플레이어가 통과하지 못하게 합니다.
// - 플레이어 오브젝트에 태그 "Player"를 설정합니다 (Inspector > Tag > Player).
// - 플레이어는 CharacterController를 사용하므로 보물은 일반 Collider(비트리거)로 두면 충돌하여 막습니다.
// - 상호작용은 G키(또는 PlayerInteractor의 전방 검사)를 통해 Interact()로 수행하세요.
public class Treasure : MonoBehaviour, IInteractable
{
    // 재진입 방지: 플레이어가 동시에 여러 번 충돌해 중복 획득되는 것을 막음
    private bool isCollected = false;

    // 실제 수집 처리
    private void Collect()
    {
        if (isCollected) return; // 중복 방지
        isCollected = true;

        if (GameManager.Instance != null)
        {
            // 요구사항: 파라미터 없는 AddTreasure() 호출
            GameManager.Instance.AddTreasure();
        }
        else
        {
            Debug.LogWarning("[Treasure] GameManager 인스턴스를 찾을 수 없습니다. 보물 카운트가 증가하지 않습니다.");
        }

        // 맵에서 보물 제거
        Destroy(gameObject);
    }

    // 인터페이스 구현: PlayerInteractor가 G키로 호출합니다.
    public void Interact()
    {
        Collect();
    }
}
