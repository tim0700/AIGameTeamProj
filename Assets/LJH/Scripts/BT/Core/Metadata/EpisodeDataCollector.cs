using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace LJH.BT
{
    /// <summary>
    /// 에피소드별 에이전트 데이터를 수집하고 CSV로 저장하는 관리 클래스
    /// BattleManager와 연동하여 전투 결과를 자동으로 수집
    /// </summary>
    public class EpisodeDataCollector : MonoBehaviour
    {
        [Header("수집 설정")]
        [SerializeField] private bool enableDataCollection = true;
        [SerializeField] private bool autoSaveOnEpisodeEnd = true;
        [SerializeField] private bool saveDetailedLogs = true;
        
        [Header("저장 설정")]
        [SerializeField] private string customSaveDirectory = "";
        [SerializeField] private bool useBuffering = true;
        [SerializeField] private int bufferSize = 20;
        
        [Header("디버그 설정")]
        [SerializeField] private bool showDebugMessages = true;
        [SerializeField] private bool validateRecords = true;
        
        // 내부 컴포넌트
        private CSVExporter csvExporter;
        private Dictionary<string, ActionTracker> agentTrackers = new Dictionary<string, ActionTracker>();
        private List<AgentSimulationRecord> currentEpisodeRecords = new List<AgentSimulationRecord>();
        
        // 🆕 누적 통계 추적
        private Dictionary<string, AgentCumulativeStats> cumulativeStats = new Dictionary<string, AgentCumulativeStats>();
        
        // 에피소드 정보
        private string currentEpisodeId;
        private float episodeStartTime;
        private bool isEpisodeActive = false;
        private int episodeCounter = 0;
        
        // 통계
        private int totalEpisodesCollected = 0;
        private int totalRecordsSaved = 0;
        private float totalCollectionTime = 0f;
        
        // 이벤트
        public System.Action<string> OnEpisodeStarted;
        public System.Action<string, int> OnEpisodeCompleted;
        public System.Action<AgentSimulationRecord> OnRecordCollected;
        public System.Action<string> OnDataSaved;
        public System.Action<string> OnCollectionError;
        
        #region Unity 생명주기
        
        private void Awake()
        {
            InitializeCollector();
        }
        
        private void Start()
        {
            ValidateSetup();
        }
        
        private void OnDestroy()
        {
            FinalizeCollector();
        }
        
        private void OnApplicationQuit()
        {
            SavePendingData();
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SavePendingData();
            }
        }
        
        #endregion
        
        #region 초기화 및 설정
        
        /// <summary>
        /// 데이터 수집기 초기화
        /// </summary>
        private void InitializeCollector()
        {
            try
            {
                // CSV Exporter 초기화
                csvExporter = new CSVExporter();
                string saveDir = string.IsNullOrEmpty(customSaveDirectory) ? 
                    null : customSaveDirectory;
                
                csvExporter.Initialize(saveDir, useBuffering, bufferSize);
                
                // 이벤트 연결
                csvExporter.OnFileCreated += (path) => LogDebug($"CSV 파일 생성: {path}");
                csvExporter.OnRecordAdded += (agentName) => LogDebug($"레코드 추가: {agentName}");
                csvExporter.OnFileSaved += (path) => OnDataSaved?.Invoke(path);
                csvExporter.OnError += (error) => OnCollectionError?.Invoke(error);
                
                LogDebug("EpisodeDataCollector 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EpisodeDataCollector] 초기화 실패: {ex.Message}");
                OnCollectionError?.Invoke($"초기화 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 설정 유효성 검증
        /// </summary>
        private void ValidateSetup()
        {
            if (!enableDataCollection)
            {
                LogDebug("데이터 수집이 비활성화되어 있습니다.");
                return;
            }
            
            if (bufferSize <= 0)
            {
                bufferSize = 20;
                LogDebug($"버퍼 크기를 기본값으로 설정: {bufferSize}");
            }
            
            LogDebug("EpisodeDataCollector 설정 검증 완료");
        }
        
        #endregion
        
        #region 에이전트 추적 관리
        
        /// <summary>
        /// 에이전트 추적기 등록
        /// </summary>
        /// <param name="agentName">에이전트 이름</param>
        /// <param name="tracker">추적기 (null이면 새로 생성)</param>
        public void RegisterAgent(string agentName, ActionTracker tracker = null)
        {
            if (string.IsNullOrEmpty(agentName))
            {
                LogDebug("에이전트 이름이 비어있습니다.");
                return;
            }
            
            if (tracker == null)
            {
                tracker = new ActionTracker();
            }
            
            agentTrackers[agentName] = tracker;
            LogDebug($"에이전트 등록: {agentName}");
        }
        
        /// <summary>
        /// 에이전트 추적기 제거
        /// </summary>
        /// <param name="agentName">에이전트 이름</param>
        public void UnregisterAgent(string agentName)
        {
            if (agentTrackers.ContainsKey(agentName))
            {
                agentTrackers.Remove(agentName);
                LogDebug($"에이전트 제거: {agentName}");
            }
        }
        
        /// <summary>
        /// 에이전트 추적기 반환
        /// </summary>
        /// <param name="agentName">에이전트 이름</param>
        /// <returns>추적기 (없으면 null)</returns>
        public ActionTracker GetAgentTracker(string agentName)
        {
            return agentTrackers.ContainsKey(agentName) ? agentTrackers[agentName] : null;
        }
        
        /// <summary>
        /// 등록된 모든 에이전트 이름 반환
        /// </summary>
        /// <returns>에이전트 이름 목록</returns>
        public List<string> GetRegisteredAgents()
        {
            return agentTrackers.Keys.ToList();
        }
        
        #endregion
        
        #region 에피소드 관리
        
        /// <summary>
        /// 새 에피소드 시작
        /// </summary>
        /// <param name="episodeId">에피소드 ID (null이면 자동 생성)</param>
        public void StartEpisode(string episodeId = null)
        {
            if (!enableDataCollection)
                return;
            
            // 이전 에피소드 정리
            if (isEpisodeActive)
            {
                LogDebug("이전 에피소드가 아직 활성 상태입니다. 강제 종료합니다.");
                EndEpisode();
            }
            
            // 새 에피소드 설정
            episodeCounter++;
            currentEpisodeId = string.IsNullOrEmpty(episodeId) ? 
                GenerateEpisodeId() : episodeId;
            
            episodeStartTime = Time.time;
            isEpisodeActive = true;
            currentEpisodeRecords.Clear();
            
            // 모든 추적기 리셋
            foreach (var tracker in agentTrackers.Values)
            {
                tracker.Reset();
            }
            
            LogDebug($"에피소드 시작: {currentEpisodeId}");
            OnEpisodeStarted?.Invoke(currentEpisodeId);
        }
        
        /// <summary>
        /// 현재 에피소드 종료
        /// </summary>
        /// <param name="battleResults">전투 결과 정보</param>
        public void EndEpisode(Dictionary<string, BattleResult> battleResults = null)
        {
            if (!enableDataCollection || !isEpisodeActive)
                return;
            
            try
            {
                float episodeDuration = Time.time - episodeStartTime;
                int recordCount = 0;
                
                // 각 에이전트의 레코드 생성
                foreach (var kvp in agentTrackers)
                {
                    string agentName = kvp.Key;
                    ActionTracker tracker = kvp.Value;
                    
                    // 전투 결과 정보 가져오기
                    BattleResult result = battleResults?.ContainsKey(agentName) == true ? 
                        battleResults[agentName] : new BattleResult();
                    
                    // 🆕 누적 통계 업데이트
                    UpdateCumulativeStats(agentName, result.winResult > 0.5f);
                    var agentStats = GetCumulativeStats(agentName);
                    
                    // 시뮬레이션 레코드 생성
                    var record = AgentSimulationRecord.Create(
                        currentEpisodeId,
                        agentName,
                        result.agentType,
                        result.winResult,
                        result.finalHP,
                        result.initialHP,
                        episodeDuration,
                        result.enemyAgentName,
                        tracker,
                        agentStats.totalRounds,
                        agentStats.totalWins
                    );
                    
                    // 유효성 검증
                    if (validateRecords && !record.IsValid())
                    {
                        LogDebug($"유효하지 않은 레코드: {agentName}");
                        continue;
                    }
                    
                    currentEpisodeRecords.Add(record);
                    recordCount++;
                    
                    OnRecordCollected?.Invoke(record);
                    LogDebug($"레코드 수집: {agentName} - {record.GetSummary()}");
                }
                
                // CSV 저장
                if (autoSaveOnEpisodeEnd && recordCount > 0)
                {
                    SaveCurrentEpisodeData();
                }
                
                // 통계 업데이트
                totalEpisodesCollected++;
                totalRecordsSaved += recordCount;
                totalCollectionTime += episodeDuration;
                
                LogDebug($"에피소드 종료: {currentEpisodeId} (지속시간: {episodeDuration:F1}초, 레코드: {recordCount}개)");
                OnEpisodeCompleted?.Invoke(currentEpisodeId, recordCount);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EpisodeDataCollector] 에피소드 종료 처리 실패: {ex.Message}");
                OnCollectionError?.Invoke($"에피소드 종료 실패: {ex.Message}");
            }
            finally
            {
                isEpisodeActive = false;
            }
        }
        
        /// <summary>
        /// 에피소드 ID 자동 생성
        /// </summary>
        /// <returns>생성된 에피소드 ID</returns>
        private string GenerateEpisodeId()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"EP_{timestamp}_{episodeCounter:D4}";
        }
        
        #endregion
        
        #region 데이터 저장
        
        /// <summary>
        /// 현재 에피소드 데이터 저장
        /// </summary>
        public void SaveCurrentEpisodeData()
        {
            if (currentEpisodeRecords.Count == 0)
            {
                LogDebug("저장할 레코드가 없습니다.");
                return;
            }
            
            try
            {
                csvExporter.AddRecords(currentEpisodeRecords);
                LogDebug($"에피소드 데이터 저장: {currentEpisodeRecords.Count}개 레코드");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EpisodeDataCollector] 데이터 저장 실패: {ex.Message}");
                OnCollectionError?.Invoke($"데이터 저장 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 대기 중인 모든 데이터 저장
        /// </summary>
        public void SavePendingData()
        {
            if (!enableDataCollection)
                return;
            
            try
            {
                // 현재 에피소드 저장
                if (isEpisodeActive && currentEpisodeRecords.Count > 0)
                {
                    SaveCurrentEpisodeData();
                }
                
                // CSV 버퍼 플러시
                csvExporter?.FlushBuffer();
                
                LogDebug("대기 중인 데이터 저장 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EpisodeDataCollector] 대기 데이터 저장 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 수동 데이터 플러시
        /// </summary>
        public void FlushData()
        {
            csvExporter?.FlushBuffer();
            LogDebug("데이터 수동 플러시 완료");
        }
        
        #endregion
        
        #region BattleManager 연동 메서드
        
        /// <summary>
        /// BattleManager에서 전투 시작 시 호출
        /// </summary>
        /// <param name="agentA">에이전트 A</param>
        /// <param name="agentB">에이전트 B</param>
        public void OnBattleStarted(AgentController agentA, AgentController agentB)
        {
            if (!enableDataCollection)
                return;
            
            // 🔧 에이전트 등록 (개선된 방식)
            if (agentA != null)
            {
                var trackerA = GetOrCreateActionTracker(agentA);
                RegisterAgent(agentA.GetAgentName(), trackerA);
                LogDebug($"에이전트 A 등록: {agentA.GetAgentName()} - Tracker: {(trackerA != null ? "OK" : "NULL")}");
            }
            
            if (agentB != null)
            {
                var trackerB = GetOrCreateActionTracker(agentB);
                RegisterAgent(agentB.GetAgentName(), trackerB);
                LogDebug($"에이전트 B 등록: {agentB.GetAgentName()} - Tracker: {(trackerB != null ? "OK" : "NULL")}");
            }
            
            // 에피소드 시작
            StartEpisode();
            
            LogDebug($"전투 시작 처리: {agentA?.GetAgentName()} vs {agentB?.GetAgentName()}");
        }
        
        /// <summary>
        /// 🔧 AgentController에서 ActionTracker 가져오거나 생성
        /// </summary>
        /// <param name="agentController">에이전트 컨트롤러</param>
        /// <returns>ActionTracker 인스턴스</returns>
        private ActionTracker GetOrCreateActionTracker(AgentController agentController)
        {
            if (agentController == null)
            {
                LogDebug("⚠️ AgentController가 null입니다.");
                return new ActionTracker();
            }
            
            // BTAgentBase에서 ActionTracker 가져오기 시도
            var btAgent = agentController.GetComponent<BTAgentBase>();
            if (btAgent != null)
            {
                var tracker = btAgent.GetActionTracker();
                if (tracker != null)
                {
                    LogDebug($"{agentController.GetAgentName()}의 기존 ActionTracker 사용");
                    return tracker;
                }
                else
                {
                    LogDebug($"⚠️ {agentController.GetAgentName()}의 ActionTracker가 null이므로 새로 생성");
                }
            }
            else
            {
                LogDebug($"⚠️ {agentController.GetAgentName()}에 BTAgentBase 컬포넌트가 없습니다.");
            }
            
            // 새 ActionTracker 생성
            LogDebug($"{agentController.GetAgentName()}용 새 ActionTracker 생성");
            return new ActionTracker();
        }
        
        /// <summary>
        /// BattleManager에서 전투 종료 시 호출
        /// </summary>
        /// <param name="winner">승리자</param>
        /// <param name="battleData">전투 데이터</param>
        public void OnBattleEnded(AgentController winner, BattleManager.BattleData battleData)
        {
            if (!enableDataCollection)
                return;
            
            // 전투 결과 정리
            var battleResults = new Dictionary<string, BattleResult>();
            
            // 🔧 에이전트 A 결과 (개선된 방식)
            if (!string.IsNullOrEmpty(battleData.agentAName))
            {
                string agentAType = GetAgentType(battleData.agentAName);
                
                battleResults[battleData.agentAName] = new BattleResult
                {
                    agentType = agentAType,
                    winResult = battleData.winner == battleData.agentAName ? 1f : 0f,
                    finalHP = battleData.agentAFinalHP,
                    initialHP = 100f, // 기본값, 실제로는 BattleData에서 가져와야 함
                    enemyAgentName = battleData.agentBName
                };
                
                LogDebug($"에이전트 A 데이터 수집: {battleData.agentAName} ({agentAType})");
            }
            
            // 🔧 에이전트 B 결과 (개선된 방식)
            if (!string.IsNullOrEmpty(battleData.agentBName))
            {
                string agentBType = GetAgentType(battleData.agentBName);
                
                battleResults[battleData.agentBName] = new BattleResult
                {
                    agentType = agentBType,
                    winResult = battleData.winner == battleData.agentBName ? 1f : 0f,
                    finalHP = battleData.agentBFinalHP,
                    initialHP = 100f, // 기본값
                    enemyAgentName = battleData.agentAName
                };
                
                LogDebug($"에이전트 B 데이터 수집: {battleData.agentBName} ({agentBType})");
            }
            
            // 에피소드 종료
            EndEpisode(battleResults);
            
            LogDebug($"전투 종료 처리: 승리자 = {battleData.winner}");
        }
        
        /// <summary>
        /// 🔧 에이전트 이름으로 AgentType 찾기
        /// </summary>
        /// <param name="agentName">에이전트 이름</param>
        /// <returns>에이전트 타입</returns>
        private string GetAgentType(string agentName)
        {
            // 등록된 에이전트에서 BTAgentBase 찾기
            foreach (var kvp in agentTrackers)
            {
                if (kvp.Key == agentName)
                {
                    // 에이전트 이름으로 GameObject 찾기
                    var agentObject = GameObject.Find(agentName);
                    if (agentObject != null)
                    {
                        var btAgent = agentObject.GetComponent<BTAgentBase>();
                        if (btAgent != null)
                        {
                            LogDebug($"{agentName}의 AgentType: {btAgent.GetAgentType()}");
                            return btAgent.GetAgentType();
                        }
                        
                        // AgentController에서 찾기
                        var agentController = agentObject.GetComponent<AgentController>();
                        if (agentController != null)
                        {
                            var btAgentFromController = agentController.GetComponent<BTAgentBase>();
                            if (btAgentFromController != null)
                            {
                                LogDebug($"{agentName}의 AgentType (Controller에서): {btAgentFromController.GetAgentType()}");
                                return btAgentFromController.GetAgentType();
                            }
                        }
                    }
                    break;
                }
            }
            
            // 기본값 반환 및 경고 로그
            LogDebug($"⚠️ {agentName}의 AgentType을 찾을 수 없어 'Unknown'을 반환합니다.");
            return "Unknown";
        }
        
        /// <summary>
        /// 🆕 에이전트의 누적 통계 업데이트
        /// </summary>
        /// <param name="agentName">에이전트 이름</param>
        /// <param name="won">승리 여부</param>
        private void UpdateCumulativeStats(string agentName, bool won)
        {
            if (!cumulativeStats.ContainsKey(agentName))
            {
                cumulativeStats[agentName] = new AgentCumulativeStats(agentName);
            }
            
            var stats = cumulativeStats[agentName];
            stats.totalRounds++;
            
            if (won)
            {
                stats.totalWins++;
            }
            
            // 승률 업데이트
            stats.winRate = stats.totalRounds > 0 ? ((float)stats.totalWins / stats.totalRounds) * 100f : 0f;
            
            LogDebug($"📊 {agentName} 누적 통계: {stats.totalWins}/{stats.totalRounds} ({stats.winRate:F1}%)");
        }
        
        /// <summary>
        /// 🆕 에이전트의 누적 통계 반환
        /// </summary>
        /// <param name="agentName">에이전트 이름</param>
        /// <returns>누적 통계</returns>
        private AgentCumulativeStats GetCumulativeStats(string agentName)
        {
            if (!cumulativeStats.ContainsKey(agentName))
            {
                cumulativeStats[agentName] = new AgentCumulativeStats(agentName);
            }
            
            return cumulativeStats[agentName];
        }
        
        #endregion
        
        #region 통계 및 정보
        
        /// <summary>
        /// 수집기 통계 반환
        /// </summary>
        /// <returns>통계 정보</returns>
        public CollectorStats GetStats()
        {
            var csvStats = csvExporter?.GetStats();
            
            return new CollectorStats
            {
                totalEpisodesCollected = totalEpisodesCollected,
                totalRecordsSaved = totalRecordsSaved,
                totalCollectionTime = totalCollectionTime,
                registeredAgentsCount = agentTrackers.Count,
                isEpisodeActive = isEpisodeActive,
                currentEpisodeId = currentEpisodeId,
                currentRecordsCount = currentEpisodeRecords.Count,
                csvFilesCount = csvStats?.totalCSVFiles ?? 0,
                bufferCount = csvStats?.bufferCount ?? 0
            };
        }
        
        /// <summary>
        /// 🆕 누적 통계 요약 반환
        /// </summary>
        /// <returns>누적 통계 요약</returns>
        public string GetCumulativeStatsSummary()
        {
            if (cumulativeStats.Count == 0)
                return "누적 통계 없음";
            
            var summary = "📊 누적 통계 요약\n";
            foreach (var kvp in cumulativeStats)
            {
                var stats = kvp.Value;
                summary += $"{stats.agentName}: {stats.totalWins}/{stats.totalRounds} 라운드 ({stats.winRate:F1}% 승률)\n";
            }
            
            return summary;
        }
        
        /// <summary>
        /// 🆕 특정 에이전트의 누적 통계 반환
        /// </summary>
        /// <param name="agentName">에이전트 이름</param>
        /// <returns>누적 통계 문자열</returns>
        public string GetAgentCumulativeStats(string agentName)
        {
            if (cumulativeStats.ContainsKey(agentName))
            {
                var stats = cumulativeStats[agentName];
                return $"{agentName}\n" +
                       $"누적 라운드: {stats.totalRounds}\n" +
                       $"누적 승리: {stats.totalWins}\n" +
                       $"승률: {stats.winRate:F1}%";
            }
            
            return $"{agentName}: 누적 통계 없음";
        }
        
        /// <summary>
        /// 현재 에피소드 정보 반환
        /// </summary>
        /// <returns>에피소드 정보</returns>
        public string GetCurrentEpisodeInfo()
        {
            if (!isEpisodeActive)
                return "활성 에피소드 없음";
            
            float duration = Time.time - episodeStartTime;
            return $"에피소드: {currentEpisodeId}\n" +
                   $"지속시간: {duration:F1}초\n" +
                   $"등록된 에이전트: {agentTrackers.Count}개\n" +
                   $"수집된 레코드: {currentEpisodeRecords.Count}개";
        }
        
        #endregion
        
        #region 유틸리티
        
        /// <summary>
        /// 디버그 메시지 출력
        /// </summary>
        /// <param name="message">메시지</param>
        private void LogDebug(string message)
        {
            if (showDebugMessages)
            {
                Debug.Log($"[EpisodeDataCollector] {message}");
            }
        }
        
        /// <summary>
        /// 수집기 종료 처리
        /// </summary>
        private void FinalizeCollector()
        {
            SavePendingData();
            csvExporter?.Finalize();
            LogDebug("EpisodeDataCollector 종료");
        }
        
        /// <summary>
        /// 설정 업데이트
        /// </summary>
        /// <param name="enableCollection">수집 활성화</param>
        /// <param name="autoSave">자동 저장</param>
        /// <param name="debugMessages">디버그 메시지</param>
        public void UpdateSettings(bool? enableCollection = null, bool? autoSave = null, bool? debugMessages = null)
        {
            if (enableCollection.HasValue)
                enableDataCollection = enableCollection.Value;
            
            if (autoSave.HasValue)
                autoSaveOnEpisodeEnd = autoSave.Value;
            
            if (debugMessages.HasValue)
                showDebugMessages = debugMessages.Value;
            
            LogDebug($"설정 업데이트: Collection={enableDataCollection}, AutoSave={autoSaveOnEpisodeEnd}");
        }
        
        #endregion
    }
    
    /// <summary>
    /// 전투 결과 정보
    /// </summary>
    [System.Serializable]
    public struct BattleResult
    {
        public string agentType;
        public float winResult;
        public float finalHP;
        public float initialHP;
        public string enemyAgentName;
    }
    
    /// <summary>
    /// 수집기 통계 정보
    /// </summary>
    [System.Serializable]
    public struct CollectorStats
    {
        public int totalEpisodesCollected;
        public int totalRecordsSaved;
        public float totalCollectionTime;
        public int registeredAgentsCount;
        public bool isEpisodeActive;
        public string currentEpisodeId;
        public int currentRecordsCount;
        public int csvFilesCount;
        public int bufferCount;
        
        public override string ToString()
        {
            return $"Collector Stats:\n" +
                   $"에피소드: {totalEpisodesCollected}개\n" +
                   $"레코드: {totalRecordsSaved}개\n" +
                   $"에이전트: {registeredAgentsCount}개\n" +
                   $"CSV 파일: {csvFilesCount}개\n" +
                   $"현재 상태: {(isEpisodeActive ? "진행중" : "대기중")}";
        }
    }
    
    /// <summary>
    /// 🆕 에이전트 누적 통계 정보
    /// </summary>
    [System.Serializable]
    public class AgentCumulativeStats
    {
        public string agentName;
        public int totalRounds;
        public int totalWins;
        public float winRate; // 승률 (%)
        
        public AgentCumulativeStats(string name)
        {
            agentName = name;
            totalRounds = 0;
            totalWins = 0;
            winRate = 0f;
        }
        
        public override string ToString()
        {
            return $"{agentName}: {totalWins}/{totalRounds} ({winRate:F1}%)";
        }
    }
}
