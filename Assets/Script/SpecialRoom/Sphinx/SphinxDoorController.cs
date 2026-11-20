using System.Collections;
using UnityEngine;

public class SphinxDoorController : MonoBehaviour
{
    [Header("문 연결")]
    [SerializeField] private Transform doorVisual;
    [SerializeField] private BoxCollider passageBlocker;

    [Header("Door 기준 로컬 위치")]
    [SerializeField] private Vector3 openPosition = new Vector3(0f, 14.75f, 0f);
    [SerializeField] private Vector3 closedPosition = new Vector3(0f, 4.75f, 0f);

    [Header("이동 시간")]
    [SerializeField, Min(0.01f)] private float moveDuration = 2f;

    private Coroutine moveCoroutine;

    private void Awake()
    {
        doorVisual.localPosition = openPosition;
        passageBlocker.enabled = false;
    }

    public void Close()
    {
        // 내려오기 시작하는 순간 통행을 막는다.
        passageBlocker.enabled = true;
        StartMoving(closedPosition, false);
    }

    public void Open()
    {
        StartMoving(openPosition, true);
    }

    private void StartMoving(Vector3 targetPosition, bool releasePassage)
    {
        // 닫히는 중 퀴즈가 끝나도 현재 위치에서 다시 열린다.
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
        }

        moveCoroutine = StartCoroutine(
            MoveDoor(targetPosition, releasePassage));
    }

    private IEnumerator MoveDoor(Vector3 targetPosition, bool releasePassage)
    {
        Vector3 startPosition = doorVisual.localPosition;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, moveDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            doorVisual.localPosition = Vector3.Lerp(
                startPosition, targetPosition, progress);

            yield return null;
        }

        doorVisual.localPosition = targetPosition;

        // 문이 완전히 올라간 다음 통행을 허용한다.
        if (releasePassage)
        {
            passageBlocker.enabled = false;
        }

        moveCoroutine = null;
    }
}