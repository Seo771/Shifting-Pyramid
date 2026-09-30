using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.View;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerMazeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform existingPlayer;
    [SerializeField, Min(0f)] private float spawnHeight = 1.25f;

    private Transform spawnedPlayer;

    // 미로가 만들어진 뒤 시작 칸의 실제 월드 위치에 플레이어를 배치한다.
    public void SpawnAtStart(MazeRenderer mazeRenderer)
    {
        if (mazeRenderer == null
            || !mazeRenderer.TryGetTileWorldPosition(MazeCoordinate.Zero, out var tilePosition))
        {
            Debug.LogWarning("PlayerMazeSpawner failed: start tile is not available.", this);
            return;
        }

        var spawnPosition = tilePosition + Vector3.up * spawnHeight;

        // 씬에 플레이어가 있으면 재사용해 중복 생성을 막는다.
        if (existingPlayer != null)
        {
            MovePlayer(existingPlayer, spawnPosition);
            return;
        }

        if (spawnedPlayer == null)
        {
            if (playerPrefab == null)
            {
                Debug.LogWarning("PlayerMazeSpawner failed: player prefab is not assigned.", this);
                return;
            }

            spawnedPlayer = Instantiate(playerPrefab, spawnPosition, playerPrefab.transform.rotation).transform;
            return;
        }

        MovePlayer(spawnedPlayer, spawnPosition);
    }

    private static void MovePlayer(Transform player, Vector3 position)
    {
        var controller = player.GetComponent<CharacterController>();
        var wasEnabled = controller != null && controller.enabled;

        if (wasEnabled)
        {
            controller.enabled = false;
        }

        player.position = position;

        if (wasEnabled)
        {
            controller.enabled = true;
        }
    }
}
