using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using LJH.BT;

/// <summary>
/// ML-Agents 호환성이 개선된 배틀 매니저
/// 일반 씬과 ML-Agents 환경 모두에서 동작
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("에이전트 설정")]
    public AgentController agentA;
    public AgentController agentB;

    [Header("전투 설정")]
    public float maxBattleDuration = 60f;
    public Transform spawnPointA;
    public Transform spawnPointB;

    [Header("UI 설정")]
    public Text battleStatusText;
    public Button startBattleButton;
    public Button resetBattleButton;
    public BattleUIManager uiManager;

    [Header("아레나 설정")]
    public Vector3 arenaCenter = Vector3.zero;
    public float arenaRadius = 10f;
    
    [Header("ML-Agents 호환성")]
    public bool autoDetectMLAgents = true;
    public bool enableDebugLogs = false;
    
    // 전투 상태
    private bool battleActive = false;
    private float battleStartTime;
    private int battleCount = 0;

    // 배틀 데이터
    private BattleData currentBattleData;
    
    // ML-Agents 호환성
    private bool isMLAgentsEnvironment = false;
    private PlayerRLAgent detectedRLAgent;
    private PlayerRLAgent_Defense detectedRLDefenseAgent;
    private int frameCounter = 0;

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
        InitializeManager();
        SetupUI();
        SetupAgents();
    }

    void Update()
    {
        frameCounter++;
        
        // ML-Agents 환경에서는 BT 에이전트만 수동으로 업데이트
        if (isMLAgentsEnvironment)
        {
            UpdateBTAgentsInMLEnvironment();
        }
        // 일반 환경에서는 기존 방식 사용
        else if (battleActive)
        {
            UpdateBattle();
            CheckBattleEnd();
        }

        UpdateUI();
    }

    #region 초기화

    /// <summary>
    /// 매니저 초기화 및 ML-Agents 환경 감지
    /// </summary>
    private void InitializeManager()
    {
        if (autoDetectMLAgents)
        {
            DetectMLAgentsEnvironment();
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"[BattleManager] 초기화 완료 - ML-Agents 환경: {isMLAgentsEnvironment}");
        }
    }

    /// <summary>
    /// ML-Agents 환경 자동 감지 (Academy 의존성 제거)
    /// </summary>
    private void DetectMLAgentsEnvironment()
    {
        // PlayerRLAgent 탐지 (최신 Unity API 사용)
        detectedRLAgent = FindFirstObjectByType<PlayerRLAgent>();
        detectedRLDefenseAgent = FindFirstObjectByType<PlayerRLAgent_Defense>();
        
        // PlayerRLAgent가 존재하고 플레이 모드면 ML-Agents 환경으로 간주
        isMLAgentsEnvironment = (detectedRLAgent != null || detectedRLDefenseAgent != null) && Application.isPlaying;
        
        if (isMLAgentsEnvironment)
        {
            AdjustArenaSettingsForMLAgents();
            
            if (enableDebugLogs)
            {
                Debug.Log($"[BattleManager] ML-Agents 환경 감지됨 - 아레나 설정 조정: {arenaCenter}, {arenaRadius}");
            }
        }
        else if (enableDebugLogs)
        {
            Debug.Log("[BattleManager] 일반 환경 감지됨");
        }
    }



    /// <summary>
    /// ML-Agents 환경에 맞게 아레나 설정 조정
    /// </summary>
    private void AdjustArenaSettingsForMLAgents()
    {
        // PlayerRLAgent의 arenaHalf를 기준으로 설정
        if (detectedRLAgent != null && detectedRLAgent.arenaCenter != null)
        {
            arenaCenter = detectedRLAgent.arenaCenter.position;
            arenaRadius = 15f; // PlayerRLAgent의 arenaHalf와 일치
        }
        else
        {
            // 기본값 사용
            arenaRadius = 15f;
        }
    }

    #endregion

    #region ML-Agents 환경 처리

    /// <summary>
    /// ML-Agents 환경에서 BT 에이전트들만 업데이트
    /// </summary>
    private void UpdateBTAgentsInMLEnvironment()
    {
        // BT 에이전트만 찾아서 업데이트
        if (agentA != null && IsBTAgent(agentA))
        {
            UpdateBTAgentSafely(agentA);
        }
        
        if (agentB != null && IsBTAgent(agentB))
        {
            UpdateBTAgentSafely(agentB);
        }
    }

    /// <summary>
    /// 안전하게 BT 에이전트 업데이트
    /// </summary>
    private void UpdateBTAgentSafely(AgentController btAgent)
    {
        try
        {
            // 상대방 찾기 (ML 에이전트)
            AgentController opponent = FindOpponentForBTAgent(btAgent);
            
            if (opponent != null && btAgent.IsAlive())
            {
                // ML-Agents 호환 GameObservation 생성
                GameObservation obs = CreateMLCompatibleObservation(btAgent, opponent);
                
                // BT 에이전트 업데이트
                btAgent.UpdateAgent(obs);
                
                // 디버그 로깅 (60프레임마다)
                if (enableDebugLogs && frameCounter % 60 == 0)
                {
                    LogBTAgentStatus(btAgent, obs);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[BattleManager] BT 에이전트 업데이트 실패: {ex.Message}");
        }
    }

    /// <summary>
    /// BT 에이전트인지 확인
    /// </summary>
    private bool IsBTAgent(AgentController agent)
    {
        if (agent == null) return false;
        
        // BTAgentBase 컴포넌트가 있는지 확인
        var btComponent = agent.GetComponent<BTAgentBase>();
        return btComponent != null;
    }

    /// <summary>
    /// BT 에이전트의 상대방 찾기
    /// </summary>
    private AgentController FindOpponentForBTAgent(AgentController btAgent)
    {
        // 먼저 설정된 상대방 확인
        if (agentA == btAgent && agentB != null) return agentB;
        if (agentB == btAgent && agentA != null) return agentA;
        
        // PlayerRL 컴포넌트를 가진 에이전트 찾기 (최신 API)
        var allAgents = FindObjectsByType<AgentController>(FindObjectsSortMode.None);
        foreach (var agent in allAgents)
        {
            if (agent != btAgent && agent.GetComponent<PlayerRL>() != null)
            {
                return agent;
            }
        }
        
        return null;
    }

    /// <summary>
    /// ML-Agents 호환 GameObservation 생성
    /// </summary>
    private GameObservation CreateMLCompatibleObservation(AgentController self, AgentController enemy)
    {
        // 아레나 데이터 유효성 검사
        Vector3 validArenaCenter = arenaCenter;
        float validArenaRadius = arenaRadius;
        
        // 아레나 데이터가 무효한 경우 기본값 사용
        if (validArenaCenter == Vector3.zero && validArenaRadius <= 0.1f)
        {
            validArenaCenter = Vector3.zero;
            validArenaRadius = 15f;
            
            if (enableDebugLogs)
            {
                Debug.LogWarning("[BattleManager] 아레나 데이터 무효, 기본값 사용");
            }
        }
        
        return new GameObservation
        {
            selfPosition = self.transform.position,
            enemyPosition = enemy.transform.position,
            selfHP = self.GetCurrentHP(),
            enemyHP = enemy.GetCurrentHP(),
            cooldowns = self.GetCooldownState(),
            distanceToEnemy = Vector3.Distance(self.transform.position, enemy.transform.position),
            currentState = self.GetCurrentState(),
            arenaCenter = validArenaCenter,    // ✅ ML-Agents와 일치
            arenaRadius = validArenaRadius     // ✅ ML-Agents와 일치
        };
    }

    /// <summary>
    /// BT 에이전트 상태 로깅
    /// </summary>
    private void LogBTAgentStatus(AgentController btAgent, GameObservation obs)
    {
        float distanceFromCenter = Vector3.Distance(obs.selfPosition, obs.arenaCenter);
        float normalizedDistance = distanceFromCenter / obs.arenaRadius;
        
        Debug.Log($"[BattleManager] {btAgent.name} - " +
                 $"위치: {obs.selfPosition}, " +
                 $"중심거리: {distanceFromCenter:F2}/{obs.arenaRadius:F2} ({normalizedDistance:P1}), " +
                 $"HP: {obs.selfHP:F0}, " +
                 $"적거리: {obs.distanceToEnemy:F2}");
    }

    #endregion

    #region 기존 전투 시스템 (일반 환경용)

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

        // 에이전트들에게 적의 정보 전달
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

        // 게임 상황 생성
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

        // UI 결과 표시
        if (uiManager != null)
        {
            uiManager.ShowBattleResult(winner, currentBattleData);
        }

        // 데이터 저장
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

        Debug.Log("전투 초기화 완료");
    }

    #endregion

    #region UI 및 유틸리티

    private void UpdateUI()
    {
        if (battleStatusText != null)
        {
            if (isMLAgentsEnvironment)
            {
                // ML-Agents 환경에서는 다른 정보 표시
                battleStatusText.text = $"ML-Agents 환경\n프레임: {frameCounter}\n" +
                                       $"Agent A HP: {agentA?.GetCurrentHP():F0}\n" +
                                       $"Agent B HP: {agentB?.GetCurrentHP():F0}";
            }
            else if (battleActive)
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
            startBattleButton.interactable = !battleActive && !isMLAgentsEnvironment;
    }

    private void SaveBattleData(BattleData data)
    {
        Debug.Log($"전투 데이터: {data.agentAName} vs {data.agentBName} - 결과: {data.winner}");
    }

    /// <summary>
    /// 현재 전투 시간 반환
    /// </summary>
    public float GetCurrentBattleTime()
    {
        if (!battleActive) return 0f;
        return Time.time - battleStartTime;
    }

    /// <summary>
    /// 전투 활성 상태 반환
    /// </summary>
    public bool IsBattleActive()
    {
        return battleActive;
    }

    /// <summary>
    /// ML-Agents 환경 여부 반환
    /// </summary>
    public bool IsMLAgentsEnvironment()
    {
        return isMLAgentsEnvironment;
    }

    #endregion

    #region Unity Editor 지원

    void OnDrawGizmos()
    {
        // 아레나 경계
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(arenaCenter, arenaRadius);
        
        // 안전 구역 (75%)
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(arenaCenter, arenaRadius * 0.75f);
        
        // 위험 구역 (95%)
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawWireSphere(arenaCenter, arenaRadius * 0.95f);

        // 스폰 포인트
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
        
        // ML-Agents 환경 표시
        if (isMLAgentsEnvironment)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(arenaCenter + Vector3.up * 2f, Vector3.one);
        }
    }

    #endregion
}
