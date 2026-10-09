using System;
using System.Collections.Generic;
using ShiftingPyramid.Maze.Core;
using UnityEngine;

namespace ShiftingPyramid.Maze.View
{
    /// <summary>중앙 바닥 피벗의 3x3 방 루트. 내부 벽/문은 제작자가 구성한다.</summary>
    [DisallowMultipleComponent]
    public class MazeRoomView : MonoBehaviour
    {
        [Tooltip("방을 제작할 때 기준으로 사용한 일반 타일 간격. 10이면 방 크기는 30x30.")]
        [SerializeField, Min(0.01f)] private float authoredTileSize = 10f;

        public float AuthoredTileSize => authoredTileSize;
        public MazeRoom Room { get; private set; }

        public void Initialize(MazeRoom room)
        {
            Room = room;
        }

        // 프리팹 정보를 순수 좌표 데이터로 바꿔 계산 계층에 전달한다.
        public MazeRoomTemplate CreateTemplate(MazeTileType tileType, float tileSize)
        {
            if (tileSize <= 0f || !Mathf.Approximately(authoredTileSize, tileSize))
                throw new InvalidOperationException($"Room '{name}': Authored Tile Size must match renderer Tile Size ({tileSize}).");
            if ((transform.localScale - Vector3.one).sqrMagnitude > 0.0001f
                || Quaternion.Angle(transform.localRotation, Quaternion.identity) > 0.01f)
                throw new InvalidOperationException($"Room '{name}': prefab root scale must be (1,1,1) and authored rotation must be zero.");
            if (GetComponentsInChildren<MazeTileView>(true).Length != 0)
                throw new InvalidOperationException($"Room '{name}': remove the old MazeTileView components from the 3x3 room.");

            var entrances = new List<MazeRoomEntrance>();
            foreach (var marker in GetComponentsInChildren<MazeRoomEntranceMarker>(true))
            {
                // 프리팹 자산의 부모는 activeInHierarchy=false일 수 있다.
                // 비활성 부모도 탐색해야 정상 루트를 중첩 방으로 오판하지 않는다.
                var owner = marker.GetComponentInParent<MazeRoomView>(true);
                if (owner == null)
                    throw new InvalidOperationException($"Room '{name}': entrance '{marker.name}' has no owning MazeRoomView.");
                if (owner != this)
                    throw new InvalidOperationException($"Room '{name}': nested MazeRoomView is not supported.");
                var entrance = marker.GetEntrance();
                var actual = transform.InverseTransformPoint(marker.transform.position);
                var expected = marker.GetExpectedLocalPosition(tileSize);
                if (Mathf.Abs(actual.x - expected.x) > tileSize * 0.01f
                    || Mathf.Abs(actual.z - expected.z) > tileSize * 0.01f)
                    throw new InvalidOperationException($"Room '{name}': entrance '{marker.name}' is off its grid boundary. Use Align Marker To Grid and align the doorway with it.");
                entrances.Add(entrance);
            }
            return new MazeRoomTemplate(tileType, entrances);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero,
                new Vector3(MazeRoom.Size * authoredTileSize, 0.1f, MazeRoom.Size * authoredTileSize));
        }
    }
}
