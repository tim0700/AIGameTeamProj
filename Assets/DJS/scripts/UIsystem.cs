using UnityEngine;
using TMPro;

public class ResultPanelController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject resultPanel;     // ��� �г�
    public TMP_Text resultText;        // �г� �� �ؽ�Ʈ

    void Start()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    void Update()
    {
        // 0 Ű�� ���� �г� ���
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            if (resultPanel == null)
                return;

            if (resultPanel.activeSelf)
            {
                // ���� ������ ����
                resultPanel.SetActive(false);
            }
            else
            {
                // ���� ������ ShowWin() ȣ���� �ѱ�
                ShowWin();
            }
        }
    }

    /// <summary>
    /// �¸� ���·� ��� �г��� �մϴ�.
    /// </summary>
    public void ShowWin()
    {
        if (resultPanel == null || resultText == null)
            return;

        resultText.text = "win!";
        resultPanel.SetActive(true);
    }

    /// <summary>
    /// �г��� ���� ���� �� ���� ȣ���� ���� �ֽ��ϴ�.
    /// </summary>
    public void HideResult()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }
}
