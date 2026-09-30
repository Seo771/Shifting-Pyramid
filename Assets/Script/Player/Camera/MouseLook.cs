using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("마우스 감도")]
    public float mouseSensitivity = 100f;

    [Header("연결할 플레이어 몸통 Transform")]
    public Transform playerBody;

    private float xRotation = 0f; // 카메라 상하 회전 값 누적용

    void Start()
    {
        // 마우스 커서를 화면 중앙에 고정하고 숨김 처리
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 마우스 입력값 받아오기
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 상하 회전 (X축 회전) 누적 및 각도 제한 (-90도 ~ 90도)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // 1. 카메라 상하 회전 적용
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 2. 플레이어 몸통 좌우 회전 적용 (Y축 회전)
        playerBody.Rotate(Vector3.up * mouseX);
    }
}