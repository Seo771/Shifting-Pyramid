using ShiftingPyramid.Game.Map;

namespace ShiftingPyramid.Game
{
    /// <summary>시간 정지만으로 막을 수 없는 플레이 조작의 공통 경계.</summary>
    public static class GameplayInput
    {
        public static bool IsBlocked => MapScreenController.IsInputBlocked
            || GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing;
    }
}
