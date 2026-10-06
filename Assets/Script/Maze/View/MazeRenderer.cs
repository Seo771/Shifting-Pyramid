using System;
using System.Collections.Generic;
using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.Settings;
using UnityEngine;

namespace ShiftingPyramid.Maze.View
{
    /// <summary>일반 타일과 점유 영역당 하나의 3x3 방 프리팹을 배치한다.</summary>
    [DisallowMultipleComponent]
    public class MazeRenderer : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private MazeTileView tilePrefab;
        [SerializeField] private Transform tileRoot;

        [Header("Layout")]
        [SerializeField, Min(0.01f)] private float tileSize = 10f;
        [SerializeField] private bool centerOnOrigin = true;

        private readonly Dictionary<MazeCoordinate, MazeTileView> tileViews = new Dictionary<MazeCoordinate, MazeTileView>();
        private readonly Dictionary<MazeCoordinate, Vector3> tilePositions = new Dictionary<MazeCoordinate, Vector3>();
        private readonly List<GameObject> roomObjects = new List<GameObject>();

        public float TileSize => tileSize;

        public bool TryGetTileWorldPosition(MazeCoordinate coordinate, out Vector3 worldPosition)
        {
            if (tilePositions.TryGetValue(coordinate, out var localPosition))
            {
                worldPosition = GetTileRoot().TransformPoint(localPosition);
                return true;
            }
            worldPosition = default;
            return false;
        }

        public void ValidateConfiguration()
        {
            if (tilePrefab == null)
                throw new InvalidOperationException("MazeRenderer: assign the ordinary Tile Prefab.");
            if (tileSize <= 0f)
                throw new InvalidOperationException("MazeRenderer: Tile Size must be positive.");
        }

        // roomPrefabs는 생성 단계에서 선택한 목록이며 Room.TemplateIndex와 대응한다.
        public void Build(MazeGrid grid, MazeBalanceSettings settings, int? seed = null,
            IReadOnlyList<MazeRoomView> roomPrefabs = null)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            ValidateConfiguration();
            foreach (var room in grid.Rooms)
                if (roomPrefabs == null || room.TemplateIndex < 0
                    || room.TemplateIndex >= roomPrefabs.Count || roomPrefabs[room.TemplateIndex] == null)
                    throw new InvalidOperationException("MazeRenderer: a reserved room has no matching 3x3 prefab.");

            Clear();
            foreach (var tile in grid.Tiles)
            {
                tilePositions.Add(tile.Coordinate, GetLocalPosition(tile.Coordinate, grid.Width, grid.Height));
                if (grid.TryGetRoom(tile.Coordinate, out _)) continue;

                var prefab = tile.TileType == MazeTileType.Exit && settings != null && settings.ExitRoomPrefab != null
                    ? settings.ExitRoomPrefab : tilePrefab;
                var view = Instantiate(prefab, GetTileRoot());
                view.transform.localPosition = tilePositions[tile.Coordinate];
                view.name = $"MazeTile_{tile.Coordinate.X}_{tile.Coordinate.Y}";
                view.Apply(tile);
                tileViews.Add(tile.Coordinate, view);
            }

            foreach (var room in grid.Rooms)
            {
                var view = Instantiate(roomPrefabs[room.TemplateIndex], GetTileRoot());
                view.transform.localPosition = GetLocalPosition(room.Center, grid.Width, grid.Height);
                view.transform.localRotation = Quaternion.identity;
                view.name = $"MazeRoom_{room.Template.TileType}_{room.Origin.X}_{room.Origin.Y}";
                view.Initialize(room);
                roomObjects.Add(view.gameObject);
            }
        }

        // 방 내부와 문은 프리팹이 관리한다. 여기서는 일반 통로 벽만 갱신한다.
        public void Refresh(MazeGrid grid)
        {
            if (grid == null) return;
            foreach (var tile in grid.Tiles)
                if (tileViews.TryGetValue(tile.Coordinate, out var view) && view != null)
                    view.Apply(tile);
        }

        public void Clear()
        {
            foreach (var view in tileViews.Values)
                if (view != null) RemoveGeneratedObject(view.gameObject);
            foreach (var room in roomObjects)
                if (room != null) RemoveGeneratedObject(room);

            tileViews.Clear();
            tilePositions.Clear();
            roomObjects.Clear();
        }

        private void RemoveGeneratedObject(GameObject generated)
        {
            // Destroy가 프레임 끝에 실행되더라도 이전 미로가 충돌하지 않게 한다.
            generated.SetActive(false);
            if (Application.isPlaying) Destroy(generated);
            else DestroyImmediate(generated);
        }

        private Transform GetTileRoot()
        {
            return tileRoot != null ? tileRoot : transform;
        }

        private Vector3 GetLocalPosition(MazeCoordinate coordinate, int width, int height)
        {
            var x = coordinate.X * tileSize;
            var z = coordinate.Y * tileSize;
            if (centerOnOrigin)
            {
                x -= (width - 1) * tileSize * 0.5f;
                z -= (height - 1) * tileSize * 0.5f;
            }
            return new Vector3(x, 0f, z);
        }
    }
}
