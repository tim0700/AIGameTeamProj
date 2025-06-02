using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BattleManager : MonoBehaviour
{
    [Header("에이전트 설정")]
    public AgentController agentA;
    public AgentController agentB;

    [Header("전투 설정")]
    public float maxBattleDuration = 60f;
    public Transform spawnPointA;
    public Transform spawnPointB;

    [Header("UI 연결")]
    public Text battleStatusText;
    public Button startBattleButton;
    public Button resetBattleButton;

    [Header("아레나 설정")]
    public Vector3 arenaCenter = Vector3.zero;
    public float arenaRadius = 10f;

    // 전투 상태
    private bool battleActive = false;
    private float battleStartTime;
    private int battleCount = 0;

    // 데이터 수집
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

        Debug.Log("전투 시작!");

        battleActive = true;
        battleStartTime = Time.time;
        battleCount++;

        // 전투 데이터 초기화
        currentBattleData = new BattleData
        {
            agentAName = agentA?.GetAgentName() ?? "Agent A",
            agentBName = agentB?.GetAgentName() ?? "Agent B"
        };

        // 에이전트 초기화
        if (agentA != null) agentA.StartBattle();
        if (agentB != null) agentB.StartBattle();

        // 에이전트들에게 상대방 정보 설정
        if (agentA != null && agentB != null)
        {
            agentA.SetEnemy(agentB);
            agentB.SetEnemy(agentA);
        }
    }

    private void UpdateBattle()
    {
        float battleDuration = Time.time - battleStartTime;

        // 시간 초과 체크
        if (battleDuration > maxBattleDuration)
        {
            EndBattle(null, "시간 초과");
            return;
        }

        // 에이전트 업데이트
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

        // 관찰 정보 생성
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

        // 에이전트 업데이트
        agent.UpdateAgent(obs);
    }

    private void CheckBattleEnd()
    {
        if (!battleActive) return;

        bool agentAAlive = agentA != null && agentA.IsAlive();
        bool agentBAlive = agentB != null && agentB.IsAlive();

        if (!agentAAlive && !agentBAlive)
        {
            EndBattle(null, "무승부");
        }
        else if (!agentAAlive)
        {
            EndBattle(agentB, "Agent B 승리");
        }
        else if (!agentBAlive)
        {
            EndBattle(agentA, "Agent A 승리");
        }
    }

    private void EndBattle(AgentController winner, string reason)
    {
        if (!battleActive) return;

        battleActive = false;
        float duration = Time.time - battleStartTime;

        Debug.Log($"전투 종료: {reason} (지속시간: {duration:F2}초)");

        // 전투 데이터 완성
        currentBattleData.duration = duration;
        currentBattleData.winner = winner?.GetAgentName() ?? "무승부";
        currentBattleData.agentAFinalHP = agentA?.GetCurrentHP() ?? 0f;
        currentBattleData.agentBFinalHP = agentB?.GetCurrentHP() ?? 0f;

        // 에이전트들에게 결과 통보
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

        // 데이터 저장 (추후 CSV 저장 기능 추가 예정)
        SaveBattleData(currentBattleData);
    }

    public void ResetBattle()
    {
        battleActive = false;

        SetupAgents();

        if (agentA != null) agentA.ResetAgent();
        if (agentB != null) agentB.ResetAgent();

        Debug.Log("전투 리셋 완료");
    }

    private void UpdateUI()
    {
        if (battleStatusText != null)
        {
            if (battleActive)
            {
                float duration = Time.time - battleStartTime;
                battleStatusText.text = $"전투 중... ({duration:F1}s)\n" +
                                       $"Agent A HP: {agentA?.GetCurrentHP():F0}\n" +
                                       $"Agent B HP: {agentB?.GetCurrentHP():F0}";
            }
            else
            {
                battleStatusText.text = $"전투 대기 중\n전투 횟수: {battleCount}";
            }
        }

        if (startBattleButton != null)
            startBattleButton.interactable = !battleActive;
    }

    private void SaveBattleData(BattleData data)
    {
        Debug.Log($"전투 데이터: {data.agentAName} vs {data.agentBName} - 승자: {data.winner}");
        // TODO: CSV 파일로 저장하는 기능 추가 예정
    }

    // 디버그용 기즈모
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