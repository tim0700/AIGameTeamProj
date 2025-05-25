using UnityEngine;
using TMPro;

public class ResultPanelController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject resultPanel;     // 결과 패널
    public TMP_Text resultText;        // 패널 내 텍스트

    void Start()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    void Update()
    {
        // 0 키를 눌러 패널 토글
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            if (resultPanel == null)
                return;

            if (resultPanel.activeSelf)
            {
                // 켜져 있으면 끄기
                resultPanel.SetActive(false);
            }
            else
            {
                // 꺼져 있으면 ShowWin() 호출해 켜기
                ShowWin();
            }
        }
    }

    /// <summary>
    /// 승리 상태로 결과 패널을 켭니다.
    /// </summary>
    public void ShowWin()
    {
        if (resultPanel == null || resultText == null)
            return;

        resultText.text = "win!";
        resultPanel.SetActive(true);
    }

    /// <summary>
    /// 패널을 끄고 싶을 때 직접 호출할 수도 있습니다.
    /// </summary>
    public void HideResult()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }
}
