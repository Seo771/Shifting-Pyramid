using System.Collections.Generic;
using ShiftingPyramid.Maze.Core;
using UnityEngine;

namespace ShiftingPyramid.Game.Map
{
    /// <summary>공유 탐험 상태에서 공개된 칸만 UI 타일로 만든다.</summary>
    [DisallowMultipleComponent]
    public sealed class MapGridView : MonoBehaviour
    {
        [SerializeField] private MapStateConnector stateConnector;
        [SerializeField] private RectTransform mapContent;
        [SerializeField] private MapTileView tilePrefab;
        [SerializeField, Min(1f)] private float cellSize = 32f;

        private readonly Dictionary<MazeCoordinate, MapTileView> tiles = new Dictionary<MazeCoordinate, MapTileView>();
        private MapExplorationState currentState;

        public float CellSize => cellSize;
        public MapExplorationState CurrentState => currentState;
        public RectTransform MapContent => mapContent;

        private void OnEnable()
        {
            if (stateConnector == null) stateConnector = GetComponentInParent<MapStateConnector>();
            if (stateConnector != null) stateConnector.StateChanged += Bind;
            Bind(stateConnector != null ? stateConnector.State : null);
        }

        private void LateUpdate()
        {
            // 생성 중에는 이전 미로의 지도를 계속 보여주지 않는다.
            var readyState = stateConnector != null ? stateConnector.State : null;
            if (!ReferenceEquals(currentState, readyState)) Bind(readyState);
        }

        private void OnDisable()
        {
            if (stateConnector != null) stateConnector.StateChanged -= Bind;
            Bind(null);
        }

        private void Bind(MapExplorationState next)
        {
            if (ReferenceEquals(currentState, next)) return;
            if (currentState != null) currentState.TileChanged -= RefreshTile;
            ClearTiles();
            currentState = next;
            if (currentState == null) return;
            currentState.TileChanged += RefreshTile;
            for (var x = 0; x < currentState.Width; x++)
                for (var y = 0; y < currentState.Height; y++)
                    RefreshTile(new MazeCoordinate(x, y));
        }

        private void RefreshTile(MazeCoordinate coordinate)
        {
            if (currentState == null || !currentState.TryGetKnownTile(coordinate, out var known))
            {
                if (tiles.TryGetValue(coordinate, out var oldTile))
                {
                    tiles.Remove(coordinate);
                    oldTile.gameObject.SetActive(false);
                    Destroy(oldTile.gameObject);
                }
                return;
            }
            if (mapContent == null || tilePrefab == null) return;
            if (!tiles.TryGetValue(coordinate, out var tile))
            {
                tile = Instantiate(tilePrefab, mapContent);
                tile.name = $"MapTile_{coordinate.X}_{coordinate.Y}";
                tiles.Add(coordinate, tile);
            }
            var rect = (RectTransform)tile.transform;
            rect.anchoredPosition = new Vector2(coordinate.X * cellSize, coordinate.Y * cellSize);
            rect.sizeDelta = Vector2.one * cellSize;
            tile.Show(known, coordinate, cellSize);
        }

        private void ClearTiles()
        {
            foreach (var tile in tiles.Values)
            {
                if (tile == null) continue;
                tile.gameObject.SetActive(false);
                Destroy(tile.gameObject);
            }
            tiles.Clear();
        }
    }
}
