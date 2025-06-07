using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// DJS UI 시스템과 LJH 전투 시스템을 연결하는 UI 관리자
/// </summary>
public class BattleUIManager : MonoBehaviour
{
    [Header("에이전트 A UI (왼쪽)")]
    public HealthBar healthBarA;
    public Image skillAttackA;      // 공격 스킬 쿨타임 이미지
    public Image skillDefendA;      // 방어 스킬 쿨타임 이미지
    public Image skillDodgeA;       // 회피 스킬 쿨타임 이미지
    public Text agentNameA;     // 에이전트 이름 텍스트

    [Header("에이전트 B UI (오른쪽)")]
    public HealthBar healthBarB;
    public Image skillAttackB;
    public Image skillDefendB;
    public Image skillDodgeB;
    public Text agentNameB;

    [Header("전투 상황 UI")]
    public Text battleStatusText;
    public Text battleTimerText;
    public GameObject battleControlPanel;  // 시작/리셋 버튼 패널 (전투 중 숨김)
    public Button startBattleButton;
    public Button resetBattleButton;

    [Header("결과 UI")]
    public BattleResultController resultController;
    public Text winnerText;
    public Text battleStatsText;
    public Button closeResultButton;  // 결과 패널 닫기 버튼 추가

    // 내부 상태 관리
    private AgentController currentAgentA;
    private AgentController currentAgentB;
    private BattleManager battleManager;
    
    // 이전 HP 값 저장 (HealthBar.ChangeHealth 계산용)
    private float lastHPA = 100f;
    private float lastHPB = 100f;

    // UI 업데이트 빈도 제어
    private float uiUpdateInterval = 0.1f;
    private float lastUIUpdate = 0f;

    // //~~~조익준 자동화 코드~~~~//
    // public void AutoRestartBattle()
    // {
    //     StartCoroutine(RestartBattleCoroutine());
    // }

    // private IEnumerator RestartBattleCoroutine()
    // {
    //     yield return new WaitForSeconds(1.0f); 
    //     battleManager.ResetBattle();
    //     battleManager.StartBattle();
    // } //~~조익준


    void Start()
    {
        // BattleManager 찾기
#if UNITY_2023_1_OR_NEWER
        battleManager = FindFirstObjectByType<BattleManager>();
#else
        battleManager = FindObjectOfType<BattleManager>();
#endif
        if (battleManager == null)
        {
            Debug.LogError("BattleManager를 찾을 수 없습니다!");
            return;
        }

        // 버튼 이벤트 연결
        if (startBattleButton != null)
            startBattleButton.onClick.AddListener(OnStartBattleClicked);
        
        if (resetBattleButton != null)
            resetBattleButton.onClick.AddListener(OnResetBattleClicked);

        if (closeResultButton != null)
            closeResultButton.onClick.AddListener(OnCloseResultClicked);

        // 초기 UI 설정
        if (battleControlPanel != null)
            battleControlPanel.SetActive(true);

        UpdateBattleStatusText("Battle Ready...");
    }

    void Update()
    {
        // UI 업데이트 주기 제어
        if (Time.time - lastUIUpdate < uiUpdateInterval) return;
        lastUIUpdate = Time.time;

        // 타이머 업데이트 (항상 업데이트)
        UpdateBattleTimer();

        // 전투 중일 때만 실시간 UI 업데이트
        if (battleManager != null && currentAgentA != null && currentAgentB != null)
        {
            UpdateAgentUI();
        }
    }

    #region Public Methods - BattleManager에서 호출

    /// <summary>
    /// 전투 시작 시 에이전트 UI 초기화
    /// </summary>
    public void InitializeBattle(AgentController agentA, AgentController agentB)
    {
        currentAgentA = agentA;
        currentAgentB = agentB;

        // 에이전트 이름 설정
        if (agentNameA != null)
            agentNameA.text = agentA.GetAgentName();
        if (agentNameB != null)
            agentNameB.text = agentB.GetAgentName();

        // HP 초기화
        lastHPA = agentA.GetMaxHP();
        lastHPB = agentB.GetMaxHP();

        // 체력바 초기화
        if (healthBarA != null)
        {
            healthBarA.maxHealth = agentA.GetMaxHP();
            healthBarA.GetComponent<HealthBar>().SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }
        if (healthBarB != null)
        {
            healthBarB.maxHealth = agentB.GetMaxHP();
            healthBarB.GetComponent<HealthBar>().SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        }

        // 스킬 UI 초기화
        ResetSkillUI(skillAttackA, skillDefendA, skillDodgeA);
        ResetSkillUI(skillAttackB, skillDefendB, skillDodgeB);

        // 전투 제어 패널 숨기기 (시작/리셋 버튼)
        if (battleControlPanel != null)
            battleControlPanel.SetActive(false);

        UpdateBattleStatusText("Battle in Progress...");

        Debug.Log("BattleUIManager: Battle UI Initialization Complete");
    }

    /// <summary>
    /// 전투 종료 시 결과 표시
    /// </summary>
    public void ShowBattleResult(AgentController winner, BattleManager.BattleData battleData)
    {
        // 승자 표시
        string winnerName = winner?.GetAgentName() ?? "무승부";
        
        if (winnerText != null)
            winnerText.text = $"승자: {winnerName}";

        // 전투 통계 표시
        if (battleStatsText != null)
        {
            battleStatsText.text = $"Battle Duration: {battleData.duration:F1}s\n" +
                                  $"{battleData.agentAName} Final HP: {battleData.agentAFinalHP:F0}\n" +
                                  $"{battleData.agentBName} Final HP: {battleData.agentBFinalHP:F0}";
        }

        // DJS 결과 패널 표시
        if (resultController != null)
        {
            if (winner == currentAgentA)
                resultController.ShowResult($"{currentAgentA.GetAgentName()} Wins!");
            else if (winner == currentAgentB)
                resultController.ShowResult($"{currentAgentB.GetAgentName()} Wins!");
            else
                resultController.ShowResult("Draw!");
        }

        // 전투 제어 패널 다시 표시
        if (battleControlPanel != null)
            battleControlPanel.SetActive(true);

        UpdateBattleStatusText($"Battle End - {winnerName} Wins!");

        Debug.Log($"BattleUIManager: Battle Result Display Complete - {winnerName}");
    }

    /// <summary>
    /// 전투 리셋 시 UI 초기화
    /// </summary>
    public void ResetBattleUI()
    {
        // 결과 패널 숨기기
        if (resultController != null)
            resultController.HideResult();

        // 체력바 초기화
        lastHPA = currentAgentA?.GetMaxHP() ?? 100f;
        lastHPB = currentAgentB?.GetMaxHP() ?? 100f;

        // 스킬 UI 초기화
        ResetSkillUI(skillAttackA, skillDefendA, skillDodgeA);
        ResetSkillUI(skillAttackB, skillDefendB, skillDodgeB);

        UpdateBattleStatusText("Battle Reset Complete");

        Debug.Log("BattleUIManager: UI Reset Complete");
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 전투 타이머 업데이트 (항상 업데이트)
    /// </summary>
    private void UpdateBattleTimer()
    {
        if (battleTimerText == null || battleManager == null) return;

        float battleTime = battleManager.GetCurrentBattleTime();
        battleTimerText.text = $"Battle Time: {battleTime:F1}s";
    }

    /// <summary>
    /// 실시간 에이전트 UI 업데이트
    /// </summary>
    private void UpdateAgentUI()
    {
        if (currentAgentA == null || currentAgentB == null) return;

        // 체력 업데이트
        UpdateHealthBar(currentAgentA, healthBarA, ref lastHPA);
        UpdateHealthBar(currentAgentB, healthBarB, ref lastHPB);

        // 스킬 쿨타임 업데이트
        UpdateSkillCooldowns(currentAgentA, skillAttackA, skillDefendA, skillDodgeA);
        UpdateSkillCooldowns(currentAgentB, skillAttackB, skillDefendB, skillDodgeB);
    }

    /// <summary>
    /// 개별 에이전트 체력바 업데이트
    /// </summary>
    private void UpdateHealthBar(AgentController agent, HealthBar healthBar, ref float lastHP)
    {
        if (agent == null || healthBar == null) return;

        float currentHP = agent.GetCurrentHP();
        if (Mathf.Abs(currentHP - lastHP) > 0.1f) // 변화가 있을 때만 업데이트
        {
            float deltaHP = currentHP - lastHP;
            healthBar.SendMessage("ChangeHealth", deltaHP, SendMessageOptions.DontRequireReceiver);
            lastHP = currentHP;
        }
    }

    /// <summary>
    /// 스킬 쿨타임 UI 업데이트
    /// </summary>
    private void UpdateSkillCooldowns(AgentController agent, Image attackImg, Image defendImg, Image dodgeImg)
    {
        if (agent == null) return;

        CooldownState cooldowns = agent.GetCooldownState();

        // 공격 쿨타임
        if (attackImg != null)
        {
            float ratio = cooldowns.attackCooldown / cooldowns.attackMaxTime;
            attackImg.fillAmount = Mathf.Clamp01(ratio);
            attackImg.enabled = ratio > 0f;
        }

        // 방어 쿨타임
        if (defendImg != null)
        {
            float ratio = cooldowns.defendCooldown / cooldowns.defendMaxTime;
            defendImg.fillAmount = Mathf.Clamp01(ratio);
            defendImg.enabled = ratio > 0f;
        }

        // 회피 쿨타임
        if (dodgeImg != null)
        {
            float ratio = cooldowns.dodgeCooldown / cooldowns.dodgeMaxTime;
            dodgeImg.fillAmount = Mathf.Clamp01(ratio);
            dodgeImg.enabled = ratio > 0f;
        }
    }

    /// <summary>
    /// 스킬 UI 초기화
    /// </summary>
    private void ResetSkillUI(Image attack, Image defend, Image dodge)
    {
        if (attack != null) { attack.fillAmount = 0f; attack.enabled = false; }
        if (defend != null) { defend.fillAmount = 0f; defend.enabled = false; }
        if (dodge != null) { dodge.fillAmount = 0f; dodge.enabled = false; }
    }

    /// <summary>
    /// 전투 상황 텍스트 업데이트
    /// </summary>
    private void UpdateBattleStatusText(string status)
    {
        if (battleStatusText != null)
            battleStatusText.text = status;
    }

    #endregion

    #region Button Events

    private void OnStartBattleClicked()
    {
        if (battleManager != null)
            battleManager.StartBattle();
    }

    private void OnResetBattleClicked()
    {
        if (battleManager != null)
            battleManager.ResetBattle();
    }

    private void OnCloseResultClicked()
    {
        if (resultController != null)
            resultController.HideResult();
        
        // 전투 제어 패널 다시 표시
        if (battleControlPanel != null)
            battleControlPanel.SetActive(true);
        
        Debug.Log("BattleUIManager: Result Panel Closed");
    }

    #endregion

    #region Public Utility Methods

    /// <summary>
    /// 특정 에이전트의 행동 결과를 UI에 표시 (선택사항)
    /// </summary>
    public void ShowActionResult(AgentController agent, ActionResult result)
    {
        if (!result.success) return;

        // 행동 성공 시 간단한 피드백 표시 (선택사항)
        string actionText = result.actionType switch
        {
            ActionType.Attack => "⚔️",
            ActionType.Defend => "🛡️", 
            ActionType.Dodge => "💨",
            _ => ""
        };

        // 여기에 행동 피드백 UI 추가 가능 (예: 에이전트 위에 이모지 표시)
        Debug.Log($"{agent.GetAgentName()}: {actionText} {result.actionType}");
    }

    #endregion
}
