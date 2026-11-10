using System;
using ShiftingPyramid.Maze.Core;

namespace ShiftingPyramid.Maze.View
{
    /// <summary>상자 → 횃불 → 내비게이션 순서. 렌더링 뒤, 준비 완료 알림 전에 실행한다.</summary>
    public sealed class MazeUpdatePipeline
    {
        public event Action<MazeGrid, MazeChangeSet> Preparing;
        public event Action<MazeGrid, MazeChangeSet> ContentsUpdating;
        public event Action<MazeGrid, MazeChangeSet> DecorationsUpdating;
        public event Action<MazeGrid, MazeChangeSet> NavigationUpdating;

        public bool IsUpdating { get; private set; }

        public void Apply(MazeGrid grid, MazeChangeSet changes)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            if (changes == null) throw new ArgumentNullException(nameof(changes));
            if (IsUpdating) throw new InvalidOperationException("Maze update is already running.");
            IsUpdating = true;
            try
            {
                Preparing?.Invoke(grid, changes);
                ContentsUpdating?.Invoke(grid, changes);
                DecorationsUpdating?.Invoke(grid, changes);
                NavigationUpdating?.Invoke(grid, changes);
            }
            finally
            {
                IsUpdating = false;
            }
        }
    }
}
