using System;
using System.Collections.Generic;
using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.Settings;
using Unity.AI.Navigation; // ★ 추가 1: NavMeshSurface 사용을 위한 네임스페이스
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

        [Header("NavMesh")]
        [SerializeField] private NavMeshSurface navMeshSurface; // ★ 추가 2: NavMeshSurface 참조 변수

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
                view.transform.localRotation = Quaternion.Euler(0f, room.RotationQuarterTurns * 90f, 0f);
                view.name = $"MazeRoom_{room.Template.TileType}_{room.Origin.X}_{room.Origin.Y}";
                view.Initialize(room);
                roomObjects.Add(view.gameObject);
            }

            // ★ 추가 3: 미로 생성이 완전히 완료된 직후 런타임 Bake 실행
            RebuildNavMesh();
        }

        // 방 내부와 문은 프리팹이 관리한다. 여기서는 일반 통로 벽만 갱신한다.
        public void Refresh(MazeGrid grid)
        {
            if (grid == null) return;
            foreach (var tile in grid.Tiles)
                if (tileViews.TryGetValue(tile.Coordinate, out var view) && view != null)
                    view.Apply(tile);

            // ★ 추가 3: 미로 지형/통로가 변경된 후에도 Bake 재실행
            RebuildNavMesh();
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

        // ★ 추가: NavMesh 재구성 헬퍼 메서드
        public void RebuildNavMesh()
        {
            // 1. 변환된 navMeshSurface가 비어있다면, 현재 오브젝트나 자식, 혹은 씬 전체에서 검색
            if (navMeshSurface == null)
            {
                navMeshSurface = GetComponentInChildren<NavMeshSurface>();
                if (navMeshSurface == null)
                {
                    navMeshSurface = FindFirstObjectByType<NavMeshSurface>();
                }
            }

            // 2. 찾았다면 바로 빌드 실행
            if (navMeshSurface != null)
            {
                navMeshSurface.BuildNavMesh();
                Debug.Log("[MazeRenderer] 실시간 미로 NavMesh 재구성 완료!");
            }
            else
            {
                Debug.LogWarning("[MazeRenderer] NavMeshSurface를 찾을 수 없어 재구성을 스킵합니다.");
            }
        }
    }
}