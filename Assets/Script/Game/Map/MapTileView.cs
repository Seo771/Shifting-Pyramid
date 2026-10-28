using ShiftingPyramid.Maze.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ShiftingPyramid.Game.Map
{
    /// <summary>공개된 한 칸의 바닥과 확인 가능한 외곽 벽을 표시한다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class MapTileView : MonoBehaviour
    {
        [SerializeField] private Image floor;
        [SerializeField] private Image northWall;
        [SerializeField] private Image eastWall;
        [SerializeField] private Image southWall;
        [SerializeField] private Image westWall;

        [Header("Style")]
        [SerializeField] private Color normalColor = new Color(0.67f, 0.57f, 0.39f);
        [SerializeField] private Color curseRoomColor = new Color(0.59f, 0.35f, 0.62f);
        [SerializeField] private Color specialRoomColor = new Color(0.32f, 0.55f, 0.62f);
        [SerializeField] private Color exitColor = new Color(0.78f, 0.62f, 0.29f);
        [SerializeField] private Color wallColor = new Color(0.13f, 0.10f, 0.08f);
        [SerializeField, Min(1f)] private float wallThickness = 3f;

        public void Show(MapTileSignature signature, MazeCoordinate coordinate, float cellSize)
        {
            if (floor == null || northWall == null || eastWall == null
                || southWall == null || westWall == null)
            {
                Debug.LogError("MapTileView: assign floor and four wall Images.", this);
                gameObject.SetActive(false);
                return;
            }

            floor.color = signature.TileType switch
            {
                MazeTileType.CurseJarRoom => curseRoomColor,
                MazeTileType.SpecialRoom => specialRoomColor,
                MazeTileType.Exit => exitColor,
                _ => normalColor
            };

            var thickness = Mathf.Min(wallThickness, cellSize * 0.5f);
            ApplyWall(northWall, ShouldShowWall(signature, coordinate, MazeDirection.North),
                new Vector2(0f, cellSize * 0.5f), new Vector2(cellSize, thickness));
            ApplyWall(eastWall, ShouldShowWall(signature, coordinate, MazeDirection.East),
                new Vector2(cellSize * 0.5f, 0f), new Vector2(thickness, cellSize));
            ApplyWall(southWall, ShouldShowWall(signature, coordinate, MazeDirection.South),
                new Vector2(0f, -cellSize * 0.5f), new Vector2(cellSize, thickness));
            ApplyWall(westWall, ShouldShowWall(signature, coordinate, MazeDirection.West),
                new Vector2(-cellSize * 0.5f, 0f), new Vector2(thickness, cellSize));
        }

        private static bool ShouldShowWall(MapTileSignature signature, MazeCoordinate coordinate,
            MazeDirection direction)
        {
            if (signature.HasRoom)
            {
                var neighbor = coordinate + direction.ToOffset();
                if (neighbor.X >= signature.RoomOrigin.X && neighbor.X < signature.RoomOrigin.X + MazeRoom.Size
                    && neighbor.Y >= signature.RoomOrigin.Y && neighbor.Y < signature.RoomOrigin.Y + MazeRoom.Size)
                    return false; // 프리팹 내부 벽은 논리 데이터로 확정할 수 없다.
            }
            return !signature.IsOpen(direction);
        }

        private void ApplyWall(Image wall, bool visible, Vector2 position, Vector2 size)
        {
            wall.gameObject.SetActive(visible);
            if (!visible) return;
            wall.color = wallColor;
            var rect = wall.rectTransform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
