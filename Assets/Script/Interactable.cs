using UnityEngine;

// 상호작용 가능한 객체가 구현해야 하는 인터페이스입니다.
// 사용법:
// - 상호작용 가능한 오브젝트의 MonoBehaviour에서 이 인터페이스를 구현하고
//   public void Interact() 메서드에 동작을 작성하세요.
// 예: Door, Chest, NPC 등
public interface IInteractable
{
    // 플레이어가 상호작용 버튼을 눌렀을 때 호출됩니다.
    void Interact();
}
