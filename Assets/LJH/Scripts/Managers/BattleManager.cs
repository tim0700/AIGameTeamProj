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
    
    [Header("ML 학습 설정")]
    [Tooltip("체크하면 전투 종료 후 자동으로 다음 라운드 시작 (ML 학습용)")]
    public bool isTrainingMode = false;
    [Tooltip("자동 재시작까지의 딜레이 시간 (초)")]
    public float autoRestartDelay = 2f;
    [Tooltip("최대 연속 전투 횟수 (0 = 무제한)")]
    public int maxTrainingEpisodes = 0;
    
    [Header("🆕 데이터 수집 설정")]
    [Tooltip("CSV 데이터 수집 활성화")]
    public bool enableDataCollection = true;
    [Tooltip("각 에피소드마다 자동 저장")]
    public bool autoSavePerEpisode = true;
    [Tooltip("실시간 통계 표시")]
    public bool showRealTimeStats = true;

    // ���� ����
    private bool battleActive = false;
    private float battleStartTime;
    private int battleCount = 0;

    // ������ ����
    private BattleData currentBattleData;
    
    // 🆕 데이터 수집 시스템
    private EpisodeDataCollector dataCollector;

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
        
        // 🆕 데이터 수집 시스템 초기화
        InitializeDataCollection();

        if (isTrainingMode)      // ML 학습용 씬이라면
            StartBattle();
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
    
    /// <summary>
    /// 🆕 데이터 수집 시스템 초기화
    /// </summary>
    private void InitializeDataCollection()
    {
        if (!enableDataCollection)
        {
            Debug.Log("[🃀 BattleManager] 데이터 수집이 비활성화되어 있습니다.");
            return;
        }
        
        // EpisodeDataCollector 컬포넌트 추가/찾기
        dataCollector = GetComponent<EpisodeDataCollector>();
        if (dataCollector == null)
        {
            dataCollector = gameObject.AddComponent<EpisodeDataCollector>();
            Debug.Log("[🃀 BattleManager] EpisodeDataCollector 컬포넌트가 자동으로 추가되었습니다.");
        }
        
        // 이벤트 연결
        dataCollector.OnEpisodeStarted += (episodeId) => 
        {
            if (showRealTimeStats)
                Debug.Log($"[🃀 BattleManager] 에피소드 시작: {episodeId}");
        };
        
        dataCollector.OnEpisodeCompleted += (episodeId, recordCount) => 
        {
            if (showRealTimeStats)
                Debug.Log($"[🃀 BattleManager] 에피소드 완료: {episodeId} ({recordCount}개 레코드)");
        };
        
        dataCollector.OnDataSaved += (filePath) => 
        {
            Debug.Log($"[🃀 BattleManager] CSV 데이터 저장 완료: {filePath}");
        };
        
        dataCollector.OnCollectionError += (error) => 
        {
            Debug.LogError($"[🃀 BattleManager] 데이터 수집 오류: {error}");
        };
        
        Debug.Log("[🃀 BattleManager] 데이터 수집 시스템 초기화 완료");
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
        
        // 🆕 데이터 수집 시작
        if (enableDataCollection && dataCollector != null)
        {
            dataCollector.OnBattleStarted(agentA, agentB);
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

        // var rl = agentA.GetComponent<RLAgentBase>();   // A가 RL_AttackAgent
        // RLAgentBase rl = agentA.GetComponent<RLAgentBase>();
        // if (rl == null)
        //     rl = agentB.GetComponent<RLAgentBase>();   // 둘 중 RLAgentBase 가진 쪽 선택

        // if (winner == agentA) rl?.Win();               // 승
        // else if (winner == agentB) rl?.Lose();              // 패
        // else rl?.Draw();              // 둘 다 죽음(무승부)


        // UI 결과 표시
        if (uiManager != null)
        {
            uiManager.ShowBattleResult(winner, currentBattleData);
        }

        // 🆕 데이터 수집 종료 및 CSV 저장
        if (enableDataCollection && dataCollector != null)
        {
            dataCollector.OnBattleEnded(winner, currentBattleData);
        }
        
        // 기존 데이터 저장 (데이터 수집이 비활성화된 경우)
        if (!enableDataCollection)
        {
            SaveBattleData(currentBattleData);
        }
        
        // 🤖 ML 학습 모드에서 자동 재시작
        if (isTrainingMode)
        {
            // 최대 에피소드 수 체크
            if (maxTrainingEpisodes > 0 && battleCount >= maxTrainingEpisodes)
            {
                Debug.Log($"ML 학습 완료: {battleCount}번의 전투 완료");
                isTrainingMode = false; // 학습 모드 종료
            }
            else
            {
                Debug.Log($"ML 학습 모드: {autoRestartDelay}초 후 자동 재시작... ({battleCount}/{(maxTrainingEpisodes > 0 ? maxTrainingEpisodes.ToString() : "∞")})");
                StartCoroutine(AutoRestartBattle());
            }
        }
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
    /// 🤖 ML 학습용 자동 재시작 코루틴
    /// </summary>
    private IEnumerator AutoRestartBattle()
    {
        // 딜레이 대기
        yield return new WaitForSeconds(autoRestartDelay);
        
        // 여전히 학습 모드인지 확인 (중간에 변경될 수 있음)
        if (!isTrainingMode)
        {
            Debug.Log("학습 모드가 비활성화되어 자동 재시작을 취소합니다.");
            yield break;
        }
        
        // 만약 전투가 아직 진행 중이면 재시작하지 않음
        if (battleActive)
        {
            Debug.LogWarning("전투가 아직 진행 중이므로 재시작을 건너띷니다.");
            yield break;
        }
        
        Debug.Log("🔄 ML 학습 모드: 자동 전투 재시작!");
        
        // 리셋 후 시작
        ResetBattle();
        yield return new WaitForSeconds(0.5f); // 리셋이 완료될 때까지 짧은 대기
        StartBattle();
    }
    
    /// <summary>
    /// ML 학습 모드 수동 제어 메서드
    /// </summary>
    public void SetTrainingMode(bool enable)
    {
        isTrainingMode = enable;
        Debug.Log($"ML 학습 모드: {(enable ? "활성화" : "비활성화")}");
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
    
    /// <summary>
    /// 🆕 데이터 수집기 통계 반환
    /// </summary>
    /// <returns>수집기 통계</returns>
    public string GetDataCollectionStats()
    {
        if (!enableDataCollection || dataCollector == null)
            return "데이터 수집 비활성화";
        
        var stats = dataCollector.GetStats();
        return $"수집된 에피소드: {stats.totalEpisodesCollected}\n" +
               $"저장된 레코드: {stats.totalRecordsSaved}\n" +
               $"CSV 파일: {stats.csvFilesCount}개";
    }
    
    /// <summary>
    /// 🆕 데이터 수집 설정 업데이트
    /// </summary>
    /// <param name="enableCollection">데이터 수집 활성화</param>
    /// <param name="autoSave">자동 저장</param>
    /// <param name="showStats">실시간 통계 표시</param>
    public void UpdateDataCollectionSettings(bool? enableCollection = null, bool? autoSave = null, bool? showStats = null)
    {
        if (enableCollection.HasValue)
        {
            enableDataCollection = enableCollection.Value;
            
            if (enableDataCollection && dataCollector == null)
            {
                InitializeDataCollection();
            }
        }
        
        if (autoSave.HasValue)
            autoSavePerEpisode = autoSave.Value;
            
        if (showStats.HasValue)
            showRealTimeStats = showStats.Value;
            
        Debug.Log($"[🃀 BattleManager] 데이터 수집 설정 업데이트: Collection={enableDataCollection}, AutoSave={autoSavePerEpisode}, ShowStats={showRealTimeStats}");
    }
    
    /// <summary>
    /// 🆕 수동 데이터 저장
    /// </summary>
    public void ManualSaveData()
    {
        if (enableDataCollection && dataCollector != null)
        {
            dataCollector.SavePendingData();
            Debug.Log("[🃀 BattleManager] 수동 데이터 저장 완료");
        }
        else
        {
            Debug.LogWarning("[🃀 BattleManager] 데이터 수집이 비활성화되어 있습니다.");
        }
    }
    
    /// <summary>
    /// 🆕 누적 통계 요약 반환
    /// </summary>
    /// <returns>누적 통계 문자열</returns>
    public string GetCumulativeStatsForUI()
    {
        if (!enableDataCollection || dataCollector == null)
            return "데이터 수집 비활성화";
        
        return dataCollector.GetCumulativeStatsSummary();
    }
    
    /// <summary>
    /// 🆕 에이전트별 누적 통계 반환
    /// </summary>
    /// <param name="agentName">에이전트 이름</param>
    /// <returns>누적 통계</returns>
    public string GetAgentCumulativeStats(string agentName)
    {
        if (!enableDataCollection || dataCollector == null)
            return "데이터 수집 비활성화";
        
        return dataCollector.GetAgentCumulativeStats(agentName);
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