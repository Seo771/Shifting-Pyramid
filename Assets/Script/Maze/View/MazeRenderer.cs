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
        private readonly Dictionary<MazeRoom, MazeRoomView> roomViews = new Dictionary<MazeRoom, MazeRoomView>();

        public float TileSize => tileSize;
        public event Action GeometryChanged;

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

        public bool TryGetCoordinate(Vector3 worldPosition, MazeGrid grid, out MazeCoordinate coordinate)
        {
            coordinate = default;
            if (grid == null || tileSize <= 0f) return false;

            var local = GetTileRoot().InverseTransformPoint(worldPosition);
            var x = centerOnOrigin ? local.x + (grid.Width - 1) * tileSize * 0.5f : local.x;
            var z = centerOnOrigin ? local.z + (grid.Height - 1) * tileSize * 0.5f : local.z;
            coordinate = new MazeCoordinate(Mathf.RoundToInt(x / tileSize), Mathf.RoundToInt(z / tileSize));
            return grid.Contains(coordinate);
        }

        // 타일 사이 이동량까지 유지해 UI가 끊기지 않게 움직인다.
        public bool TryGetContinuousGridPosition(Vector3 worldPosition, MazeGrid grid, out Vector2 position)
        {
            position = default;
            if (grid == null || tileSize <= 0f) return false;
            var local = GetTileRoot().InverseTransformPoint(worldPosition);
            position = new Vector2(
                local.x / tileSize + (centerOnOrigin ? (grid.Width - 1) * 0.5f : 0f),
                local.z / tileSize + (centerOnOrigin ? (grid.Height - 1) * 0.5f : 0f));
            return true;
        }

        public Vector2 GetGridDirection(Vector3 worldDirection)
        {
            var local = GetTileRoot().InverseTransformDirection(worldDirection);
            return new Vector2(local.x, local.z);
        }

        public void ValidateConfiguration()
        {
            if (tilePrefab == null)
                throw new InvalidOperationException("MazeRenderer: assign the ordinary Tile Prefab.");
            if (tileSize <= 0f)
                throw new InvalidOperationException("MazeRenderer: Tile Size must be positive.");
        }

        // roomPrefabs는 방 종류 카탈로그이며 Room.TemplateIndex와 대응한다.
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
                AddTileView(tile, prefab);
            }

            foreach (var room in grid.Rooms)
            {
                AddRoomView(room, roomPrefabs[room.TemplateIndex]);
            }

            GeometryChanged?.Invoke();
        }

        // 방 내부와 문은 프리팹이 관리한다. 여기서는 일반 통로 벽만 갱신한다.
        public void Refresh(MazeGrid grid)
        {
            if (grid == null) return;
            foreach (var tile in grid.Tiles)
                if (tileViews.TryGetValue(tile.Coordinate, out var view) && view != null)
                    view.Apply(tile);

            GeometryChanged?.Invoke();
        }

        // 변경된 두 방 영역만 교체하고, GeometryChanged는 뒤따르는 Refresh에서 한 번 알린다.
        public void ApplyRoomReplacement(MazeGrid grid, MazeRoom removed, MazeRoom added,
            IReadOnlyList<MazeRoomView> roomPrefabs)
        {
            if (grid == null || removed == null || added == null)
                throw new ArgumentNullException(nameof(grid), "Room replacement needs a grid and both rooms.");
            if (roomPrefabs == null || added.TemplateIndex < 0
                || added.TemplateIndex >= roomPrefabs.Count || roomPrefabs[added.TemplateIndex] == null)
                throw new InvalidOperationException("MazeRenderer: replacement room has no matching prefab.");
            if (!roomViews.TryGetValue(removed, out var oldView))
                throw new InvalidOperationException("MazeRenderer: the removed room view was not found.");
            foreach (var cell in removed.Cells)
                if (!tilePositions.ContainsKey(cell) || tileViews.ContainsKey(cell))
                    throw new InvalidOperationException("MazeRenderer: removed room footprint is inconsistent.");
            foreach (var cell in added.Cells)
                if (!tilePositions.ContainsKey(cell) || !tileViews.ContainsKey(cell))
                    throw new InvalidOperationException("MazeRenderer: new room footprint is inconsistent.");

            roomViews.Remove(removed);
            RemoveGeneratedObject(oldView.gameObject);
            foreach (var cell in removed.Cells)
                AddTileView(grid.GetTile(cell), tilePrefab);

            foreach (var cell in added.Cells)
            {
                var view = tileViews[cell];
                tileViews.Remove(cell);
                RemoveGeneratedObject(view.gameObject);
            }
            AddRoomView(added, roomPrefabs[added.TemplateIndex]);
        }

        public void Clear()
        {
            foreach (var view in tileViews.Values)
                if (view != null) RemoveGeneratedObject(view.gameObject);
            foreach (var view in roomViews.Values)
                if (view != null) RemoveGeneratedObject(view.gameObject);

            tileViews.Clear();
            tilePositions.Clear();
            roomViews.Clear();
        }

        private void AddTileView(MazeTile tile, MazeTileView prefab)
        {
            var view = Instantiate(prefab, GetTileRoot());
            view.transform.localPosition = tilePositions[tile.Coordinate];
            view.name = $"MazeTile_{tile.Coordinate.X}_{tile.Coordinate.Y}";
            view.Apply(tile);
            tileViews.Add(tile.Coordinate, view);
        }

        private void AddRoomView(MazeRoom room, MazeRoomView prefab)
        {
            var view = Instantiate(prefab, GetTileRoot());
            view.transform.localPosition = tilePositions[room.Center];
            view.transform.localRotation = Quaternion.Euler(0f, room.RotationQuarterTurns * 90f, 0f);
            view.name = $"MazeRoom_{room.Template.TileType}_{room.Origin.X}_{room.Origin.Y}";
            view.Initialize(room);
            roomViews.Add(room, view);
        }

        private void RemoveGeneratedObject(GameObject generated)
        {
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
