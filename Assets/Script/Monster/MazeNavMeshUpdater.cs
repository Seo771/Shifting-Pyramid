using ShiftingPyramid.Maze.View;
using Unity.AI.Navigation;
using UnityEngine;

/// <summary>미로 표시가 바뀐 뒤 몬스터 이동용 NavMesh를 다시 만든다.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MazeRenderer), typeof(NavMeshSurface))]
public class MazeNavMeshUpdater : MonoBehaviour
{
    private MazeRenderer mazeRenderer;
    private NavMeshSurface navMeshSurface;

    private void OnEnable()
    {
        mazeRenderer = GetComponent<MazeRenderer>();
        navMeshSurface = GetComponent<NavMeshSurface>();
        mazeRenderer.GeometryChanged += RebuildNavMesh;
    }

    private void OnDisable()
    {
        if (mazeRenderer != null) mazeRenderer.GeometryChanged -= RebuildNavMesh;
    }

    private void RebuildNavMesh()
    {
        navMeshSurface.BuildNavMesh();
    }
}
