namespace ShiftingPyramid.Maze.Core
{
    /// <summary>
    /// 타일의 기본 용도.
    /// 게임 오브젝트를 직접 참조하지 않고 좌표 데이터에 의미만 표시한다.
    /// </summary>
    public enum MazeTileType
    {
        Normal,
        TreasureRoom,
        Exit,
        FixedSpecialRoom,
        DynamicSpecialRoom
    }
}
