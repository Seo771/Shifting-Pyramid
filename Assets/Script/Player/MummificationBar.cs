
using UnityEngine;
using UnityEngine.UI;

public class MummificationBar : MonoBehaviour
{
    [Header("미라화 바 설정")]
    [SerializeField] private Mummification mummification;
    [SerializeField] private Slider mummificationSlider;

    private void Update()
    {
        if (mummification == null || mummificationSlider == null)
            return;

        // 미라화 수치(0~100)를 슬라이더에 반영
        mummificationSlider.value = mummification.CurrentValue;
    }
}
