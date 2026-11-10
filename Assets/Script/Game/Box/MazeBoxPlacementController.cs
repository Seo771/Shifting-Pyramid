using System;
using System.Collections.Generic;
using ShiftingPyramid.Game.World;
using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.View;
using UnityEngine;

namespace ShiftingPyramid.Game.Box
{
    /// <summary>완성된 미로의 일반 길목에 게임용 BOX를 배치한다.</summary>
    [DisallowMultipleComponent]
    public sealed class MazeBoxPlacementController : MonoBehaviour
    {
        [SerializeField] private MazeTestBootstrapper bootstrapper;
        [SerializeField] private RandomBox boxPrefab;
        [SerializeField, Min(0)] private int targetCount = 6;
        [SerializeField, Min(0)] private int startExclusionDistance = 3;
        [SerializeField, Min(1)] private int minimumBoxDistance = 4;
        [SerializeField, Min(0f)] private float wallInset = 0.75f;
        [SerializeField, Min(0.5f)] private float minimumPassageWidth = 2f;
        [SerializeField] private LayerMask obstructionMask = ~0;

        private readonly List<PlacedBox> placed = new List<PlacedBox>();
        private MazeGrid activeGrid;
        private Transform boxRoot;
        private int changeCount;

        private sealed class PlacedBox
        {
            public MazeCoordinate Coordinate;
            public RandomBox Instance;
            public bool HasPlacement;
        }

        private void OnEnable()
        {
            if (bootstrapper == null) bootstrapper = FindFirstObjectByType<MazeTestBootstrapper>();
            if (bootstrapper == null) return;
            bootstrapper.Updates.ContentsUpdating += HandleUpdate;
            if (bootstrapper.IsMazeReady && bootstrapper.CurrentGrid != null)
            {
                if (ReferenceEquals(activeGrid, bootstrapper.CurrentGrid)) ReconcileBoxes(false);
                else HandleMazeReady(bootstrapper.CurrentGrid);
            }
            else if (boxRoot != null) boxRoot.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (bootstrapper != null) bootstrapper.Updates.ContentsUpdating -= HandleUpdate;
        }

        private void OnDestroy()
        {
            ClearBoxes();
        }

        private void HandleMazeReady(MazeGrid grid)
        {
            ClearBoxes();
            if (grid == null || boxPrefab == null || targetCount == 0 || bootstrapper.Renderer == null) return;
            if (!boxPrefab.TryGetComponent(out BoxCollider boxCollider))
            {
                Debug.LogWarning("BOX 프리팹 루트에 BoxCollider가 필요합니다.", this);
                return;
            }

            activeGrid = grid;
            changeCount = 0;
            boxRoot = new GameObject("GeneratedBoxes").transform;
            boxRoot.SetParent(transform, false);
            var renderer = bootstrapper.Renderer;
            var deadEnds = new List<MazeCoordinate>();
            var straightPaths = new List<MazeCoordinate>();
            foreach (var tile in grid.Tiles)
            {
                if (!IsEligible(grid, tile.Coordinate, out _, out var straight)) continue;
                if (straight) straightPaths.Add(tile.Coordinate);
                else deadEnds.Add(tile.Coordinate);
            }

            var random = new System.Random(unchecked(bootstrapper.CurrentSeed ^ 0x4B4F58));
            Shuffle(deadEnds, random);
            Shuffle(straightPaths, random);
            Physics.SyncTransforms();
            PlaceCandidates(deadEnds, grid, renderer, boxCollider, random);
            PlaceCandidates(straightPaths, grid, renderer, boxCollider, random);
            if (placed.Count < targetCount)
                Debug.Log($"BOX 후보 부족: {targetCount}개 중 {placed.Count}개 배치했습니다.", this);
        }

        private void PlaceCandidates(List<MazeCoordinate> candidates, MazeGrid grid,
            MazeRenderer renderer, BoxCollider boxCollider, System.Random random)
        {
            foreach (var coordinate in candidates)
            {
                if (placed.Count >= targetCount) return;
                var tooClose = false;
                foreach (var existing in placed)
                    if (Math.Abs(existing.Coordinate.X - coordinate.X)
                        + Math.Abs(existing.Coordinate.Y - coordinate.Y) < minimumBoxDistance)
                    {
                        tooClose = true;
                        break;
                    }
                if (tooClose) continue;
                if (!TryFindPose(grid, renderer, boxCollider, coordinate, random, null,
                    out var position, out var rotation)) continue;

                var instance = Instantiate(boxPrefab, position, rotation, boxRoot);
                instance.name = $"Box_{coordinate.X}_{coordinate.Y}";
                placed.Add(new PlacedBox
                {
                    Coordinate = coordinate, Instance = instance, HasPlacement = true
                });
                Physics.SyncTransforms();
            }
        }

        private bool IsEligible(MazeGrid grid, MazeCoordinate coordinate,
            out MazeDirection openDirection, out bool straight)
        {
            openDirection = MazeDirection.North;
            straight = false;
            var tile = grid.GetTile(coordinate);
            if (tile.TileType != MazeTileType.Normal || tile.IsProtected
                || grid.TryGetRoom(coordinate, out _)
                || coordinate.X + coordinate.Y <= startExclusionDistance) return false;

            var openCount = 0;
            var openings = 0;
            for (var direction = MazeDirection.North; direction <= MazeDirection.West; direction++)
            {
                var neighbor = coordinate + direction.ToOffset();
                if (grid.TryGetRoom(neighbor, out _)) return false;
                if (!tile.IsOpen(direction)) continue;
                openCount++;
                openDirection = direction;
                openings |= 1 << (int)direction;
            }
            straight = openings == 5 || openings == 10;
            return openCount == 1 || straight;
        }

        private bool TryFindPose(MazeGrid grid, MazeRenderer renderer, BoxCollider boxCollider,
            MazeCoordinate coordinate, System.Random random, Quaternion? preferredRotation,
            out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = default;
            if (!IsEligible(grid, coordinate, out var openDirection, out var straight)
                || !renderer.TryGetTileWorldPosition(coordinate, out var center)) return false;

            var firstWall = straight ? (openDirection == MazeDirection.North
                || openDirection == MazeDirection.South ? MazeDirection.East : MazeDirection.North)
                : openDirection.Opposite();
            var walls = straight
                ? new[] { firstWall, firstWall.Opposite() }
                : new[] { firstWall };
            if (walls.Length == 2)
            {
                var swap = random.Next(2) == 1;
                if (preferredRotation.HasValue
                    && TryWorldDirection(grid, renderer, coordinate, walls[0], center, out var outward))
                    swap = Vector3.Dot(preferredRotation.Value * Vector3.forward, -outward) < 0.5f;
                if (swap) (walls[0], walls[1]) = (walls[1], walls[0]);
            }

            var scale = boxPrefab.transform.localScale;
            var halfWidth = Mathf.Abs(boxCollider.size.x * scale.x) * 0.5f;
            var halfHeight = Mathf.Abs(boxCollider.size.y * scale.y) * 0.5f;
            var halfDepth = Mathf.Abs(boxCollider.size.z * scale.z) * 0.5f;
            var tileSize = renderer.TileSize;
            if (2f * halfWidth + 2f * wallInset > tileSize
                || tileSize - (2f * halfDepth + wallInset) < minimumPassageWidth) return false;

            foreach (var wall in walls)
            {
                if (!TryWorldDirection(grid, renderer, coordinate, wall, center, out var outward)) continue;
                var offset = tileSize * 0.5f - halfDepth - wallInset;
                var horizontal = center + outward * offset;
                var hits = Physics.RaycastAll(horizontal + Vector3.up * tileSize,
                    Vector3.down, tileSize * 2f, ~0, QueryTriggerInteraction.Ignore);
                foreach (var hit in hits)
                {
                    if (hit.collider.GetComponent<MazeFloor>() == null
                        || !renderer.TryGetCoordinate(hit.point, grid, out var floorCoordinate)
                        || !floorCoordinate.Equals(coordinate)) continue;

                    var candidateRotation = Quaternion.LookRotation(-outward, Vector3.up);
                    var localCenter = Vector3.Scale(boxCollider.center, scale);
                    var candidatePosition = horizontal;
                    candidatePosition.y = hit.point.y + halfHeight - localCenter.y + 0.02f;
                    var colliderCenter = candidatePosition + candidateRotation * localCenter;
                    var extents = new Vector3(halfWidth, halfHeight, halfDepth)
                        - Vector3.one * 0.02f;
                    var overlaps = Physics.OverlapBox(colliderCenter, extents,
                        candidateRotation, obstructionMask, QueryTriggerInteraction.Ignore);
                    var blocked = false;
                    foreach (var overlap in overlaps)
                        if (overlap.GetComponent<MazeFloor>() == null)
                        {
                            blocked = true;
                            break;
                        }
                    if (blocked) continue;

                    position = candidatePosition;
                    rotation = candidateRotation;
                    return true;
                }
            }
            return false;
        }

        private static bool TryWorldDirection(MazeGrid grid, MazeRenderer renderer,
            MazeCoordinate coordinate, MazeDirection direction, Vector3 center, out Vector3 result)
        {
            result = default;
            if (grid.Contains(coordinate + direction.ToOffset())
                && renderer.TryGetTileWorldPosition(coordinate + direction.ToOffset(), out var next))
                result = next - center;
            else if (grid.Contains(coordinate + direction.Opposite().ToOffset())
                && renderer.TryGetTileWorldPosition(coordinate + direction.Opposite().ToOffset(), out var previous))
                result = center - previous;
            result.y = 0f;
            if (result.sqrMagnitude < 0.001f) return false;
            result.Normalize();
            return true;
        }

        private void HandleUpdate(MazeGrid grid, MazeChangeSet changes)
        {
            if (changes.IsFullRebuild || !ReferenceEquals(activeGrid, grid)) HandleMazeReady(grid);
            else ReconcileBoxes(true);
        }

        private void ReconcileBoxes(bool geometryChanged)
        {
            if (bootstrapper == null || (!bootstrapper.IsMazeReady && !bootstrapper.Updates.IsUpdating)
                || !ReferenceEquals(activeGrid, bootstrapper.CurrentGrid))
            {
                if (boxRoot != null) boxRoot.gameObject.SetActive(false);
                return;
            }
            if (boxRoot == null || boxPrefab == null
                || !boxPrefab.TryGetComponent(out BoxCollider boxCollider)) return;

            if (geometryChanged) changeCount++;
            boxRoot.gameObject.SetActive(true);
            var renderer = bootstrapper.Renderer;
            if (renderer == null) return;
            var pending = new List<PlacedBox>();
            var reserved = new List<MazeCoordinate>();
            // 물리 검사는 이전 위치의 BOX에 막히지 않도록 모두 숨긴 뒤 수행한다.
            foreach (var item in placed)
            {
                if (item.Instance == null) continue;
                if (item.Instance.IsOpened) reserved.Add(item.Coordinate);
                else item.Instance.gameObject.SetActive(false);
            }
            Physics.SyncTransforms();

            var random = new System.Random(unchecked(bootstrapper.CurrentSeed + changeCount * 7919));
            foreach (var item in placed)
            {
                if (item.Instance == null || item.Instance.IsOpened) continue;
                if (item.HasPlacement && HasEnoughDistance(item.Coordinate, reserved)
                    && TryFindPose(activeGrid, renderer, boxCollider, item.Coordinate,
                        random, item.Instance.transform.rotation, out var position, out var rotation))
                {
                    ActivateAt(item, position, rotation);
                    reserved.Add(item.Coordinate);
                }
                else
                {
                    item.HasPlacement = false;
                    pending.Add(item);
                }
            }
            if (pending.Count == 0) return;

            var deadEnds = new List<MazeCoordinate>();
            var straightPaths = new List<MazeCoordinate>();
            foreach (var tile in activeGrid.Tiles)
            {
                if (!IsEligible(activeGrid, tile.Coordinate, out _, out var straight)) continue;
                if (straight) straightPaths.Add(tile.Coordinate);
                else deadEnds.Add(tile.Coordinate);
            }
            Shuffle(deadEnds, random);
            Shuffle(straightPaths, random);
            deadEnds.AddRange(straightPaths);
            foreach (var item in pending)
            {
                foreach (var coordinate in deadEnds)
                {
                    if (!HasEnoughDistance(coordinate, reserved)
                        || !TryFindPose(activeGrid, renderer, boxCollider, coordinate,
                            random, null, out var position, out var rotation)) continue;
                    item.Coordinate = coordinate;
                    item.HasPlacement = true;
                    ActivateAt(item, position, rotation);
                    reserved.Add(coordinate);
                    break;
                }
                // 후보가 없으면 다음 변화까지 비활성 상태로 보관한다.
            }
        }

        private bool HasEnoughDistance(MazeCoordinate coordinate, List<MazeCoordinate> reserved)
        {
            foreach (var other in reserved)
                if (Math.Abs(other.X - coordinate.X) + Math.Abs(other.Y - coordinate.Y)
                    < minimumBoxDistance) return false;
            return true;
        }

        private static void ActivateAt(PlacedBox item, Vector3 position, Quaternion rotation)
        {
            item.Instance.transform.SetPositionAndRotation(position, rotation);
            item.Instance.name = $"Box_{item.Coordinate.X}_{item.Coordinate.Y}";
            item.Instance.gameObject.SetActive(true);
            Physics.SyncTransforms();
        }

        private static void Shuffle(List<MazeCoordinate> coordinates, System.Random random)
        {
            for (var i = coordinates.Count - 1; i > 0; i--)
            {
                var next = random.Next(i + 1);
                (coordinates[i], coordinates[next]) = (coordinates[next], coordinates[i]);
            }
        }

        private void ClearBoxes()
        {
            activeGrid = null;
            placed.Clear();
            if (boxRoot == null) return;
            boxRoot.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(boxRoot.gameObject);
            else DestroyImmediate(boxRoot.gameObject);
            boxRoot = null;
        }
    }
}
