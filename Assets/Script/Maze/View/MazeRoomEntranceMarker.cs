using ShiftingPyramid.Maze.Core;
using UnityEngine;

namespace ShiftingPyramid.Maze.View
{
    /// <summary>직접 만든 문 앞에 붙이는 격자 연결 표시점.</summary>
    [DisallowMultipleComponent]
    public class MazeRoomEntranceMarker : MonoBehaviour
    {
        [Tooltip("방 남서쪽=(0,0), 북동쪽=(2,2). 북쪽 중앙 문은 (1,2).")]
        [SerializeField] private Vector2Int localCell = new Vector2Int(1, 2);
        [SerializeField] private MazeDirection direction = MazeDirection.North;

        public MazeRoomEntrance GetEntrance()
        {
            return new MazeRoomEntrance(new MazeCoordinate(localCell.x, localCell.y), direction);
        }

        public Vector3 GetExpectedLocalPosition(float tileSize)
        {
            var entrance = GetEntrance();
            var offset = entrance.Direction.ToOffset();
            return new Vector3((localCell.x - 1 + offset.X * 0.5f) * tileSize,
                0f, (localCell.y - 1 + offset.Y * 0.5f) * tileSize);
        }

        [ContextMenu("Align Marker To Grid")]
        private void AlignMarkerToGrid()
        {
            var room = GetComponentInParent<MazeRoomView>(true);
            if (room == null)
            {
                Debug.LogWarning("Add MazeRoomView to the room root first.", this);
                return;
            }
            var position = GetExpectedLocalPosition(room.AuthoredTileSize);
            position.y = room.transform.InverseTransformPoint(transform.position).y;
            transform.position = room.transform.TransformPoint(position);
        }

        private void OnDrawGizmosSelected()
        {
            var room = GetComponentInParent<MazeRoomView>(true);
            if (room == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position, 0.2f);
            var offset = direction.ToOffset();
            Gizmos.DrawRay(transform.position,
                room.transform.TransformDirection(new Vector3(offset.X, 0f, offset.Y)) * 2f);
        }
    }
}
