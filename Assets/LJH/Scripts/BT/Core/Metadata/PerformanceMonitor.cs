using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace LJH.BT
{
    /// <summary>
    /// BT 트리의 실시간 성능 모니터링 및 병목 탐지 시스템
    /// BTTreeStatistics와 연동하여 런타임 성능을 추적하고 자동으로 최적화 제안을 제공
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        [Header("모니터링 설정")]
        [SerializeField] private bool enableMonitoring = true;
        [SerializeField] private float monitoringInterval = 1f; // 모니터링 주기 (초)
        [SerializeField] private int historySize = 100; // 성능 히스토리 크기
        
        [Header("경고 임계값")]
        [SerializeField] private float fpsWarningThreshold = 30f; // FPS 경고 임계값
        [SerializeField] private float executionTimeWarningThreshold = 5f; // 실행 시간 경고 임계값 (ms)
        [SerializeField] private float memoryWarningThreshold = 500f; // 메모리 경고 임계값 (MB)
        [SerializeField] private float failureRateWarningThreshold = 0.3f; // 실패율 경고 임계값
        
        [Header("자동 최적화")]
        [SerializeField] private bool enableAutoOptimization = false; // 자동 최적화 사용
        [SerializeField] private float optimizationTriggerThreshold = 0.7f; // 최적화 트리거 임계값
        [SerializeField] private int optimizationCooldown = 30; // 최적화 간격 (초)
        
        [Header("디버그 표시")]
        #pragma warning disable 0414
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showPerformanceGraph = false;
        #pragma warning restore 0414
        [SerializeField] private bool logPerformanceWarnings = true;
        
        // 내부 변수들
        private Dictionary<string, BTTreeStatistics> monitoredTrees = new Dictionary<string, BTTreeStatistics>();
        private List<PerformanceSnapshot> performanceHistory = new List<PerformanceSnapshot>();
        private List<PerformanceAlert> activeAlerts = new List<PerformanceAlert>();
        
        // 성능 메트릭
        private float currentFPS = 0f;
        private float currentMemoryUsage = 0f;
        private float averageFrameTime = 0f;
        private int frameCount = 0;
        private float fpsSum = 0f;
        
        // 모니터링 상태
        private bool isMonitoring = false;
        private float lastMonitoringTime = 0f;
        private float lastOptimizationTime = 0f;
        
        // 이벤트 시스템
        public System.Action<PerformanceAlert> OnPerformanceAlert;
        public System.Action<string, float> OnPerformanceImprovement;
        public System.Action<string> OnBottleneckDetected;
        
        #region Unity 생명주기
        
        private void Start()
        {
            if (enableMonitoring)
            {
                StartMonitoring();
            }
        }
        
        private void Update()
        {
            if (!isMonitoring) return;
            
            UpdatePerformanceMetrics();
            
            // 주기적 모니터링
            if (Time.time - lastMonitoringTime >= monitoringInterval)
            {
                PerformMonitoringCycle();
                lastMonitoringTime = Time.time;
            }
        }
        
        private void OnDestroy()
        {
            StopMonitoring();
        }
        
        #endregion
        
        #region 모니터링 제어
        
        /// <summary>
        /// 모니터링 시작
        /// </summary>
        public void StartMonitoring()
        {
            if (isMonitoring) return;
            
            isMonitoring = true;
            lastMonitoringTime = Time.time;
            lastOptimizationTime = Time.time;
            
            // 성능 메트릭 초기화
            frameCount = 0;
            fpsSum = 0f;
            
            Debug.Log("[PerformanceMonitor] 모니터링이 시작되었습니다.");
        }
        
        /// <summary>
        /// 모니터링 중지
        /// </summary>
        public void StopMonitoring()
        {
            if (!isMonitoring) return;
            
            isMonitoring = false;
            
            Debug.Log("[PerformanceMonitor] 모니터링이 중지되었습니다.");
        }
        
        /// <summary>
        /// BT 트리 등록
        /// </summary>
        public void RegisterTree(string treeName, BTTreeStatistics treeStats)
        {
            if (string.IsNullOrEmpty(treeName) || treeStats == null) return;
            
            monitoredTrees[treeName] = treeStats;
            
            Debug.Log($"[PerformanceMonitor] BT 트리 '{treeName}'가 모니터링에 등록되었습니다.");
        }
        
        /// <summary>
        /// BT 트리 등록 해제
        /// </summary>
        public void UnregisterTree(string treeName)
        {
            if (monitoredTrees.ContainsKey(treeName))
            {
                monitoredTrees.Remove(treeName);
                Debug.Log($"[PerformanceMonitor] BT 트리 '{treeName}'가 모니터링에서 해제되었습니다.");
            }
        }
        
        #endregion
        
        #region 성능 메트릭 수집
        
        /// <summary>
        /// 성능 메트릭 업데이트
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            // FPS 계산
            frameCount++;
            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime > 0f)
            {
                float fps = 1f / deltaTime;
                fpsSum += fps;
                currentFPS = fpsSum / frameCount;
                averageFrameTime = deltaTime * 1000f; // ms로 변환
            }
            
            // 메모리 사용량 (주기적으로만 체크 - 비용이 높음)
            if (frameCount % 60 == 0) // 1초마다 (60fps 기준)
            {
                currentMemoryUsage = (float)System.GC.GetTotalMemory(false) / (1024f * 1024f); // MB로 변환
            }
        }
        
        /// <summary>
        /// 모니터링 사이클 수행
        /// </summary>
        private void PerformMonitoringCycle()
        {
            // 성능 스냅샷 생성
            var snapshot = CreatePerformanceSnapshot();
            AddPerformanceSnapshot(snapshot);
            
            // 각 트리 분석
            foreach (var kvp in monitoredTrees)
            {
                AnalyzeTreePerformance(kvp.Key, kvp.Value);
            }
            
            // 전체 시스템 성능 분석
            AnalyzeSystemPerformance(snapshot);
            
            // 경고 처리
            ProcessActiveAlerts();
            
            // 자동 최적화 체크
            if (enableAutoOptimization && Time.time - lastOptimizationTime > optimizationCooldown)
            {
                CheckAutoOptimization();
            }
            
            // 프레임 카운터 리셋
            frameCount = 0;
            fpsSum = 0f;
        }
        
        /// <summary>
        /// 성능 스냅샷 생성
        /// </summary>
        private PerformanceSnapshot CreatePerformanceSnapshot()
        {
            return new PerformanceSnapshot
            {
                timestamp = Time.time,
                fps = currentFPS,
                frameTime = averageFrameTime,
                memoryUsage = currentMemoryUsage,
                activeTreeCount = monitoredTrees.Count,
                totalTreeExecutions = monitoredTrees.Values.Sum(t => t.totalTreeExecutions),
                averageSuccessRate = monitoredTrees.Count > 0 ? monitoredTrees.Values.Average(t => t.successRate) : 0f
            };
        }
        
        /// <summary>
        /// 성능 스냅샷 추가
        /// </summary>
        private void AddPerformanceSnapshot(PerformanceSnapshot snapshot)
        {
            performanceHistory.Add(snapshot);
            
            // 히스토리 크기 제한
            if (performanceHistory.Count > historySize)
            {
                performanceHistory.RemoveAt(0);
            }
        }
        
        #endregion
        
        #region 성능 분석
        
        /// <summary>
        /// 개별 트리 성능 분석
        /// </summary>
        private void AnalyzeTreePerformance(string treeName, BTTreeStatistics treeStats)
        {
            // 실행 시간 체크
            if (treeStats.averageExecutionTime > executionTimeWarningThreshold)
            {
                CreateAlert(AlertType.HighExecutionTime, treeName, 
                    $"평균 실행 시간이 {treeStats.averageExecutionTime:F2}ms로 임계값을 초과했습니다.");
            }
            
            // 실패율 체크
            if (treeStats.successRate < (1f - failureRateWarningThreshold))
            {
                CreateAlert(AlertType.HighFailureRate, treeName, 
                    $"성공률이 {treeStats.successRate:P1}로 낮습니다.");
            }
            
            // 병목 지점 체크
            if (treeStats.bottlenecks.Count > 0)
            {
                var severestBottleneck = treeStats.bottlenecks.OrderByDescending(b => b.severity).First();
                if (severestBottleneck.severity > 2f) // 임계값의 2배 이상
                {
                    CreateAlert(AlertType.PerformanceBottleneck, treeName, 
                        $"심각한 병목 발견: {severestBottleneck.nodeName} - {severestBottleneck.description}");
                        
                    OnBottleneckDetected?.Invoke($"{treeName}.{severestBottleneck.nodeName}");
                }
            }
            
            // 효율성 체크 (새로운 메트릭)
            if (treeStats.executionEfficiency < 0.1f && treeStats.totalTreeExecutions > 10)
            {
                CreateAlert(AlertType.LowEfficiency, treeName, 
                    $"실행 효율성이 {treeStats.executionEfficiency:F3}으로 낮습니다.");
            }
        }
        
        /// <summary>
        /// 시스템 전체 성능 분석
        /// </summary>
        private void AnalyzeSystemPerformance(PerformanceSnapshot snapshot)
        {
            // FPS 체크
            if (snapshot.fps < fpsWarningThreshold)
            {
                CreateAlert(AlertType.LowFPS, "System", 
                    $"FPS가 {snapshot.fps:F1}로 임계값 {fpsWarningThreshold}를 하회했습니다.");
            }
            
            // 메모리 체크
            if (snapshot.memoryUsage > memoryWarningThreshold)
            {
                CreateAlert(AlertType.HighMemoryUsage, "System", 
                    $"메모리 사용량이 {snapshot.memoryUsage:F1}MB로 임계값을 초과했습니다.");
            }
            
            // 성능 추세 분석 (최근 10개 스냅샷)
            if (performanceHistory.Count >= 10)
            {
                var recentSnapshots = performanceHistory.TakeLast(10).ToList();
                AnalyzePerformanceTrends(recentSnapshots);
            }
        }
        
        /// <summary>
        /// 성능 추세 분석
        /// </summary>
        private void AnalyzePerformanceTrends(List<PerformanceSnapshot> recentSnapshots)
        {
            // FPS 하락 추세 체크
            var fpsValues = recentSnapshots.Select(s => s.fps).ToList();
            if (IsDecreasingTrend(fpsValues, 0.9f)) // 10% 이상 하락
            {
                CreateAlert(AlertType.PerformanceDegradation, "System", 
                    "FPS가 지속적으로 하락하는 추세입니다.");
            }
            
            // 메모리 증가 추세 체크
            var memoryValues = recentSnapshots.Select(s => s.memoryUsage).ToList();
            if (IsIncreasingTrend(memoryValues, 1.1f)) // 10% 이상 증가
            {
                CreateAlert(AlertType.MemoryLeak, "System", 
                    "메모리 사용량이 지속적으로 증가하는 추세입니다. 메모리 누수를 확인하세요.");
            }
        }
        
        /// <summary>
        /// 감소 추세 감지
        /// </summary>
        private bool IsDecreasingTrend(List<float> values, float threshold)
        {
            if (values.Count < 2) return false;
            
            float firstValue = values.First();
            float lastValue = values.Last();
            
            return lastValue < firstValue * threshold;
        }
        
        /// <summary>
        /// 증가 추세 감지
        /// </summary>
        private bool IsIncreasingTrend(List<float> values, float threshold)
        {
            if (values.Count < 2) return false;
            
            float firstValue = values.First();
            float lastValue = values.Last();
            
            return lastValue > firstValue * threshold;
        }
        
        #endregion
        
        #region 경고 시스템
        
        /// <summary>
        /// 경고 생성
        /// </summary>
        private void CreateAlert(AlertType type, string source, string message)
        {
            // 중복 경고 체크 (같은 소스, 같은 타입의 경고가 이미 활성화되어 있는지)
            if (activeAlerts.Any(a => a.type == type && a.source == source && !a.isResolved))
            {
                return; // 중복 경고 방지
            }
            
            var alert = new PerformanceAlert
            {
                id = System.Guid.NewGuid().ToString(),
                type = type,
                source = source,
                message = message,
                timestamp = Time.time,
                severity = GetAlertSeverity(type),
                isResolved = false
            };
            
            activeAlerts.Add(alert);
            
            // 심각도에 따른 로깅
            if (logPerformanceWarnings)
            {
                switch (alert.severity)
                {
                    case AlertSeverity.Low:
                        Debug.Log($"[PerformanceMonitor] {alert.message}");
                        break;
                    case AlertSeverity.Medium:
                        Debug.LogWarning($"[PerformanceMonitor] {alert.message}");
                        break;
                    case AlertSeverity.High:
                        Debug.LogError($"[PerformanceMonitor] {alert.message}");
                        break;
                }
            }
            
            // 이벤트 발생
            OnPerformanceAlert?.Invoke(alert);
        }
        
        /// <summary>
        /// 경고 심각도 결정
        /// </summary>
        private AlertSeverity GetAlertSeverity(AlertType type)
        {
            switch (type)
            {
                case AlertType.LowFPS:
                case AlertType.HighMemoryUsage:
                case AlertType.PerformanceBottleneck:
                    return AlertSeverity.High;
                    
                case AlertType.HighExecutionTime:
                case AlertType.HighFailureRate:
                case AlertType.PerformanceDegradation:
                    return AlertSeverity.Medium;
                    
                default:
                    return AlertSeverity.Low;
            }
        }
        
        /// <summary>
        /// 활성 경고 처리
        /// </summary>
        private void ProcessActiveAlerts()
        {
            for (int i = activeAlerts.Count - 1; i >= 0; i--)
            {
                var alert = activeAlerts[i];
                
                // 자동 해결 체크 (조건이 개선되었는지 확인)
                if (ShouldResolveAlert(alert))
                {
                    alert.isResolved = true;
                    alert.resolvedTime = Time.time;
                    
                    if (logPerformanceWarnings)
                    {
                        Debug.Log($"[PerformanceMonitor] 경고가 해결되었습니다: {alert.message}");
                    }
                }
                
                // 오래된 해결된 경고 제거 (1분 후)
                if (alert.isResolved && Time.time - alert.resolvedTime > 60f)
                {
                    activeAlerts.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// 경고 해결 여부 판단
        /// </summary>
        private bool ShouldResolveAlert(PerformanceAlert alert)
        {
            switch (alert.type)
            {
                case AlertType.LowFPS:
                    return currentFPS >= fpsWarningThreshold * 1.1f; // 10% 여유
                    
                case AlertType.HighMemoryUsage:
                    return currentMemoryUsage <= memoryWarningThreshold * 0.9f; // 10% 여유
                    
                case AlertType.HighExecutionTime:
                    if (monitoredTrees.ContainsKey(alert.source))
                    {
                        return monitoredTrees[alert.source].averageExecutionTime <= executionTimeWarningThreshold * 0.9f;
                    }
                    break;
                    
                case AlertType.HighFailureRate:
                    if (monitoredTrees.ContainsKey(alert.source))
                    {
                        return monitoredTrees[alert.source].successRate >= (1f - failureRateWarningThreshold) + 0.1f;
                    }
                    break;
            }
            
            return false;
        }
        
        #endregion
        
        #region 자동 최적화
        
        /// <summary>
        /// 자동 최적화 체크
        /// </summary>
        private void CheckAutoOptimization()
        {
            foreach (var kvp in monitoredTrees)
            {
                var treeName = kvp.Key;
                var treeStats = kvp.Value;
                
                // 최적화 필요성 점수 계산
                float optimizationScore = CalculateOptimizationScore(treeStats);
                
                if (optimizationScore >= optimizationTriggerThreshold)
                {
                    PerformAutoOptimization(treeName, treeStats);
                    lastOptimizationTime = Time.time;
                    break; // 한 번에 하나씩만 최적화
                }
            }
        }
        
        /// <summary>
        /// 최적화 필요성 점수 계산
        /// </summary>
        private float CalculateOptimizationScore(BTTreeStatistics treeStats)
        {
            float score = 0f;
            
            // 실행 시간 가중치 (40%)
            if (treeStats.averageExecutionTime > executionTimeWarningThreshold)
            {
                score += 0.4f * (treeStats.averageExecutionTime / executionTimeWarningThreshold);
            }
            
            // 실패율 가중치 (30%)
            if (treeStats.successRate < 0.8f)
            {
                score += 0.3f * (1f - treeStats.successRate);
            }
            
            // 병목 심각도 가중치 (20%)
            if (treeStats.bottlenecks.Count > 0)
            {
                float maxSeverity = treeStats.bottlenecks.Max(b => b.severity);
                score += 0.2f * Mathf.Min(maxSeverity / 3f, 1f); // 심각도 3을 최대로 정규화
            }
            
            // 효율성 가중치 (10%)
            if (treeStats.executionEfficiency < 0.2f)
            {
                score += 0.1f * (0.2f - treeStats.executionEfficiency) / 0.2f;
            }
            
            return Mathf.Clamp01(score);
        }
        
        /// <summary>
        /// 자동 최적화 수행
        /// </summary>
        private void PerformAutoOptimization(string treeName, BTTreeStatistics treeStats)
        {
            Debug.Log($"[PerformanceMonitor] '{treeName}' 트리에 대한 자동 최적화를 시작합니다.");
            
            // 최우선 최적화 대상 노드 선택
            var topPriorityNode = treeStats.optimizationPriorities
                .OrderByDescending(kvp => kvp.Value)
                .FirstOrDefault();
            
            if (!string.IsNullOrEmpty(topPriorityNode.Key))
            {
                // 여기서 실제 최적화 로직을 구현할 수 있습니다.
                // 예: 파라미터 조정, 캐싱 활성화, 실행 순서 변경 등
                
                Debug.Log($"[PerformanceMonitor] 최우선 최적화 대상: {topPriorityNode.Key} (우선순위: {topPriorityNode.Value:F3})");
                
                // 최적화 제안 이벤트 발생
                OnPerformanceImprovement?.Invoke(treeName, topPriorityNode.Value);
            }
        }
        
        #endregion
        
        #region 공개 API
        
        /// <summary>
        /// 현재 성능 상태 반환
        /// </summary>
        public PerformanceStatus GetCurrentPerformanceStatus()
        {
            return new PerformanceStatus
            {
                fps = currentFPS,
                frameTime = averageFrameTime,
                memoryUsage = currentMemoryUsage,
                monitoredTreeCount = monitoredTrees.Count,
                activeAlertCount = activeAlerts.Count(a => !a.isResolved),
                averageSuccessRate = monitoredTrees.Count > 0 ? monitoredTrees.Values.Average(t => t.successRate) : 0f
            };
        }
        
        /// <summary>
        /// 활성 경고 목록 반환
        /// </summary>
        public List<PerformanceAlert> GetActiveAlerts()
        {
            return activeAlerts.Where(a => !a.isResolved).ToList();
        }
        
        /// <summary>
        /// 성능 히스토리 반환
        /// </summary>
        public List<PerformanceSnapshot> GetPerformanceHistory(int count = -1)
        {
            if (count <= 0 || count > performanceHistory.Count)
            {
                return new List<PerformanceSnapshot>(performanceHistory);
            }
            
            return performanceHistory.TakeLast(count).ToList();
        }
        
        /// <summary>
        /// 트리별 성능 리포트 생성
        /// </summary>
        public string GeneratePerformanceReport()
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== 성능 모니터링 리포트 ===");
            report.AppendLine($"모니터링 시간: {Time.time:F1}초");
            report.AppendLine($"현재 FPS: {currentFPS:F1}");
            report.AppendLine($"메모리 사용량: {currentMemoryUsage:F1}MB");
            report.AppendLine($"모니터링 중인 트리: {monitoredTrees.Count}개");
            report.AppendLine($"활성 경고: {activeAlerts.Count(a => !a.isResolved)}개");
            report.AppendLine();
            
            // 트리별 상태
            report.AppendLine("=== 트리별 성능 ===");
            foreach (var kvp in monitoredTrees)
            {
                var stats = kvp.Value;
                report.AppendLine($"- {kvp.Key}: 성공률 {stats.successRate:P1}, 평균 실행시간 {stats.averageExecutionTime:F2}ms");
            }
            report.AppendLine();
            
            // 최근 경고들
            var recentAlerts = activeAlerts.Where(a => !a.isResolved).Take(5);
            if (recentAlerts.Any())
            {
                report.AppendLine("=== 최근 경고 ===");
                foreach (var alert in recentAlerts)
                {
                    report.AppendLine($"- [{alert.severity}] {alert.source}: {alert.message}");
                }
            }
            
            return report.ToString();
        }
        
        #endregion
        
        #region Unity Inspector 지원
        
        [System.Serializable]
        public class InspectorInfo
        {
            public int monitoredTrees = 0;
            public float currentFPS = 0f;
            public float memoryUsage = 0f;
            public int activeAlerts = 0;
            public string lastAlert = "없음";
        }
        
        [SerializeField] private InspectorInfo inspectorInfo = new InspectorInfo();
        
        /// <summary>
        /// Inspector 정보 업데이트 (에디터에서만 호출)
        /// </summary>
        private void UpdateInspectorInfo()
        {
            #if UNITY_EDITOR
            inspectorInfo.monitoredTrees = monitoredTrees.Count;
            inspectorInfo.currentFPS = currentFPS;
            inspectorInfo.memoryUsage = currentMemoryUsage;
            inspectorInfo.activeAlerts = activeAlerts.Count(a => !a.isResolved);
            
            var latestAlert = activeAlerts.Where(a => !a.isResolved).OrderByDescending(a => a.timestamp).FirstOrDefault();
            inspectorInfo.lastAlert = latestAlert?.message ?? "없음";
            #endif
        }
        
        #endregion
    }
    
    #region 데이터 구조체들
    
    /// <summary>
    /// 성능 스냅샷
    /// </summary>
    [System.Serializable]
    public struct PerformanceSnapshot
    {
        public float timestamp;
        public float fps;
        public float frameTime;
        public float memoryUsage;
        public int activeTreeCount;
        public int totalTreeExecutions;
        public float averageSuccessRate;
    }
    
    /// <summary>
    /// 성능 경고
    /// </summary>
    [System.Serializable]
    public class PerformanceAlert
    {
        public string id;
        public AlertType type;
        public string source;
        public string message;
        public float timestamp;
        public AlertSeverity severity;
        public bool isResolved;
        public float resolvedTime;
    }
    
    /// <summary>
    /// 경고 타입
    /// </summary>
    public enum AlertType
    {
        LowFPS,
        HighMemoryUsage,
        HighExecutionTime,
        HighFailureRate,
        PerformanceBottleneck,
        LowEfficiency,
        PerformanceDegradation,
        MemoryLeak
    }
    
    /// <summary>
    /// 경고 심각도
    /// </summary>
    public enum AlertSeverity
    {
        Low,
        Medium,
        High
    }
    
    /// <summary>
    /// 현재 성능 상태
    /// </summary>
    [System.Serializable]
    public struct PerformanceStatus
    {
        public float fps;
        public float frameTime;
        public float memoryUsage;
        public int monitoredTreeCount;
        public int activeAlertCount;
        public float averageSuccessRate;
    }
    
    #endregion
}
