using ShiftingPyramid.Maze.View;
using UnityEngine;

namespace ShiftingPyramid.Game.Map
{
    /// <summary>전체 격자를 화면에 맞추고 현재 플레이어 위치를 표시한다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapGridView))]
    public sealed class FullMapController : MonoBehaviour
    {
        [SerializeField] private MazeTestBootstrapper bootstrapper;
        [SerializeField] private MapGridView gridView;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform playerArrow;
        [SerializeField, Min(0f)] private float edgePadding = 24f;
        [SerializeField] private float arrowRotationOffset;

        private MapExplorationState fittedState;
        private Vector2 fittedSize;
        private float fittedCellSize;
        private float mapScale = 1f;

        private void Awake()
        {
            if (gridView == null) gridView = GetComponent<MapGridView>();
        }

        private void LateUpdate()
        {
            if (bootstrapper == null) bootstrapper = FindFirstObjectByType<MazeTestBootstrapper>();
            var state = gridView != null ? gridView.CurrentState : null;
            var player = bootstrapper != null ? bootstrapper.PlayerTransform : null;
            var renderer = bootstrapper != null ? bootstrapper.Renderer : null;
            var grid = bootstrapper != null ? bootstrapper.CurrentGrid : null;
            var ready = bootstrapper != null && bootstrapper.IsMazeReady
                && state != null && player != null && renderer != null
                && viewport != null && gridView.MapContent != null && playerArrow != null;
            if (playerArrow != null) playerArrow.gameObject.SetActive(ready);
            if (!ready) return;

            var size = viewport.rect.size;
            if (!ReferenceEquals(fittedState, state) || fittedSize != size
                || !Mathf.Approximately(fittedCellSize, gridView.CellSize))
                FitMap(state, size);

            if (!renderer.TryGetContinuousGridPosition(player.position, grid, out var position)) return;
            var center = new Vector2((state.Width - 1) * 0.5f, (state.Height - 1) * 0.5f);
            playerArrow.anchoredPosition = (position - center) * (gridView.CellSize * mapScale);
            var direction = renderer.GetGridDirection(player.forward);
            if (direction.sqrMagnitude > 0.001f)
            {
                var angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                playerArrow.localRotation = Quaternion.Euler(0f, 0f, arrowRotationOffset - angle);
            }
        }

        private void FitMap(MapExplorationState state, Vector2 size)
        {
            fittedState = state;
            fittedSize = size;
            fittedCellSize = gridView.CellSize;
            var availableWidth = Mathf.Max(1f, size.x - edgePadding * 2f);
            var availableHeight = Mathf.Max(1f, size.y - edgePadding * 2f);
            mapScale = Mathf.Min(availableWidth / (state.Width * fittedCellSize),
                availableHeight / (state.Height * fittedCellSize));
            var content = gridView.MapContent;
            content.localScale = Vector3.one * mapScale;
            // 타일 좌표 0부터 시작하는 컨텐츠를 뷰포트 중앙에 맞춘다.
            content.anchoredPosition = new Vector2(-(state.Width - 1) * fittedCellSize * mapScale * 0.5f,
                -(state.Height - 1) * fittedCellSize * mapScale * 0.5f);
        }
    }
}
