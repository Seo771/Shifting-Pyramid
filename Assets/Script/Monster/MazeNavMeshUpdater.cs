using ShiftingPyramid.Maze.View;
using ShiftingPyramid.Maze.Core;
using Unity.AI.Navigation;
using UnityEngine;

/// <summary>미로 표시가 바뀐 뒤 몬스터 이동용 NavMesh를 다시 만든다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MazeRenderer), typeof(NavMeshSurface))]
public class MazeNavMeshUpdater : MonoBehaviour
{
    private MazeTestBootstrapper bootstrapper;
    private NavMeshSurface navMeshSurface;

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        // Renderer를 Start에서 자동 탐색한 테스트 씬도 지원한다.
        if (bootstrapper == null) Subscribe();
    }

    private void Subscribe()
    {
        navMeshSurface = GetComponent<NavMeshSurface>();
        var renderer = GetComponent<MazeRenderer>();
        foreach (var candidate in FindObjectsByType<MazeTestBootstrapper>(FindObjectsSortMode.None))
            if (candidate.Renderer == renderer)
            {
                bootstrapper = candidate;
                break;
            }
        if (bootstrapper == null)
        {
            return;
        }
        bootstrapper.Updates.NavigationUpdating += RebuildNavMesh;
        if (bootstrapper.IsMazeReady) RebuildNavMesh(bootstrapper.CurrentGrid, null);
    }

    private void OnDisable()
    {
        if (bootstrapper != null) bootstrapper.Updates.NavigationUpdating -= RebuildNavMesh;
        bootstrapper = null;
    }

    private void RebuildNavMesh(MazeGrid grid, MazeChangeSet changes)
    {
        Physics.SyncTransforms();
        navMeshSurface.BuildNavMesh();
    }
}
