using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 결과 패널 컨트롤러 (LJH 버전 - DJS UI 시스템 호환)
/// </summary>
public class BattleResultController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject resultPanel;     // 결과 패널
    public Text resultText;        // 패널 내 텍스트

    void Start()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    /// <summary>
    /// 승리 결과로 결과 패널을 표시합니다.
    /// </summary>
    public void ShowWin()
    {
        if (resultPanel == null || resultText == null)
            return;

        resultText.text = "Win!";
        resultPanel.SetActive(true);
    }

    /// <summary>
    /// 결과 패널을 숨깁니다.
    /// </summary>
    public void HideResult()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    /// <summary>
    /// 커스텀 메시지로 결과 패널 표시
    /// </summary>
    public void ShowResult(string message)
    {
        if (resultPanel == null || resultText == null)
            return;

        resultText.text = message;
        resultPanel.SetActive(true);
    }
}
