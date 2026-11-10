using ShiftingPyramid.Maze.Core;
using ShiftingPyramid.Maze.View;
using UnityEngine;
using ShiftingPyramid.Game.World;

namespace ShiftingPyramid.Game.Map
{
    /// <summary>플레이어가 밟거나 낮게 뛰어 지나간 미로 바닥을 기록한다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapStateConnector))]
    public sealed class MapDiscoveryTracker : MonoBehaviour
    {
        [SerializeField] private MazeTestBootstrapper bootstrapper;
        [SerializeField, Min(0.1f)] private float floorProbeDistance = 0.75f;
        [SerializeField, Min(0.1f)] private float airborneProbeDistance = 2.25f;

        private const float ProbeStartHeight = 0.15f;
        private const float MinimumFloorNormalY = 0.7f;
        private readonly RaycastHit[] floorHits = new RaycastHit[16];
        private MapStateConnector mapState;
        private Transform player;
        private CharacterController controller;
        private MazeGrid previousGrid;
        private Vector3 previousFoot;
        private bool hasPreviousFoot;
        private bool previousGrounded;

        private void Awake()
        {
            mapState = GetComponent<MapStateConnector>();
        }

        private void LateUpdate()
        {
            if (bootstrapper == null) bootstrapper = FindFirstObjectByType<MazeTestBootstrapper>();
            var state = mapState.State;
            if (bootstrapper == null || state == null)
            {
                hasPreviousFoot = false;
                return;
            }

            var grid = bootstrapper.CurrentGrid;
            var renderer = bootstrapper.Renderer;
            var currentPlayer = bootstrapper.PlayerTransform;
            if (grid == null || renderer == null || currentPlayer == null)
            {
                hasPreviousFoot = false;
                return;
            }

            if (player != currentPlayer)
            {
                player = currentPlayer;
                controller = player.GetComponent<CharacterController>();
                hasPreviousFoot = false;
            }
            if (!ReferenceEquals(previousGrid, grid))
            {
                previousGrid = grid;
                hasPreviousFoot = false;
            }
            if (controller == null || !controller.enabled || !controller.gameObject.activeInHierarchy)
            {
                hasPreviousFoot = false;
                return;
            }

            var foot = controller.bounds.center;
            foot.y = controller.bounds.min.y;
            var steps = 1;
            if (hasPreviousFoot
                && renderer.TryGetContinuousGridPosition(previousFoot, grid, out var previousPosition)
                && renderer.TryGetContinuousGridPosition(foot, grid, out var currentPosition))
            {
                var distance = Vector2.Distance(previousPosition, currentPosition);
                // 순간 이동은 건너온 길을 탐험한 것으로 처리하지 않는다.
                if (distance <= 1.5f) steps = Mathf.Max(1, Mathf.CeilToInt(distance * 2f));
                else hasPreviousFoot = false;
            }

            for (var step = hasPreviousFoot ? 1 : steps; step <= steps; step++)
            {
                var sample = hasPreviousFoot
                    ? Vector3.Lerp(previousFoot, foot, step / (float)steps) : foot;
                var groundedSample = controller.isGrounded
                    && (!hasPreviousFoot || previousGrounded || step == steps);
                TryRevealFloor(sample, groundedSample, grid, renderer, state);
            }
            previousFoot = foot;
            hasPreviousFoot = true;
            previousGrounded = controller.isGrounded;
        }

        private void TryRevealFloor(Vector3 foot, bool grounded, MazeGrid grid,
            MazeRenderer renderer, MapExplorationState state)
        {
            if (!renderer.TryGetCoordinate(foot, grid, out var coordinate)
                || state.IsExplored(coordinate)) return;

            // 공중에서는 짧은 거리 안의 실제 미로 바닥만 공개한다.
            var origin = foot + Vector3.up * ProbeStartHeight;
            var hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, floorHits,
                ProbeStartHeight + (grounded ? floorProbeDistance : airborneProbeDistance),
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            if (hitCount == floorHits.Length) return;

            var closestIndex = -1;
            for (var i = 0; i < hitCount; i++)
            {
                var hit = floorHits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(player)) continue;
                if (closestIndex < 0 || hit.distance < floorHits[closestIndex].distance)
                    closestIndex = i;
            }
            if (closestIndex < 0) return;

            var floorHit = floorHits[closestIndex];
            if (floorHit.normal.y < MinimumFloorNormalY
                || floorHit.collider.GetComponent<MazeFloor>() == null
                || !renderer.TryGetCoordinate(floorHit.point, grid, out var floorCoordinate)
                || !floorCoordinate.Equals(coordinate)) return;

            state.Reveal(coordinate);
        }
    }
}
