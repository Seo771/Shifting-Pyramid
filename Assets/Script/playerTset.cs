using UnityEngine;

public class TempPlayer : MonoBehaviour
{
    public float speed = 5f; // 이동 속도

    void Update()
    {
        // 키보드 방향키 또는 WASD 입력 받기
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 입력받은 방향으로 원기둥 이동시키기
        Vector3 moveDirection = new Vector3(h, 0, v);
        transform.Translate(moveDirection * speed * Time.deltaTime);
    }
}