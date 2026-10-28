using ShiftingPyramid.Maze.View;
using UnityEngine;

namespace ShiftingPyramid.Game.Map
{
    /// <summary>플레이어는 중앙에 두고 탐험 지도를 반대 방향으로 움직인다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapGridView))]
    public sealed class MiniMapController : MonoBehaviour
    {
        [SerializeField] private MazeTestBootstrapper bootstrapper;
        [SerializeField] private MapGridView gridView;
        [SerializeField] private RectTransform playerArrow;
        [SerializeField] private float arrowRotationOffset;

        private void Awake()
        {
            if (gridView == null) gridView = GetComponent<MapGridView>();
        }

        private void LateUpdate()
        {
            if (bootstrapper == null) bootstrapper = FindFirstObjectByType<MazeTestBootstrapper>();
            var player = bootstrapper != null ? bootstrapper.PlayerTransform : null;
            var renderer = bootstrapper != null ? bootstrapper.Renderer : null;
            var grid = bootstrapper != null ? bootstrapper.CurrentGrid : null;
            var ready = bootstrapper != null && bootstrapper.IsMazeReady
                && gridView.CurrentState != null && player != null && renderer != null
                && gridView.MapContent != null && playerArrow != null;
            if (playerArrow != null) playerArrow.gameObject.SetActive(ready);
            if (!ready || !renderer.TryGetContinuousGridPosition(player.position, grid, out var position)) return;

            gridView.MapContent.anchoredPosition = -position * gridView.CellSize;
            var direction = renderer.GetGridDirection(player.forward);
            if (direction.sqrMagnitude > 0.001f)
            {
                var angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
                playerArrow.localRotation = Quaternion.Euler(0f, 0f, arrowRotationOffset - angle);
            }
        }
    }
}
