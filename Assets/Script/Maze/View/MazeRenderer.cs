using System.Collections.Generic;
using ShiftingPyramid.Maze.Core;
using UnityEngine;

namespace ShiftingPyramid.Maze.View
{
    /// <summary>
    /// MazeGrid 데이터를 Unity 씬의 타일 프리팹들로 배치한다.
    /// 미로 생성 알고리즘은 담당하지 않고, 생성된 데이터를 보여주는 역할만 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public class MazeRenderer : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private MazeTileView tilePrefab;
        [SerializeField] private Transform tileRoot;

        [Header("Layout")]
        [SerializeField] private float tileSize = 10f;
        [SerializeField] private bool centerOnOrigin = true;

        private readonly Dictionary<MazeCoordinate, MazeTileView> tileViews = new Dictionary<MazeCoordinate, MazeTileView>();

        public float TileSize => tileSize;

        // MazeGrid 전체를 프리팹으로 새로 만든다.
        public void Build(MazeGrid grid)
        {
            if (grid == null)
            {
                Debug.LogWarning("MazeRenderer.Build failed: grid is null.", this);
                return;
            }

            if (tilePrefab == null)
            {
                Debug.LogWarning("MazeRenderer.Build failed: tile prefab is not assigned.", this);
                return;
            }

            Clear();

            foreach (var tile in grid.Tiles)
            {
                var view = Instantiate(tilePrefab, GetTileRoot());
                view.transform.localPosition = GetLocalPosition(tile.Coordinate, grid.Width, grid.Height);
                view.name = $"MazeTile_{tile.Coordinate.X}_{tile.Coordinate.Y}";
                view.Apply(tile);

                tileViews.Add(tile.Coordinate, view);
            }
        }

        // 이미 만들어진 타일 프리팹에 벽 상태만 다시 반영한다.
        public void Refresh(MazeGrid grid)
        {
            if (grid == null)
            {
                return;
            }

            foreach (var tile in grid.Tiles)
            {
                if (tileViews.TryGetValue(tile.Coordinate, out var view))
                {
                    view.Apply(tile);
                }
            }
        }

        // 현재 렌더러가 만든 타일 오브젝트를 모두 삭제한다.
        public void Clear()
        {
            foreach (var view in tileViews.Values)
            {
                if (view == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(view.gameObject);
                }
                else
                {
                    DestroyImmediate(view.gameObject);
                }
            }

            tileViews.Clear();
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
