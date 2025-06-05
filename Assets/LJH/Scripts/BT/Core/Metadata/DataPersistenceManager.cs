using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace LJH.BT
{
    /// <summary>
    /// BT 시스템의 데이터 영속성 관리자
    /// JSON 기반 직렬화, 에피소드별 데이터 저장, 장기간 성능 추적, 데이터 압축 및 정리 기능 제공
    /// BTTreeStatistics, PerformanceMonitor, MLOptimizationManager와 연동하여 종합적인 데이터 관리
    /// </summary>
    public class DataPersistenceManager : MonoBehaviour
    {
        [Header("데이터 저장 설정")]
        [SerializeField] private bool enableDataPersistence = true;
        [SerializeField] private string dataRootPath = "Assets/BTData/";
        [SerializeField] private string sessionDataPath = "Sessions/";
        [SerializeField] private string trendsDataPath = "Trends/";
        [SerializeField] private string backupDataPath = "Backups/";
        
        [Header("저장 포맷 설정")]
        #pragma warning disable 0414
        [SerializeField] private DataFormat primaryFormat = DataFormat.JSON;
        [SerializeField] private bool enableCompression = false; // Unity에서는 압축 비활성화
        #pragma warning restore 0414
        [SerializeField] private bool prettyPrintJson = false;
        
        [Header("데이터 정리 설정")]
        [SerializeField] private bool enableAutoCleanup = true;
        [SerializeField] private int maxSessionFiles = 100;
        [SerializeField] private int maxDaysToKeep = 30;
        [SerializeField] private float cleanupInterval = 3600f; // 1시간마다
        [SerializeField] private long maxDataSizeMB = 500; // 최대 데이터 크기 (MB)
        
        [Header("실시간 저장 설정")]
        [SerializeField] private bool enableRealTimeSave = true;
        [SerializeField] private float autoSaveInterval = 300f; // 5분마다 자동 저장
        [SerializeField] private bool saveOnEpisodeEnd = true;
        [SerializeField] private bool saveOnApplicationQuit = true;
        
        [Header("백업 설정")]
        [SerializeField] private bool enableBackup = true;
        [SerializeField] private int maxBackupFiles = 10;
        [SerializeField] private float backupInterval = 86400f; // 24시간마다
        
        // 내부 데이터 구조
        private Dictionary<string, SessionData> activeSessions = new Dictionary<string, SessionData>();
        private Dictionary<string, TrendData> trendAnalytics = new Dictionary<string, TrendData>();
        private List<EpisodeRecord> episodeHistory = new List<EpisodeRecord>();
        
        // 데이터 상태 추적
        private float lastAutoSaveTime = 0f;
        private float lastCleanupTime = 0f;
        private float lastBackupTime = 0f;
        private bool hasUnsavedData = false;
        
        // 캐시 시스템
        private Dictionary<string, object> dataCache = new Dictionary<string, object>();
        private int maxCacheSize = 50;
        
        // 이벤트 시스템
        public System.Action<string> OnSessionSaved;
        public System.Action<string> OnDataLoaded;
        public System.Action<string> OnBackupCreated;
        public System.Action<CleanupResult> OnDataCleaned;
        public System.Action<string> OnDataError;
        
        #region Unity 생명주기
        
        private void Awake()
        {
            if (enableDataPersistence)
            {
                InitializeDataSystem();
            }
        }
        
        private void Start()
        {
            LoadExistingData();
        }
        
        private void Update()
        {
            if (!enableDataPersistence) return;
            
            // 자동 저장
            if (enableRealTimeSave && hasUnsavedData && Time.time - lastAutoSaveTime >= autoSaveInterval)
            {
                PerformAutoSave();
                lastAutoSaveTime = Time.time;
            }
            
            // 자동 정리
            if (enableAutoCleanup && Time.time - lastCleanupTime >= cleanupInterval)
            {
                PerformDataCleanup();
                lastCleanupTime = Time.time;
            }
            
            // 백업
            if (enableBackup && Time.time - lastBackupTime >= backupInterval)
            {
                CreateBackup();
                lastBackupTime = Time.time;
            }
        }
        
        private void OnApplicationQuit()
        {
            if (saveOnApplicationQuit && hasUnsavedData)
            {
                SaveAllSessions();
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && hasUnsavedData)
            {
                SaveAllSessions();
            }
        }
        
        #endregion
        
        #region 초기화 및 설정
        
        /// <summary>
        /// 데이터 시스템 초기화
        /// </summary>
        private void InitializeDataSystem()
        {
            try
            {
                // 디렉토리 구조 생성
                CreateDirectoryStructure();
                
                // 기본 설정 검증
                ValidateSettings();
                
                // 데이터 무결성 체크
                PerformIntegrityCheck();
                
                Debug.Log("[DataPersistenceManager] 데이터 영속성 시스템이 초기화되었습니다.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DataPersistenceManager] 초기화 실패: {ex.Message}");
                OnDataError?.Invoke($"초기화 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 디렉토리 구조 생성
        /// </summary>
        private void CreateDirectoryStructure()
        {
            var directories = new[]
            {
                dataRootPath,
                Path.Combine(dataRootPath, sessionDataPath),
                Path.Combine(dataRootPath, trendsDataPath),
                Path.Combine(dataRootPath, backupDataPath),
                Path.Combine(dataRootPath, "Temp/"),
                Path.Combine(dataRootPath, "Cache/")
            };
            
            foreach (var dir in directories)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
        }
        
        /// <summary>
        /// 설정 유효성 검증
        /// </summary>
        private void ValidateSettings()
        {
            if (maxSessionFiles <= 0) maxSessionFiles = 100;
            if (maxDaysToKeep <= 0) maxDaysToKeep = 30;
            if (maxDataSizeMB <= 0) maxDataSizeMB = 500;
            if (autoSaveInterval < 60f) autoSaveInterval = 60f; // 최소 1분
        }
        
        /// <summary>
        /// 데이터 무결성 체크
        /// </summary>
        private void PerformIntegrityCheck()
        {
            try
            {
                // 손상된 파일 검사
                var sessionFiles = Directory.GetFiles(Path.Combine(dataRootPath, sessionDataPath), "*.json");
                var corruptedFiles = new List<string>();
                
                foreach (var file in sessionFiles)
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        if (string.IsNullOrEmpty(content) || !IsValidJson(content))
                        {
                            corruptedFiles.Add(file);
                        }
                    }
                    catch
                    {
                        corruptedFiles.Add(file);
                    }
                }
                
                // 손상된 파일 백업 후 제거
                if (corruptedFiles.Count > 0)
                {
                    Debug.LogWarning($"[DataPersistenceManager] {corruptedFiles.Count}개의 손상된 파일을 발견했습니다.");
                    BackupCorruptedFiles(corruptedFiles);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DataPersistenceManager] 무결성 체크 중 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// JSON 유효성 검사
        /// </summary>
        private bool IsValidJson(string jsonString)
        {
            try
            {
                JsonUtility.FromJson<object>(jsonString);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 손상된 파일 백업
        /// </summary>
        private void BackupCorruptedFiles(List<string> corruptedFiles)
        {
            var corruptedDir = Path.Combine(dataRootPath, "Corrupted", System.DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(corruptedDir);
            
            foreach (var file in corruptedFiles)
            {
                var filename = Path.GetFileName(file);
                var backupPath = Path.Combine(corruptedDir, filename);
                
                try
                {
                    File.Move(file, backupPath);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[DataPersistenceManager] 손상된 파일 백업 실패 ({filename}): {ex.Message}");
                }
            }
        }
        
        #endregion
        
        #region 세션 데이터 관리
        
        /// <summary>
        /// 새 세션 시작
        /// </summary>
        public string StartNewSession(string treeName, string agentType)
        {
            var sessionId = GenerateSessionId(treeName);
            var sessionData = new SessionData
            {
                sessionId = sessionId,
                treeName = treeName,
                agentType = agentType,
                startTime = System.DateTime.Now,
                episodes = new List<EpisodeRecord>(),
                performanceSnapshots = new List<PerformanceSnapshot>(),
                parameterChanges = new List<ParameterChangeRecord>()
            };
            
            activeSessions[sessionId] = sessionData;
            hasUnsavedData = true;
            
            Debug.Log($"[DataPersistenceManager] 새 세션 시작: {sessionId}");
            return sessionId;
        }
        
        /// <summary>
        /// 세션 종료
        /// </summary>
        public void EndSession(string sessionId)
        {
            if (!activeSessions.ContainsKey(sessionId)) return;
            
            var sessionData = activeSessions[sessionId];
            sessionData.endTime = System.DateTime.Now;
            sessionData.duration = (float)(sessionData.endTime - sessionData.startTime).TotalSeconds;
            
            // 세션 저장
            SaveSession(sessionId);
            
            // 활성 세션에서 제거
            activeSessions.Remove(sessionId);
            
            Debug.Log($"[DataPersistenceManager] 세션 종료: {sessionId}");
        }
        
        /// <summary>
        /// 에피소드 기록 추가
        /// </summary>
        public void RecordEpisode(string sessionId, EpisodeRecord episode)
        {
            if (!activeSessions.ContainsKey(sessionId)) return;
            
            activeSessions[sessionId].episodes.Add(episode);
            episodeHistory.Add(episode);
            hasUnsavedData = true;
            
            // 에피소드 종료 시 자동 저장
            if (saveOnEpisodeEnd)
            {
                SaveSession(sessionId);
            }
        }
        
        /// <summary>
        /// 성능 스냅샷 추가
        /// </summary>
        public void AddPerformanceSnapshot(string sessionId, PerformanceSnapshot snapshot)
        {
            if (!activeSessions.ContainsKey(sessionId)) return;
            
            activeSessions[sessionId].performanceSnapshots.Add(snapshot);
            hasUnsavedData = true;
        }
        
        /// <summary>
        /// 파라미터 변경 기록
        /// </summary>
        public void RecordParameterChange(string sessionId, string nodeName, string parameterName, float oldValue, float newValue, string reason)
        {
            if (!activeSessions.ContainsKey(sessionId)) return;
            
            var changeRecord = new ParameterChangeRecord
            {
                timestamp = System.DateTime.Now,
                nodeName = nodeName,
                parameterName = parameterName,
                oldValue = oldValue,
                newValue = newValue,
                changeReason = reason
            };
            
            activeSessions[sessionId].parameterChanges.Add(changeRecord);
            hasUnsavedData = true;
        }
        
        /// <summary>
        /// 세션 ID 생성
        /// </summary>
        private string GenerateSessionId(string treeName)
        {
            var timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var guid = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            return $"{treeName}_{timestamp}_{guid}";
        }
        
        #endregion
        
        #region 데이터 저장/로드
        
        /// <summary>
        /// 세션 저장
        /// </summary>
        public void SaveSession(string sessionId)
        {
            if (!activeSessions.ContainsKey(sessionId))
            {
                Debug.LogWarning($"[DataPersistenceManager] 세션을 찾을 수 없습니다: {sessionId}");
                return;
            }
            
            try
            {
                var sessionData = activeSessions[sessionId];
                var filename = $"Session_{sessionId}.json";
                var filepath = Path.Combine(dataRootPath, sessionDataPath, filename);
                
                // JSON 직렬화
                var jsonData = SerializeToJson(sessionData);
                
                // Unity에서는 압축 없이 저장
                File.WriteAllText(filepath, jsonData, Encoding.UTF8);
                
                Debug.Log($"[DataPersistenceManager] 세션 저장 완료: {sessionId}");
                OnSessionSaved?.Invoke(sessionId);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DataPersistenceManager] 세션 저장 실패 ({sessionId}): {ex.Message}");
                OnDataError?.Invoke($"세션 저장 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 모든 활성 세션 저장
        /// </summary>
        public void SaveAllSessions()
        {
            var sessionIds = activeSessions.Keys.ToList();
            foreach (var sessionId in sessionIds)
            {
                SaveSession(sessionId);
            }
            
            hasUnsavedData = false;
            Debug.Log($"[DataPersistenceManager] {sessionIds.Count}개 세션 저장 완료");
        }
        
        /// <summary>
        /// 세션 로드
        /// </summary>
        public SessionData LoadSession(string sessionId)
        {
            try
            {
                // 캐시 확인
                var cacheKey = $"session_{sessionId}";
                if (dataCache.ContainsKey(cacheKey))
                {
                    return (SessionData)dataCache[cacheKey];
                }
                
                // 파일에서 로드
                var jsonFilepath = Path.Combine(dataRootPath, sessionDataPath, $"Session_{sessionId}.json");
                var compressedFilepath = Path.Combine(dataRootPath, sessionDataPath, $"Session_{sessionId}.json.gz");
                
                string jsonData = null;
                
                // JSON 파일 확인
                if (File.Exists(jsonFilepath))
                {
                    jsonData = File.ReadAllText(jsonFilepath, Encoding.UTF8);
                }
                else
                {
                    Debug.LogWarning($"[DataPersistenceManager] 세션 파일을 찾을 수 없습니다: {sessionId}");
                    return null;
                }
                
                // 역직렬화
                var sessionData = DeserializeFromJson<SessionData>(jsonData);
                
                // 캐시에 저장
                AddToCache(cacheKey, sessionData);
                
                OnDataLoaded?.Invoke(sessionId);
                return sessionData;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DataPersistenceManager] 세션 로드 실패 ({sessionId}): {ex.Message}");
                OnDataError?.Invoke($"세션 로드 실패: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 기존 데이터 로드
        /// </summary>
        private void LoadExistingData()
        {
            try
            {
                // 에피소드 히스토리 로드
                LoadEpisodeHistory();
                
                // 트렌드 데이터 로드
                LoadTrendData();
                
                Debug.Log("[DataPersistenceManager] 기존 데이터 로드 완료");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DataPersistenceManager] 기존 데이터 로드 중 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 에피소드 히스토리 로드
        /// </summary>
        private void LoadEpisodeHistory()
        {
            var historyFile = Path.Combine(dataRootPath, "EpisodeHistory.json");
            if (File.Exists(historyFile))
            {
                var jsonData = File.ReadAllText(historyFile, Encoding.UTF8);
                var historyData = DeserializeFromJson<EpisodeHistoryData>(jsonData);
                episodeHistory = historyData.episodes ?? new List<EpisodeRecord>();
            }
        }
        
        /// <summary>
        /// 트렌드 데이터 로드
        /// </summary>
        private void LoadTrendData()
        {
            var trendFiles = Directory.GetFiles(Path.Combine(dataRootPath, trendsDataPath), "*.json");
            foreach (var file in trendFiles)
            {
                try
                {
                    var jsonData = File.ReadAllText(file, Encoding.UTF8);
                    var trendData = DeserializeFromJson<TrendData>(jsonData);
                    trendAnalytics[trendData.treeName] = trendData;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[DataPersistenceManager] 트렌드 파일 로드 실패 ({file}): {ex.Message}");
                }
            }
        }
        
        #endregion
        
        #region 직렬화/압축
        
        /// <summary>
        /// JSON 직렬화
        /// </summary>
        private string SerializeToJson<T>(T data)
        {
            return JsonUtility.ToJson(data, prettyPrintJson);
        }
        
        /// <summary>
        /// JSON 역직렬화
        /// </summary>
        private T DeserializeFromJson<T>(string jsonData)
        {
            return JsonUtility.FromJson<T>(jsonData);
        }
        

        
        #endregion
        
        #region 트렌드 분석
        
        /// <summary>
        /// 트렌드 데이터 업데이트
        /// </summary>
        public void UpdateTrendData(string treeName, PerformanceTrendPoint dataPoint)
        {
            if (!trendAnalytics.ContainsKey(treeName))
            {
                trendAnalytics[treeName] = new TrendData(treeName);
            }
            
            trendAnalytics[treeName].AddDataPoint(dataPoint);
            
            // 주기적으로 트렌드 데이터 저장
            if (trendAnalytics[treeName].dataPoints.Count % 50 == 0) // 50개마다
            {
                SaveTrendData(treeName);
            }
        }
        
        /// <summary>
        /// 트렌드 데이터 저장
        /// </summary>
        private void SaveTrendData(string treeName)
        {
            try
            {
                if (!trendAnalytics.ContainsKey(treeName)) return;
                
                var trendData = trendAnalytics[treeName];
                var filename = $"Trends_{treeName}_{System.DateTime.Now:yyyyMM}.json";
                var filepath = Path.Combine(dataRootPath, trendsDataPath, filename);
                
                var jsonData = SerializeToJson(trendData);
                File.WriteAllText(filepath, jsonData, Encoding.UTF8);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DataPersistenceManager] 트렌드 데이터 저장 실패 ({treeName}): {ex.Message}");
            }
        }
        
        /// <summary>
        /// 장기 트렌드 분석
        /// </summary>
        public TrendAnalysisResult AnalyzeLongTermTrends(string treeName, int days = 30)
        {
            if (!trendAnalytics.ContainsKey(treeName))
            {
                return new TrendAnalysisResult { treeName = treeName, hasData = false };
            }
            
            var trendData = trendAnalytics[treeName];
            var cutoffDate = System.DateTime.Now.AddDays(-days);
            var recentPoints = trendData.dataPoints
                .Where(p => p.timestamp >= cutoffDate)
                .OrderBy(p => p.timestamp)
                .ToList();
            
            if (recentPoints.Count < 2)
            {
                return new TrendAnalysisResult { treeName = treeName, hasData = false };
            }
            
            return new TrendAnalysisResult
            {
                treeName = treeName,
                hasData = true,
                periodDays = days,
                dataPointCount = recentPoints.Count,
                averageSuccessRate = recentPoints.Average(p => p.successRate),
                averageExecutionTime = recentPoints.Average(p => p.executionTime),
                successRateTrend = CalculateTrend(recentPoints.Select(p => (double)p.successRate).ToList()),
                executionTimeTrend = CalculateTrend(recentPoints.Select(p => (double)p.executionTime).ToList()),
                improvementRate = CalculateImprovementRate(recentPoints),
                startPeriod = recentPoints.First().timestamp,
                endPeriod = recentPoints.Last().timestamp
            };
        }
        
        /// <summary>
        /// 트렌드 계산 (선형 회귀)
        /// </summary>
        private double CalculateTrend(List<double> values)
        {
            if (values.Count < 2) return 0;
            
            int n = values.Count;
            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;
            
            for (int i = 0; i < n; i++)
            {
                double x = i;
                double y = values[i];
                
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumX2 += x * x;
            }
            
            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            return slope;
        }
        
        /// <summary>
        /// 개선율 계산
        /// </summary>
        private float CalculateImprovementRate(List<PerformanceTrendPoint> points)
        {
            if (points.Count < 2) return 0f;
            
            var firstHalf = points.Take(points.Count / 2);
            var secondHalf = points.Skip(points.Count / 2);
            
            float firstAvg = firstHalf.Average(p => p.successRate);
            float secondAvg = secondHalf.Average(p => p.successRate);
            
            return firstAvg > 0 ? (secondAvg - firstAvg) / firstAvg * 100f : 0f;
        }
        
        #endregion
        
        #region 데이터 정리 및 백업
        
        /// <summary>
        /// 자동 저장 수행
        /// </summary>
        private void PerformAutoSave()
        {
            try
            {
                SaveAllSessions();
                
                // 에피소드 히스토리 저장
                SaveEpisodeHistory();
                
                // 트렌드 데이터 저장
                foreach (var treeName in trendAnalytics.Keys.ToList())
                {
                    SaveTrendData(treeName);
                }
                
                hasUnsavedData = false;
                Debug.Log("[DataPersistenceManager] 자동 저장 완료");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DataPersistenceManager] 자동 저장 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 데이터 정리 수행
        /// </summary>
        private void PerformDataCleanup()
        {
            try
            {
                var result = new CleanupResult();
                var cutoffDate = System.DateTime.Now.AddDays(-maxDaysToKeep);
                
                // 오래된 세션 파일 정리
                var sessionDir = Path.Combine(dataRootPath, sessionDataPath);
                var sessionFiles = Directory.GetFiles(sessionDir, "Session_*.json*");
                
                foreach (var file in sessionFiles)
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(file);
                        result.deletedFiles++;
                        result.freedSpaceMB += fileInfo.Length / (1024f * 1024f);
                    }
                }
                
                // 최대 파일 수 제한
                var remainingFiles = Directory.GetFiles(sessionDir, "Session_*.json*")
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .Skip(maxSessionFiles)
                    .ToList();
                
                foreach (var file in remainingFiles)
                {
                    var fileInfo = new FileInfo(file);
                    File.Delete(file);
                    result.deletedFiles++;
                    result.freedSpaceMB += fileInfo.Length / (1024f * 1024f);
                }
                
                // 전체 데이터 크기 제한
                CheckDataSizeLimit(result);
                
                // 캐시 정리
                CleanupCache();
                
                if (result.deletedFiles > 0)
                {
                    Debug.Log($"[DataPersistenceManager] 데이터 정리 완료: {result.deletedFiles}개 파일 삭제, {result.freedSpaceMB:F2}MB 해제");
                }
                
                OnDataCleaned?.Invoke(result);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DataPersistenceManager] 데이터 정리 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 데이터 크기 제한 확인
        /// </summary>
        private void CheckDataSizeLimit(CleanupResult result)
        {
            var directoryInfo = new DirectoryInfo(dataRootPath);
            var totalSizeMB = CalculateDirectorySize(directoryInfo) / (1024f * 1024f);
            
            if (totalSizeMB > maxDataSizeMB)
            {
                // 가장 오래된 파일부터 삭제
                var allFiles = directoryInfo.GetFiles("*", SearchOption.AllDirectories)
                    .Where(f => f.Extension == ".json" || f.Extension == ".gz")
                    .OrderBy(f => f.CreationTime)
                    .ToList();
                
                foreach (var file in allFiles)
                {
                    var fileSizeMB = file.Length / (1024f * 1024f);
                    file.Delete();
                    result.deletedFiles++;
                    result.freedSpaceMB += fileSizeMB;
                    totalSizeMB -= fileSizeMB;
                    
                    if (totalSizeMB <= maxDataSizeMB * 0.8f) // 80%까지 정리
                        break;
                }
            }
        }
        
        /// <summary>
        /// 디렉토리 크기 계산
        /// </summary>
        private long CalculateDirectorySize(DirectoryInfo directory)
        {
            long size = 0;
            try
            {
                size += directory.GetFiles().Sum(f => f.Length);
                size += directory.GetDirectories().Sum(d => CalculateDirectorySize(d));
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DataPersistenceManager] 디렉토리 크기 계산 실패 ({directory.Name}): {ex.Message}");
            }
            return size;
        }
        
        /// <summary>
        /// 백업 생성
        /// </summary>
        private void CreateBackup()
        {
            try
            {
                var backupName = $"Backup_{System.DateTime.Now:yyyyMMdd_HHmmss}";
                var backupPath = Path.Combine(dataRootPath, backupDataPath, backupName);
                
                Directory.CreateDirectory(backupPath);
                
                // 중요 파일들 백업
                BackupDirectory(Path.Combine(dataRootPath, sessionDataPath), Path.Combine(backupPath, "Sessions"));
                BackupDirectory(Path.Combine(dataRootPath, trendsDataPath), Path.Combine(backupPath, "Trends"));
                
                // 백업 메타데이터 저장
                var backupInfo = new BackupInfo
                {
                    backupName = backupName,
                    creationTime = System.DateTime.Now,
                    originalSize = CalculateDirectorySize(new DirectoryInfo(dataRootPath)),
                    backupSize = CalculateDirectorySize(new DirectoryInfo(backupPath))
                };
                
                var metadataPath = Path.Combine(backupPath, "backup_info.json");
                File.WriteAllText(metadataPath, SerializeToJson(backupInfo), Encoding.UTF8);
                
                // 오래된 백업 정리
                CleanupOldBackups();
                
                Debug.Log($"[DataPersistenceManager] 백업 생성 완료: {backupName}");
                OnBackupCreated?.Invoke(backupName);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DataPersistenceManager] 백업 생성 실패: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 디렉토리 백업
        /// </summary>
        private void BackupDirectory(string sourceDir, string targetDir)
        {
            if (!Directory.Exists(sourceDir)) return;
            
            Directory.CreateDirectory(targetDir);
            
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var targetFile = Path.Combine(targetDir, fileName);
                File.Copy(file, targetFile, true);
            }
            
            foreach (var dir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(dir);
                BackupDirectory(dir, Path.Combine(targetDir, dirName));
            }
        }
        
        /// <summary>
        /// 오래된 백업 정리
        /// </summary>
        private void CleanupOldBackups()
        {
            var backupDir = Path.Combine(dataRootPath, backupDataPath);
            var backupDirs = Directory.GetDirectories(backupDir)
                .OrderByDescending(d => Directory.GetCreationTime(d))
                .Skip(maxBackupFiles)
                .ToList();
            
            foreach (var dir in backupDirs)
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[DataPersistenceManager] 백업 삭제 실패 ({dir}): {ex.Message}");
                }
            }
        }
        
        #endregion
        
        #region 캐시 관리
        
        /// <summary>
        /// 캐시에 데이터 추가
        /// </summary>
        private void AddToCache(string key, object data)
        {
            if (dataCache.Count >= maxCacheSize)
            {
                // LRU 방식으로 가장 오래된 항목 제거
                var oldestKey = dataCache.Keys.First();
                dataCache.Remove(oldestKey);
            }
            
            dataCache[key] = data;
        }
        
        /// <summary>
        /// 캐시 정리
        /// </summary>
        private void CleanupCache()
        {
            // 캐시 크기를 절반으로 줄임
            var keysToRemove = dataCache.Keys.Take(dataCache.Count / 2).ToList();
            foreach (var key in keysToRemove)
            {
                dataCache.Remove(key);
            }
        }
        
        #endregion
        
        #region 유틸리티 메서드
        
        /// <summary>
        /// 에피소드 히스토리 저장
        /// </summary>
        private void SaveEpisodeHistory()
        {
            try
            {
                var historyData = new EpisodeHistoryData { episodes = episodeHistory };
                var historyFile = Path.Combine(dataRootPath, "EpisodeHistory.json");
                var jsonData = SerializeToJson(historyData);
                File.WriteAllText(historyFile, jsonData, Encoding.UTF8);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[DataPersistenceManager] 에피소드 히스토리 저장 실패: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 공개 API
        
        /// <summary>
        /// 활성 세션 목록 반환
        /// </summary>
        public List<string> GetActiveSessionIds()
        {
            return activeSessions.Keys.ToList();
        }
        
        /// <summary>
        /// 세션 정보 반환
        /// </summary>
        public SessionData GetSessionData(string sessionId)
        {
            return activeSessions.ContainsKey(sessionId) ? activeSessions[sessionId] : null;
        }
        
        /// <summary>
        /// 에피소드 히스토리 반환
        /// </summary>
        public List<EpisodeRecord> GetEpisodeHistory(string treeName = null, int maxCount = -1)
        {
            var history = string.IsNullOrEmpty(treeName) ? 
                episodeHistory : 
                episodeHistory.Where(e => e.treeName == treeName).ToList();
            
            if (maxCount > 0 && history.Count > maxCount)
            {
                history = history.TakeLast(maxCount).ToList();
            }
            
            return history;
        }
        
        /// <summary>
        /// 트렌드 분석 결과 반환
        /// </summary>
        public TrendAnalysisResult GetTrendAnalysis(string treeName, int days = 30)
        {
            return AnalyzeLongTermTrends(treeName, days);
        }
        
        /// <summary>
        /// 데이터 통계 반환
        /// </summary>
        public DataStatistics GetDataStatistics()
        {
            var stats = new DataStatistics
            {
                totalSessions = activeSessions.Count,
                totalEpisodes = episodeHistory.Count,
                totalDataSizeMB = CalculateDirectorySize(new DirectoryInfo(dataRootPath)) / (1024f * 1024f),
                oldestDataDate = episodeHistory.Count > 0 ? episodeHistory.Min(e => e.timestamp) : System.DateTime.Now,
                newestDataDate = episodeHistory.Count > 0 ? episodeHistory.Max(e => e.timestamp) : System.DateTime.Now,
                cacheHitRate = 0f, // 캐시 히트율 계산 로직 필요시 구현
                treeNames = trendAnalytics.Keys.ToList()
            };
            
            return stats;
        }
        
        /// <summary>
        /// 저장된 세션 목록 반환
        /// </summary>
        public List<string> GetSavedSessionIds()
        {
            var sessionDir = Path.Combine(dataRootPath, sessionDataPath);
            if (!Directory.Exists(sessionDir)) return new List<string>();
            
            var sessionFiles = Directory.GetFiles(sessionDir, "Session_*.json*");
            return sessionFiles.Select(f => {
                var filename = Path.GetFileNameWithoutExtension(f);
                if (filename.EndsWith(".json")) filename = Path.GetFileNameWithoutExtension(filename);
                return filename.Replace("Session_", "");
            }).ToList();
        }
        
        /// <summary>
        /// 데이터 무결성 상태 반환
        /// </summary>
        public DataIntegrityStatus GetDataIntegrityStatus()
        {
            var status = new DataIntegrityStatus
            {
                hasCorruptedFiles = false,
                missingFiles = new List<string>(),
                totalFiles = 0,
                validFiles = 0,
                lastCheckTime = System.DateTime.Now
            };
            
            // 간단한 무결성 체크
            try
            {
                var sessionDir = Path.Combine(dataRootPath, sessionDataPath);
                var sessionFiles = Directory.GetFiles(sessionDir, "*.json*");
                status.totalFiles = sessionFiles.Length;
                
                foreach (var file in sessionFiles)
                {
                    try
                    {
                        var content = File.ReadAllText(file);
                        if (!string.IsNullOrEmpty(content))
                        {
                            // 간단한 JSON 구조 검증
                            if (content.TrimStart().StartsWith("{") && content.TrimEnd().EndsWith("}"))
                            {
                                // 유효한 JSON 형태로 간주
                            }
                            else
                            {
                                throw new System.Exception("Invalid JSON format");
                            }
                        }
                        status.validFiles++;
                    }
                    catch
                    {
                        status.hasCorruptedFiles = true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DataPersistenceManager] 무결성 체크 실패: {ex.Message}");
            }
            
            return status;
        }
        
        #endregion
    }
    
    #region 데이터 구조체들
    
    /// <summary>
    /// 세션 데이터
    /// </summary>
    [System.Serializable]
    public class SessionData
    {
        public string sessionId;
        public string treeName;
        public string agentType;
        public System.DateTime startTime;
        public System.DateTime endTime;
        public float duration;
        public List<EpisodeRecord> episodes;
        public List<PerformanceSnapshot> performanceSnapshots;
        public List<ParameterChangeRecord> parameterChanges;
    }
    
    /// <summary>
    /// 에피소드 기록
    /// </summary>
    [System.Serializable]
    public class EpisodeRecord
    {
        public string episodeId;
        public string treeName;
        public string agentName;
        public System.DateTime timestamp;
        public float duration;
        public bool won;
        public float finalHP;
        public float initialHP;
        public int totalActions;
        public int successfulActions;
        public float averageExecutionTime;
        public Dictionary<string, int> nodeExecutionCounts;
        public Dictionary<string, float> finalParameters;
    }
    
    /// <summary>
    /// 파라미터 변경 기록
    /// </summary>
    [System.Serializable]
    public class ParameterChangeRecord
    {
        public System.DateTime timestamp;
        public string nodeName;
        public string parameterName;
        public float oldValue;
        public float newValue;
        public string changeReason;
    }
    
    /// <summary>
    /// 트렌드 데이터
    /// </summary>
    [System.Serializable]
    public class TrendData
    {
        public string treeName;
        public List<PerformanceTrendPoint> dataPoints;
        
        public TrendData(string name)
        {
            treeName = name;
            dataPoints = new List<PerformanceTrendPoint>();
        }
        
        public void AddDataPoint(PerformanceTrendPoint point)
        {
            dataPoints.Add(point);
            
            // 최대 개수 제한 (메모리 관리)
            if (dataPoints.Count > 10000)
            {
                dataPoints.RemoveAt(0);
            }
        }
    }
    
    /// <summary>
    /// 성능 트렌드 포인트
    /// </summary>
    [System.Serializable]
    public struct PerformanceTrendPoint
    {
        public System.DateTime timestamp;
        public float successRate;
        public float executionTime;
        public float memoryUsage;
        public float fps;
        public int episodeCount;
    }
    
    /// <summary>
    /// 트렌드 분석 결과
    /// </summary>
    [System.Serializable]
    public class TrendAnalysisResult
    {
        public string treeName;
        public bool hasData;
        public int periodDays;
        public int dataPointCount;
        public float averageSuccessRate;
        public float averageExecutionTime;
        public double successRateTrend; // 양수: 증가, 음수: 감소
        public double executionTimeTrend; // 양수: 증가, 음수: 감소
        public float improvementRate; // 개선율 (%)
        public System.DateTime startPeriod;
        public System.DateTime endPeriod;
    }
    
    /// <summary>
    /// 정리 결과
    /// </summary>
    [System.Serializable]
    public class CleanupResult
    {
        public int deletedFiles = 0;
        public float freedSpaceMB = 0f;
        public System.DateTime cleanupTime = System.DateTime.Now;
    }
    
    /// <summary>
    /// 백업 정보
    /// </summary>
    [System.Serializable]
    public class BackupInfo
    {
        public string backupName;
        public System.DateTime creationTime;
        public long originalSize;
        public long backupSize;
    }
    
    /// <summary>
    /// 에피소드 히스토리 데이터
    /// </summary>
    [System.Serializable]
    public class EpisodeHistoryData
    {
        public List<EpisodeRecord> episodes;
    }
    
    /// <summary>
    /// 데이터 통계
    /// </summary>
    [System.Serializable]
    public class DataStatistics
    {
        public int totalSessions;
        public int totalEpisodes;
        public float totalDataSizeMB;
        public System.DateTime oldestDataDate;
        public System.DateTime newestDataDate;
        public float cacheHitRate;
        public List<string> treeNames;
    }
    
    /// <summary>
    /// 데이터 무결성 상태
    /// </summary>
    [System.Serializable]
    public class DataIntegrityStatus
    {
        public bool hasCorruptedFiles;
        public List<string> missingFiles;
        public int totalFiles;
        public int validFiles;
        public System.DateTime lastCheckTime;
    }
    
    /// <summary>
    /// 데이터 포맷
    /// </summary>
    public enum DataFormat
    {
        JSON,
        CompressedJSON,
        Binary
    }
    
    #endregion
}
