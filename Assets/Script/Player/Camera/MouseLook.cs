using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("마우스 감도")]
    public float mouseSensitivity = 200f;

    [Header("연결할 플레이어 몸통 Transform")]
    public Transform playerBody;

    private float xRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ★ Update 대신 LateUpdate를 사용하여 플레이어 이동이 완전히 끝난 후 카메라 갱신
    void LateUpdate()
    {
        if (playerBody == null) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // 1. 카메라 상하 회전
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 2. 플레이어 좌우 회전
        playerBody.Rotate(Vector3.up * mouseX);
    }
}