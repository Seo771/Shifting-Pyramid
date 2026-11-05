using System;
using System.Collections.Generic;
using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.View;
using UnityEngine;

namespace ShiftingPyramid.Game.Torch
{
    /// <summary>일반 길목의 닫힌 벽에 적은 수의 횃불을 배치한다.</summary>
    [DisallowMultipleComponent]
    public sealed class MazeTorchPlacementController : MonoBehaviour
    {
        [SerializeField] private MazeTestBootstrapper bootstrapper;
        [SerializeField] private GameObject torchPrefab;
        [SerializeField, Min(0)] private int maximumTorches = 15;
        [SerializeField, Min(1)] private int minimumTileDistance = 3;
        [SerializeField, Min(0f)] private float wallInset = 0.5f;
        [SerializeField, Min(0f)] private float mountHeight = 7f;
        [SerializeField, Min(0.1f)] private float torchScale = 2.5f;
        [SerializeField, Min(0f)] private float boxClearance = 2f;
        [SerializeField, Min(0.1f)] private float lightRange = 16f;

        private readonly List<MazeCoordinate> occupiedCells = new List<MazeCoordinate>();
        private MazeRenderer subscribedRenderer;
        private MazeGrid activeGrid;
        private Transform torchRoot;
        private int changeCount;
        private bool needsRebuild;

        private readonly struct WallCandidate
        {
            public readonly MazeCoordinate Cell;
            public readonly MazeDirection Wall;

            public WallCandidate(MazeCoordinate cell, MazeDirection wall)
            {
                Cell = cell;
                Wall = wall;
            }
        }

        private void OnEnable()
        {
            if (bootstrapper == null) bootstrapper = FindFirstObjectByType<MazeTestBootstrapper>();
            if (bootstrapper == null) return;
            bootstrapper.MazeReady += HandleMazeReady;
            SubscribeRenderer();
            if (bootstrapper.IsMazeReady && bootstrapper.CurrentGrid != null)
                HandleMazeReady(bootstrapper.CurrentGrid);
        }

        private void OnDisable()
        {
            if (bootstrapper != null) bootstrapper.MazeReady -= HandleMazeReady;
            if (subscribedRenderer != null) subscribedRenderer.GeometryChanged -= HandleGeometryChanged;
            subscribedRenderer = null;
            ClearTorches();
        }

        private void HandleMazeReady(MazeGrid grid)
        {
            activeGrid = grid;
            changeCount = 0;
            SubscribeRenderer();
            needsRebuild = grid != null;
        }

        private void HandleGeometryChanged()
        {
            // 초기 Build 신호는 MazeReady보다 먼저 오므로 사용하지 않는다.
            if (bootstrapper == null || !bootstrapper.IsMazeReady
                || !ReferenceEquals(activeGrid, bootstrapper.CurrentGrid))
            {
                if (torchRoot != null) torchRoot.gameObject.SetActive(false);
                return;
            }
            changeCount++;
            needsRebuild = true;
        }

        private void LateUpdate()
        {
            if (!needsRebuild) return;
            needsRebuild = false;
            if (bootstrapper == null || !bootstrapper.IsMazeReady
                || !ReferenceEquals(activeGrid, bootstrapper.CurrentGrid)) return;
            // BOX의 GeometryChanged 처리와 플레이어 배치가 끝난 뒤 검사한다.
            RebuildTorches();
        }

        private void RebuildTorches()
        {
            ClearTorches();
            var renderer = bootstrapper.Renderer;
            if (activeGrid == null || renderer == null || torchPrefab == null || maximumTorches == 0) return;

            var candidates = new List<WallCandidate>();
            foreach (var tile in activeGrid.Tiles)
            {
                if (tile.TileType != MazeTileType.Normal) continue;
                for (var direction = MazeDirection.North; direction <= MazeDirection.West; direction++)
                {
                    if (tile.IsOpen(direction)) continue;
                    var neighbor = tile.Coordinate + direction.ToOffset();
                    if (activeGrid.Contains(neighbor))
                    {
                        // 한 벽을 두 타일에서 중복 설치하지 않는다.
                        if (neighbor.X < tile.Coordinate.X || neighbor.Y < tile.Coordinate.Y) continue;
                        if (activeGrid.GetTile(neighbor).TileType != MazeTileType.Normal) continue;
                    }
                    candidates.Add(new WallCandidate(tile.Coordinate, direction));
                }
            }

            var random = new System.Random(unchecked(bootstrapper.CurrentSeed + changeCount * 104729));
            for (var i = candidates.Count - 1; i > 0; i--)
            {
                var next = random.Next(i + 1);
                (candidates[i], candidates[next]) = (candidates[next], candidates[i]);
            }

            Physics.SyncTransforms();
            torchRoot = new GameObject("GeneratedTorches").transform;
            torchRoot.SetParent(transform, false);
            foreach (var candidate in candidates)
            {
                if (occupiedCells.Count >= maximumTorches) break;
                if (!IsFarEnough(candidate.Cell)
                    || !renderer.TryGetTileWorldPosition(candidate.Cell, out var center)
                    || !TryWorldDirection(renderer, candidate.Cell, candidate.Wall,
                        center, out var outward)) continue;

                var position = center + outward * (renderer.TileSize * 0.5f - wallInset)
                    + Vector3.up * mountHeight;
                if (TouchesBox(position)) continue;
                var instance = Instantiate(torchPrefab, position,
                    Quaternion.LookRotation(outward, Vector3.up), torchRoot);
                instance.name = $"Torch_{candidate.Cell.X}_{candidate.Cell.Y}_{candidate.Wall}";
                instance.transform.localScale = Vector3.one * torchScale;
                if (instance.TryGetComponent(out TorchLight torchLight)) torchLight.lightRange = lightRange;
                occupiedCells.Add(candidate.Cell);
            }
        }

        private bool IsFarEnough(MazeCoordinate cell)
        {
            foreach (var occupied in occupiedCells)
                if (Math.Abs(occupied.X - cell.X) + Math.Abs(occupied.Y - cell.Y)
                    < minimumTileDistance) return false;
            return true;
        }

        private bool TouchesBox(Vector3 position)
        {
            if (boxClearance <= 0f) return false;
            foreach (var collider in Physics.OverlapSphere(position, boxClearance,
                ~0, QueryTriggerInteraction.Ignore))
                if (collider.GetComponentInParent<RandomBox>() != null) return true;
            return false;
        }

        private static bool TryWorldDirection(MazeRenderer renderer, MazeCoordinate cell,
            MazeDirection direction, Vector3 center, out Vector3 outward)
        {
            outward = default;
            if (renderer.TryGetTileWorldPosition(cell + direction.ToOffset(), out var next))
                outward = next - center;
            else if (renderer.TryGetTileWorldPosition(cell + direction.Opposite().ToOffset(), out var previous))
                outward = center - previous;
            outward.y = 0f;
            if (outward.sqrMagnitude < 0.001f) return false;
            outward.Normalize();
            return true;
        }

        private void SubscribeRenderer()
        {
            var renderer = bootstrapper != null ? bootstrapper.Renderer : null;
            if (ReferenceEquals(subscribedRenderer, renderer)) return;
            if (subscribedRenderer != null) subscribedRenderer.GeometryChanged -= HandleGeometryChanged;
            subscribedRenderer = renderer;
            if (subscribedRenderer != null) subscribedRenderer.GeometryChanged += HandleGeometryChanged;
        }

        private void ClearTorches()
        {
            occupiedCells.Clear();
            if (torchRoot == null) return;
            torchRoot.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(torchRoot.gameObject);
            else DestroyImmediate(torchRoot.gameObject);
            torchRoot = null;
        }
    }
}
