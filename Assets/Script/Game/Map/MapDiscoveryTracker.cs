using ShiftingPyramid.Maze.View;
using UnityEngine;

namespace ShiftingPyramid.Game.Map
{
    /// <summary>플레이어가 밟은 미로 바닥의 칸만 탐험 상태에 기록한다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MapStateConnector))]
    public sealed class MapDiscoveryTracker : MonoBehaviour
    {
        [SerializeField] private MazeTestBootstrapper bootstrapper;
        [SerializeField, Min(0.1f)] private float floorProbeDistance = 0.75f;

        private const float ProbeStartHeight = 0.15f;
        private const float MinimumFloorNormalY = 0.7f;
        private readonly RaycastHit[] floorHits = new RaycastHit[16];
        private MapStateConnector mapState;
        private Transform player;
        private CharacterController controller;

        private void Awake()
        {
            mapState = GetComponent<MapStateConnector>();
        }

        private void LateUpdate()
        {
            if (bootstrapper == null) bootstrapper = FindFirstObjectByType<MazeTestBootstrapper>();
            if (bootstrapper == null || mapState.State == null) return;

            var grid = bootstrapper.CurrentGrid;
            var renderer = bootstrapper.Renderer;
            var currentPlayer = bootstrapper.PlayerTransform;
            if (grid == null || renderer == null || currentPlayer == null) return;

            if (player != currentPlayer)
            {
                player = currentPlayer;
                controller = player.GetComponent<CharacterController>();
            }
            if (controller == null || !controller.enabled || !controller.gameObject.activeInHierarchy
                || !controller.isGrounded) return;

            var foot = controller.bounds.center;
            foot.y = controller.bounds.min.y;
            if (!renderer.TryGetCoordinate(foot, grid, out var coordinate)
                || mapState.State.IsExplored(coordinate)) return;

            // 캐릭터 자신의 콜라이더를 건너뛰고 실제 발밑의 첫 물체만 확인한다.
            var origin = foot + Vector3.up * ProbeStartHeight;
            var hitCount = Physics.RaycastNonAlloc(origin, Vector3.down, floorHits,
                ProbeStartHeight + floorProbeDistance, Physics.DefaultRaycastLayers,
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
                || floorHit.collider.GetComponent<MapFloor>() == null
                || !renderer.TryGetCoordinate(floorHit.point, grid, out var floorCoordinate)
                || !floorCoordinate.Equals(coordinate)) return;

            mapState.State.Reveal(coordinate);
        }
    }
}
