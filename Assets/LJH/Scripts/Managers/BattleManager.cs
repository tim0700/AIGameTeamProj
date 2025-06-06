using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using LJH.BT;

public class BattleManager : MonoBehaviour
{
    [Header("������Ʈ ����")]
    public AgentController agentA;
    public AgentController agentB;

    [Header("���� ����")]
    public float maxBattleDuration = 60f;
    public Transform spawnPointA;
    public Transform spawnPointB;

    [Header("UI ����")]
    public Text battleStatusText;
    public Button startBattleButton;
    public Button resetBattleButton;
    public BattleUIManager uiManager;  // UI 관리자 추가

    [Header("�Ʒ��� ����")]
    public Vector3 arenaCenter = Vector3.zero;
    public float arenaRadius = 10f;

    // ���� ����
    private bool battleActive = false;
    private float battleStartTime;
    private int battleCount = 0;

    // ������ ����
    private BattleData currentBattleData;

    [System.Serializable]
    public class BattleData
    {
        public string agentAName;
        public string agentBName;
        public float duration;
        public string winner;
        public float agentAFinalHP;
        public float agentBFinalHP;
        public int agentAActions;
        public int agentBActions;
    }

    void Start()
    {
        SetupUI();
        SetupAgents();
    }

    void Update()
    {
        if (battleActive)
        {
            UpdateBattle();
            CheckBattleEnd();
        }

        UpdateUI();
    }

    private void SetupUI()
    {
        if (startBattleButton != null)
            startBattleButton.onClick.AddListener(StartBattle);

        if (resetBattleButton != null)
            resetBattleButton.onClick.AddListener(ResetBattle);
    }

    private void SetupAgents()
    {
        if (agentA != null && spawnPointA != null)
        {
            agentA.transform.position = spawnPointA.position;
            agentA.transform.rotation = spawnPointA.rotation;
        }

        if (agentB != null && spawnPointB != null)
        {
            agentB.transform.position = spawnPointB.position;
            agentB.transform.rotation = spawnPointB.rotation;
        }
    }

    public void StartBattle()
    {
        if (battleActive) return;

        Debug.Log("���� ����!");

        battleActive = true;
        battleStartTime = Time.time;
        battleCount++;

        // ���� ������ �ʱ�ȭ
        currentBattleData = new BattleData
        {
            agentAName = agentA?.GetAgentName() ?? "Agent A",
            agentBName = agentB?.GetAgentName() ?? "Agent B"
        };

        // ������Ʈ �ʱ�ȭ
        if (agentA != null) agentA.StartBattle();
        if (agentB != null) agentB.StartBattle();

        // ������Ʈ�鿡�� ���� ���� ����
        if (agentA != null && agentB != null)
        {
            agentA.SetEnemy(agentB);
            agentB.SetEnemy(agentA);
        }

        // UI 초기화
        if (uiManager != null)
        {
            uiManager.InitializeBattle(agentA, agentB);
        }
    }

    private void UpdateBattle()
    {
        float battleDuration = Time.time - battleStartTime;

        // �ð� �ʰ� üũ
        if (battleDuration > maxBattleDuration)
        {
            EndBattle(null, "�ð� �ʰ�");
            return;
        }

        // ������Ʈ ������Ʈ
        if (agentA != null && agentA.IsAlive())
        {
            UpdateAgent(agentA, agentB);
        }

        if (agentB != null && agentB.IsAlive())
        {
            UpdateAgent(agentB, agentA);
        }
    }

    private void UpdateAgent(AgentController agent, AgentController enemy)
    {
        if (agent == null || enemy == null) return;

        // ���� ���� ����
        GameObservation obs = new GameObservation
        {
            selfPosition = agent.transform.position,
            enemyPosition = enemy.transform.position,
            selfHP = agent.GetCurrentHP(),
            enemyHP = enemy.GetCurrentHP(),
            cooldowns = agent.GetCooldownState(),
            distanceToEnemy = Vector3.Distance(agent.transform.position, enemy.transform.position),
            currentState = agent.GetCurrentState(),
            arenaCenter = arenaCenter,
            arenaRadius = arenaRadius
        };

        // ������Ʈ ������Ʈ
        agent.UpdateAgent(obs);
    }

    private void CheckBattleEnd()
    {
        if (!battleActive) return;

        bool agentAAlive = agentA != null && agentA.IsAlive();
        bool agentBAlive = agentB != null && agentB.IsAlive();

        if (!agentAAlive && !agentBAlive)
        {
            EndBattle(null, "���º�");
        }
        else if (!agentAAlive)
        {
            EndBattle(agentB, "Agent B �¸�");
        }
        else if (!agentBAlive)
        {
            EndBattle(agentA, "Agent A �¸�");
        }
    }

    private void EndBattle(AgentController winner, string reason)
    {
        if (!battleActive) return;

        battleActive = false;
        float duration = Time.time - battleStartTime;

        Debug.Log($"���� ����: {reason} (���ӽð�: {duration:F2}��)");

        // ���� ������ �ϼ�
        currentBattleData.duration = duration;
        currentBattleData.winner = winner?.GetAgentName() ?? "���º�";
        currentBattleData.agentAFinalHP = agentA?.GetCurrentHP() ?? 0f;
        currentBattleData.agentBFinalHP = agentB?.GetCurrentHP() ?? 0f;

        // ������Ʈ�鿡�� ��� �뺸
        EpisodeResult resultA = new EpisodeResult
        {
            won = winner == agentA,
            finalHP = agentA?.GetCurrentHP() ?? 0f,
            battleDuration = duration,
            agentName = agentA?.GetAgentName() ?? "Agent A",
            enemyName = agentB?.GetAgentName() ?? "Agent B"
        };

        EpisodeResult resultB = new EpisodeResult
        {
            won = winner == agentB,
            finalHP = agentB?.GetCurrentHP() ?? 0f,
            battleDuration = duration,
            agentName = agentB?.GetAgentName() ?? "Agent B",
            enemyName = agentA?.GetAgentName() ?? "Agent A"
        };

        agentA?.OnBattleEnd(resultA);
        agentB?.OnBattleEnd(resultB);

        // UI 결과 표시
        if (uiManager != null)
        {
            uiManager.ShowBattleResult(winner, currentBattleData);
        }

        // ������ ���� (���� CSV ���� ��� �߰� ����)
        SaveBattleData(currentBattleData);
    }

    public void ResetBattle()
    {
        battleActive = false;

        SetupAgents();

        if (agentA != null) agentA.ResetAgent();
        if (agentB != null) agentB.ResetAgent();

        // UI 리셋
        if (uiManager != null)
        {
            uiManager.ResetBattleUI();
        }

        Debug.Log("���� ���� �Ϸ�");
    }

    private void UpdateUI()
    {
        if (battleStatusText != null)
        {
            if (battleActive)
            {
                float duration = Time.time - battleStartTime;
                battleStatusText.text = $"���� ��... ({duration:F1}s)\n" +
                                       $"Agent A HP: {agentA?.GetCurrentHP():F0}\n" +
                                       $"Agent B HP: {agentB?.GetCurrentHP():F0}";
            }
            else
            {
                battleStatusText.text = $"���� ��� ��\n���� Ƚ��: {battleCount}";
            }
        }

        if (startBattleButton != null)
            startBattleButton.interactable = !battleActive;
    }

    private void SaveBattleData(BattleData data)
    {
        Debug.Log($"���� ������: {data.agentAName} vs {data.agentBName} - ����: {data.winner}");
        // TODO: CSV ���Ϸ� �����ϴ� ��� �߰� ����
    }

    /// <summary>
    /// 현재 전투 시간 반환 (UI에서 사용)
    /// </summary>
    public float GetCurrentBattleTime()
    {
        if (!battleActive) return 0f;
        return Time.time - battleStartTime;
    }

    /// <summary>
    /// 전투 활성 상태 반환 (UI에서 사용)
    /// </summary>
    public bool IsBattleActive()
    {
        return battleActive;
    }

    // ����׿� �����
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(arenaCenter, arenaRadius);

        if (spawnPointA != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(spawnPointA.position, 0.5f);
        }

        if (spawnPointB != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(spawnPointB.position, 0.5f);
        }
    }
}