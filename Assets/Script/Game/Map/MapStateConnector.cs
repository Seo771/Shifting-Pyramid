using System;
using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.View;
using UnityEngine;

namespace ShiftingPyramid.Game.Map
{
    /// <summary>완료된 미로마다 게임용 탐험 상태를 새로 만든다.</summary>
    [DisallowMultipleComponent]
    public sealed class MapStateConnector : MonoBehaviour
    {
        [SerializeField] private MazeTestBootstrapper bootstrapper;

        private MazeGrid stateGrid;
        private MapExplorationState state;
        private MazeRenderer subscribedRenderer;

        public MapExplorationState State => bootstrapper != null && bootstrapper.IsMazeReady
            && ReferenceEquals(stateGrid, bootstrapper.CurrentGrid) ? state : null;
        public event Action<MapExplorationState> StateChanged;

        private void OnEnable()
        {
            if (bootstrapper == null) bootstrapper = FindFirstObjectByType<MazeTestBootstrapper>();
            if (bootstrapper == null) return;

            bootstrapper.MazeReady += HandleMazeReady;
            SubscribeRenderer();
            // 늦게 활성화된 경우에도 완료된 현재 미로를 받는다.
            if (!bootstrapper.IsMazeReady || bootstrapper.CurrentGrid == null) return;
            if (!ReferenceEquals(stateGrid, bootstrapper.CurrentGrid))
                HandleMazeReady(bootstrapper.CurrentGrid);
            else state?.Reconcile(bootstrapper.CurrentGrid);
        }

        private void OnDisable()
        {
            if (bootstrapper != null) bootstrapper.MazeReady -= HandleMazeReady;
            if (subscribedRenderer != null)
                subscribedRenderer.GeometryChanged -= HandleGeometryChanged;
            subscribedRenderer = null;
        }

        private void HandleMazeReady(MazeGrid grid)
        {
            if (grid == null) return;
            SubscribeRenderer();
            stateGrid = grid;
            state = new MapExplorationState(grid);
            StateChanged?.Invoke(state);
        }

        private void SubscribeRenderer()
        {
            var renderer = bootstrapper != null ? bootstrapper.Renderer : null;
            if (ReferenceEquals(subscribedRenderer, renderer)) return;
            if (subscribedRenderer != null)
                subscribedRenderer.GeometryChanged -= HandleGeometryChanged;
            subscribedRenderer = renderer;
            if (subscribedRenderer != null)
                subscribedRenderer.GeometryChanged += HandleGeometryChanged;
        }

        private void HandleGeometryChanged()
        {
            // 초기 Build 이벤트는 MazeReady보다 먼저 오므로 여기서는 무시한다.
            if (bootstrapper == null || !bootstrapper.IsMazeReady
                || state == null || !ReferenceEquals(stateGrid, bootstrapper.CurrentGrid)) return;
            state.Reconcile(stateGrid);
        }
    }
}
