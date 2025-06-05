using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace LJH.BT
{
    /// <summary>
    /// BT 트리 전체 레벨의 통계 정보 관리
    /// 개별 노드 메타데이터를 집계하여 트리 전체의 성능을 분석
    /// ML 최적화와 성능 분석을 위한 종합적인 데이터 제공
    /// </summary>
    [System.Serializable]
    public class BTTreeStatistics : MonoBehaviour
    {
        [Header("트리 기본 정보")]
        public string treeName = "BT Tree";
        public string agentType = "Unknown";
        public int treeDepth = 0;
        public int totalNodeCount = 0;
        
        [Header("실행 통계")]
        public int totalTreeExecutions = 0;
        public int successfulExecutions = 0;
        public int failedExecutions = 0;
        public float averageExecutionTime = 0f;
        public float totalExecutionTime = 0f;
        
        [Header("노드 분석")]
        public Dictionary<string, NodeStatistics> nodeStats = new Dictionary<string, NodeStatistics>();
        public Dictionary<string, int> nodeTypeDistribution = new Dictionary<string, int>();
        public Dictionary<string, float> nodeExecutionFrequency = new Dictionary<string, float>();
        
        [Header("분기 패턴 분석")]
        public Dictionary<string, BranchPattern> branchPatterns = new Dictionary<string, BranchPattern>();
        public float averageBranchingFactor = 0f;
        public int maxExecutionDepth = 0;
        
        [Header("성능 메트릭")]
        public float successRate = 0f;
        public float averageNodesPerExecution = 0f;
        public float executionEfficiency = 0f; // 성공률 / 평균 실행 시간
        public float nodeUtilization = 0f; // 실제 사용된 노드 / 전체 노드
        
        [Header("ML 최적화 정보")]
        public List<ParameterCorrelation> parameterCorrelations = new List<ParameterCorrelation>();
        public List<PerformanceBottleneck> bottlenecks = new List<PerformanceBottleneck>();
        public Dictionary<string, float> optimizationPriorities = new Dictionary<string, float>();
        
        [Header("시간별 추세")]
        public List<ExecutionSnapshot> executionHistory = new List<ExecutionSnapshot>();
        public int maxHistorySize = 1000;
        
        /// <summary>
        /// 트리 통계 초기화
        /// </summary>
        public void Initialize(string name, string type, int depth, int nodeCount)
        {
            treeName = name;
            agentType = type;
            treeDepth = depth;
            totalNodeCount = nodeCount;
            
            // 컬렉션 초기화
            nodeStats.Clear();
            nodeTypeDistribution.Clear();
            nodeExecutionFrequency.Clear();
            branchPatterns.Clear();
            parameterCorrelations.Clear();
            bottlenecks.Clear();
            optimizationPriorities.Clear();
            executionHistory.Clear();
            
            // 통계 초기화
            ResetStatistics();
        }
        
        /// <summary>
        /// 노드 실행 결과 업데이트
        /// </summary>
        public void UpdateNodeExecution(string nodeName, string nodeType, NodeState result, float executionTime, NodeMetadata metadata)
        {
            // 노드별 통계 업데이트
            if (!nodeStats.ContainsKey(nodeName))
            {
                nodeStats[nodeName] = new NodeStatistics(nodeName, nodeType);
            }
            
            nodeStats[nodeName].UpdateExecution(result, executionTime, metadata);
            
            // 노드 타입별 분포 업데이트
            if (!nodeTypeDistribution.ContainsKey(nodeType))
            {
                nodeTypeDistribution[nodeType] = 0;
            }
            nodeTypeDistribution[nodeType]++;
            
            // 실행 빈도 업데이트
            if (!nodeExecutionFrequency.ContainsKey(nodeName))
            {
                nodeExecutionFrequency[nodeName] = 0f;
            }
            nodeExecutionFrequency[nodeName]++;
        }
        
        /// <summary>
        /// 트리 실행 완료 업데이트
        /// </summary>
        public void UpdateTreeExecution(NodeState finalResult, float totalTime, List<string> executedNodes, int executionDepth)
        {
            totalTreeExecutions++;
            totalExecutionTime += totalTime;
            averageExecutionTime = totalExecutionTime / totalTreeExecutions;
            
            // 결과별 카운트
            switch (finalResult)
            {
                case NodeState.Success:
                    successfulExecutions++;
                    break;
                case NodeState.Failure:
                    failedExecutions++;
                    break;
                // Running은 일반적으로 최종 결과가 아니므로 별도 처리하지 않음
            }
            
            // 성공률 계산
            int completedExecutions = successfulExecutions + failedExecutions;
            if (completedExecutions > 0)
            {
                successRate = (float)successfulExecutions / completedExecutions;
            }
            
            // 실행 깊이 업데이트
            maxExecutionDepth = Mathf.Max(maxExecutionDepth, executionDepth);
            
            // 평균 노드 실행 수 계산
            if (executedNodes != null)
            {
                averageNodesPerExecution = ((averageNodesPerExecution * (totalTreeExecutions - 1)) + executedNodes.Count) / totalTreeExecutions;
            }
            
            // 효율성 계산
            if (averageExecutionTime > 0)
            {
                executionEfficiency = successRate / averageExecutionTime;
            }
            
            // 실행 스냅샷 저장
            SaveExecutionSnapshot(finalResult, totalTime, executedNodes, executionDepth);
            
            // 주기적으로 분석 업데이트
            if (totalTreeExecutions % 10 == 0)
            {
                UpdateAnalytics();
            }
        }
        
        /// <summary>
        /// 분기 패턴 업데이트
        /// </summary>
        public void UpdateBranchPattern(string selectorName, int selectedBranch, List<string> availableBranches)
        {
            if (!branchPatterns.ContainsKey(selectorName))
            {
                branchPatterns[selectorName] = new BranchPattern(selectorName);
            }
            
            branchPatterns[selectorName].UpdateSelection(selectedBranch, availableBranches);
        }
        
        /// <summary>
        /// 파라미터 상관관계 추가
        /// </summary>
        public void AddParameterCorrelation(string nodeName, string parameterName, float parameterValue, float performanceMetric)
        {
            var correlation = parameterCorrelations.FirstOrDefault(c => c.nodeName == nodeName && c.parameterName == parameterName);
            if (correlation == null)
            {
                correlation = new ParameterCorrelation(nodeName, parameterName);
                parameterCorrelations.Add(correlation);
            }
            
            correlation.AddDataPoint(parameterValue, performanceMetric);
        }
        
        /// <summary>
        /// 성능 병목 지점 식별
        /// </summary>
        public void IdentifyBottlenecks(float timeThreshold = 5f, float failureRateThreshold = 0.3f)
        {
            bottlenecks.Clear();
            
            foreach (var kvp in nodeStats)
            {
                var stats = kvp.Value;
                
                // 시간 기반 병목
                if (stats.averageExecutionTime > timeThreshold)
                {
                    bottlenecks.Add(new PerformanceBottleneck
                    {
                        nodeName = stats.nodeName,
                        bottleneckType = BottleneckType.HighExecutionTime,
                        severity = stats.averageExecutionTime / timeThreshold,
                        description = $"평균 실행 시간이 {stats.averageExecutionTime:F2}ms로 임계값 {timeThreshold}ms를 초과"
                    });
                }
                
                // 실패율 기반 병목
                if (stats.failureRate > failureRateThreshold)
                {
                    bottlenecks.Add(new PerformanceBottleneck
                    {
                        nodeName = stats.nodeName,
                        bottleneckType = BottleneckType.HighFailureRate,
                        severity = stats.failureRate / failureRateThreshold,
                        description = $"실패율이 {stats.failureRate:P1}로 임계값 {failureRateThreshold:P1}를 초과"
                    });
                }
            }
            
            // 심각도순으로 정렬
            bottlenecks.Sort((a, b) => b.severity.CompareTo(a.severity));
        }
        
        /// <summary>
        /// 최적화 우선순위 계산
        /// </summary>
        public void CalculateOptimizationPriorities()
        {
            optimizationPriorities.Clear();
            
            foreach (var kvp in nodeStats)
            {
                var stats = kvp.Value;
                
                // 실행 빈도 × 개선 가능성 × 성능 영향도
                float frequency = nodeExecutionFrequency.GetValueOrDefault(stats.nodeName, 0f) / totalTreeExecutions;
                float improvementPotential = 1f - stats.successRate; // 개선 여지
                float performanceImpact = stats.averageExecutionTime / averageExecutionTime; // 상대적 성능 영향
                
                float priority = frequency * improvementPotential * performanceImpact;
                optimizationPriorities[stats.nodeName] = priority;
            }
            
            // 정규화 (0-1 범위로)
            if (optimizationPriorities.Count > 0)
            {
                float maxPriority = optimizationPriorities.Values.Max();
                if (maxPriority > 0)
                {
                    var normalizedPriorities = new Dictionary<string, float>();
                    foreach (var kvp in optimizationPriorities)
                    {
                        normalizedPriorities[kvp.Key] = kvp.Value / maxPriority;
                    }
                    optimizationPriorities = normalizedPriorities;
                }
            }
        }
        
        /// <summary>
        /// 실행 스냅샷 저장
        /// </summary>
        private void SaveExecutionSnapshot(NodeState result, float executionTime, List<string> executedNodes, int depth)
        {
            var snapshot = new ExecutionSnapshot
            {
                timestamp = Time.time,
                result = result,
                executionTime = executionTime,
                executedNodeCount = executedNodes?.Count ?? 0,
                executionDepth = depth,
                currentSuccessRate = successRate
            };
            
            executionHistory.Add(snapshot);
            
            // 히스토리 크기 제한
            if (executionHistory.Count > maxHistorySize)
            {
                executionHistory.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// 분석 데이터 업데이트
        /// </summary>
        private void UpdateAnalytics()
        {
            // 노드 활용도 계산
            int activeNodes = nodeStats.Count(kvp => kvp.Value.executionCount > 0);
            nodeUtilization = totalNodeCount > 0 ? (float)activeNodes / totalNodeCount : 0f;
            
            // 분기 인수 계산
            if (branchPatterns.Count > 0)
            {
                averageBranchingFactor = (float)branchPatterns.Values.Average(bp => bp.branchCount);
            }
            
            // 병목 지점 식별
            IdentifyBottlenecks();
            
            // 최적화 우선순위 계산
            CalculateOptimizationPriorities();
        }
        
        /// <summary>
        /// 통계 리셋
        /// </summary>
        public void ResetStatistics()
        {
            totalTreeExecutions = 0;
            successfulExecutions = 0;
            failedExecutions = 0;
            averageExecutionTime = 0f;
            totalExecutionTime = 0f;
            successRate = 0f;
            averageNodesPerExecution = 0f;
            executionEfficiency = 0f;
            nodeUtilization = 0f;
            averageBranchingFactor = 0f;
            maxExecutionDepth = 0;
            
            foreach (var stats in nodeStats.Values)
            {
                stats.Reset();
            }
            
            foreach (var pattern in branchPatterns.Values)
            {
                pattern.Reset();
            }
            
            parameterCorrelations.Clear();
            bottlenecks.Clear();
            optimizationPriorities.Clear();
            executionHistory.Clear();
        }
        
        /// <summary>
        /// 요약 리포트 생성
        /// </summary>
        public string GenerateSummaryReport()
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine($"=== BT Tree 성능 리포트: {treeName} ===");
            report.AppendLine($"Agent Type: {agentType}");
            report.AppendLine($"Tree Depth: {treeDepth}, Total Nodes: {totalNodeCount}");
            report.AppendLine();
            
            report.AppendLine("=== 실행 통계 ===");
            report.AppendLine($"총 실행 횟수: {totalTreeExecutions}");
            report.AppendLine($"성공률: {successRate:P1} ({successfulExecutions}/{successfulExecutions + failedExecutions})");
            report.AppendLine($"평균 실행 시간: {averageExecutionTime:F2}ms");
            report.AppendLine($"실행 효율성: {executionEfficiency:F3}");
            report.AppendLine($"노드 활용도: {nodeUtilization:P1}");
            report.AppendLine();
            
            report.AppendLine("=== 성능 병목 ===");
            if (bottlenecks.Count > 0)
            {
                foreach (var bottleneck in bottlenecks.Take(3)) // 상위 3개만
                {
                    report.AppendLine($"- {bottleneck.nodeName}: {bottleneck.description} (심각도: {bottleneck.severity:F2})");
                }
            }
            else
            {
                report.AppendLine("발견된 병목 없음");
            }
            report.AppendLine();
            
            report.AppendLine("=== 최적화 우선순위 (상위 5개) ===");
            var topPriorities = optimizationPriorities.OrderByDescending(kvp => kvp.Value).Take(5);
            foreach (var priority in topPriorities)
            {
                report.AppendLine($"- {priority.Key}: {priority.Value:F3}");
            }
            
            return report.ToString();
        }
    }
    
    /// <summary>
    /// 개별 노드 통계 정보
    /// </summary>
    [System.Serializable]
    public class NodeStatistics
    {
        public string nodeName;
        public string nodeType;
        public int executionCount = 0;
        public int successCount = 0;
        public int failureCount = 0;
        public int runningCount = 0;
        public float averageExecutionTime = 0f;
        public float totalExecutionTime = 0f;
        public float successRate = 0f;
        public float failureRate = 0f;
        public float lastExecutionTime = 0f;
        
        public NodeStatistics(string name, string type)
        {
            nodeName = name;
            nodeType = type;
        }
        
        public void UpdateExecution(NodeState result, float executionTime, NodeMetadata metadata)
        {
            executionCount++;
            lastExecutionTime = executionTime;
            totalExecutionTime += executionTime;
            averageExecutionTime = totalExecutionTime / executionCount;
            
            switch (result)
            {
                case NodeState.Success:
                    successCount++;
                    break;
                case NodeState.Failure:
                    failureCount++;
                    break;
                case NodeState.Running:
                    runningCount++;
                    break;
            }
            
            // 성공률/실패율 계산 (Running 제외)
            int completedExecutions = successCount + failureCount;
            if (completedExecutions > 0)
            {
                successRate = (float)successCount / completedExecutions;
                failureRate = (float)failureCount / completedExecutions;
            }
        }
        
        public void Reset()
        {
            executionCount = 0;
            successCount = 0;
            failureCount = 0;
            runningCount = 0;
            averageExecutionTime = 0f;
            totalExecutionTime = 0f;
            successRate = 0f;
            failureRate = 0f;
            lastExecutionTime = 0f;
        }
    }
    
    /// <summary>
    /// 분기 패턴 정보 (Selector 노드용)
    /// </summary>
    [System.Serializable]
    public class BranchPattern
    {
        public string selectorName;
        public int branchCount = 0;
        public Dictionary<int, int> branchSelections = new Dictionary<int, int>();
        public Dictionary<int, float> branchProbabilities = new Dictionary<int, float>();
        public int totalSelections = 0;
        
        public BranchPattern(string name)
        {
            selectorName = name;
        }
        
        public void UpdateSelection(int selectedBranch, List<string> availableBranches)
        {
            branchCount = availableBranches.Count;
            totalSelections++;
            
            if (!branchSelections.ContainsKey(selectedBranch))
            {
                branchSelections[selectedBranch] = 0;
            }
            branchSelections[selectedBranch]++;
            
            // 확률 업데이트
            branchProbabilities.Clear();
            foreach (var kvp in branchSelections)
            {
                branchProbabilities[kvp.Key] = (float)kvp.Value / totalSelections;
            }
        }
        
        public void Reset()
        {
            branchSelections.Clear();
            branchProbabilities.Clear();
            totalSelections = 0;
        }
    }
    
    /// <summary>
    /// 파라미터 상관관계 정보
    /// </summary>
    [System.Serializable]
    public class ParameterCorrelation
    {
        public string nodeName;
        public string parameterName;
        public List<float> parameterValues = new List<float>();
        public List<float> performanceValues = new List<float>();
        public float correlationCoefficient = 0f;
        
        public ParameterCorrelation(string node, string parameter)
        {
            nodeName = node;
            parameterName = parameter;
        }
        
        public void AddDataPoint(float paramValue, float performanceValue)
        {
            parameterValues.Add(paramValue);
            performanceValues.Add(performanceValue);
            
            // 상관계수 계산 (최소 3개 데이터 포인트 필요)
            if (parameterValues.Count >= 3)
            {
                correlationCoefficient = CalculateCorrelation();
            }
        }
        
        private float CalculateCorrelation()
        {
            if (parameterValues.Count != performanceValues.Count || parameterValues.Count < 2)
                return 0f;
            
            float meanX = (float)parameterValues.Average();
            float meanY = (float)performanceValues.Average();
            
            float numerator = 0f;
            float denomX = 0f;
            float denomY = 0f;
            
            for (int i = 0; i < parameterValues.Count; i++)
            {
                float deltaX = parameterValues[i] - meanX;
                float deltaY = performanceValues[i] - meanY;
                
                numerator += deltaX * deltaY;
                denomX += deltaX * deltaX;
                denomY += deltaY * deltaY;
            }
            
            float denominator = Mathf.Sqrt((float)(denomX * denomY));
            return denominator > 0f ? numerator / denominator : 0f;
        }
    }
    
    /// <summary>
    /// 성능 병목 정보
    /// </summary>
    [System.Serializable]
    public class PerformanceBottleneck
    {
        public string nodeName;
        public BottleneckType bottleneckType;
        public float severity; // 심각도 (1.0 = 임계값, 2.0 = 임계값의 2배 등)
        public string description;
        public string recommendation; // 최적화 제안
    }
    
    /// <summary>
    /// 병목 유형
    /// </summary>
    public enum BottleneckType
    {
        HighExecutionTime,
        HighFailureRate,
        FrequentExecution,
        MemoryUsage,
        ParameterInefficiency
    }
    
    /// <summary>
    /// 실행 스냅샷 (시간별 추세 분석용)
    /// </summary>
    [System.Serializable]
    public class ExecutionSnapshot
    {
        public float timestamp;
        public NodeState result;
        public float executionTime;
        public int executedNodeCount;
        public int executionDepth;
        public float currentSuccessRate;
    }
}
