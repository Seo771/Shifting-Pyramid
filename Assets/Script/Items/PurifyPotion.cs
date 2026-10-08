using UnityEngine;

// 미라화 수치를 낮추는 소모형 아이템
public class PurifyPotion : MonoBehaviour
{
    [Header("정화량 설정")]
    [Tooltip("사용 시 감소시킬 미라화 수치 (절대값)")]
    public float reduceAmount = 10f;

    // 즉시 사용하여 플레이어의 미라화 수치를 감소시킵니다.
    // user 파라미터로 플레이어 GameObject를 전달하거나 null을 전달하면 씬에서 Mummification을 찾아 적용합니다.
    public void Use(GameObject user = null)
    {
        Mummification target = null;

        if (user != null)
        {
            target = user.GetComponent<Mummification>();
        }

        if (target == null)
        {
            target = Object.FindFirstObjectByType<Mummification>();
        }

        if (target == null)
        {
            Debug.LogWarning("[PurifyPotion] Mummification 컴포넌트를 찾을 수 없습니다. 정화 실패.");
            return;
        }

        target.DecreaseMummification(reduceAmount);

        // 소모형 아이템이므로 사용 후 자신의 인스턴스를 제거
        Destroy(gameObject);
    }
}
