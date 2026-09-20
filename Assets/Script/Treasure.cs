using UnityEngine;

// Treasure 스크립트는 보물 프리팹에 붙입니다.
// 작동 개요:
// - 프리팹에 Collider(또는 Collider2D)를 추가하고 "Is Trigger"를 체크합니다.
// - 플레이어 오브젝트에 태그 "Player"를 설정합니다 (Inspector > Tag > Player).
// - 플레이어 또는 보물 중 적어도 한쪽에 Rigidbody 또는 Rigidbody2D가 있어야 트리거 이벤트가 발생합니다.
// - 플레이어가 충돌하면 GameManager.Instance.AddTreasure()를 호출하고 보물을 삭제합니다.
public class Treasure : MonoBehaviour
{
    // 재진입 방지: 플레이어가 동시에 여러 번 충돌해 중복 획득되는 것을 막음
    private bool isCollected = false;

    // 3D 물리 이벤트(일반 물리 엔진)를 처리
    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;
        // 충돌한 오브젝트의 태그가 "Player"인지 안전하게 비교
        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }

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

    // 에디터 세팅 팁 (요약):
    // 1) 보물 프리팹: Collider 또는 Collider2D 컴포넌트 추가 후 Is Trigger 체크
    // 2) 플레이어: Tag를 "Player"로 설정
    // 3) Rigidbody 또는 Rigidbody2D: 플레이어나 보물 중 적어도 하나에 추가(정적 트리거의 경우에는 플레이어에 추가하는 것이 일반적)
}
