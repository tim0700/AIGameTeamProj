using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LJH.BT
{
    /// <summary>
    /// ML 최적화를 위한 데이터 수집, 분석, 익스포트 매니저
    /// 파라미터 최적화, 성능 분석, A/B 테스트 등 ML 기반 최적화를 지원
    /// </summary>
    public class MLOptimizationManager : MonoBehaviour
    {
        [Header("ML 최적화 설정")]
        [SerializeField] private bool enableMLOptimization = true;
        [SerializeField] private bool enableDataCollection = true;
        [SerializeField] private int maxDataPoints = 10000; // 최대 데이터 포인트 수
        [SerializeField] private float dataCollectionInterval = 5f; // 데이터 수집 주기 (초)
        
        [Header("실험 설정")]
        #pragma warning disable 0414
        [SerializeField] private bool enableABTesting = false;
        #pragma warning restore 0414
        [SerializeField] private int experimentDuration = 300; // 실험 기간 (초)
        #pragma warning disable 0414
        [SerializeField] private float significanceLevel = 0.05f; // 통계적 유의수준
        #pragma warning restore 0414
        
        [Header("최적화 알고리즘")]
        [SerializeField] private OptimizationAlgorithm optimizationAlgorithm = OptimizationAlgorithm.BayesianOptimization;
        [SerializeField] private int maxOptimizationIterations = 100;
        [SerializeField] private float convergenceThreshold = 0.001f;
        
        [Header("데이터 저장")]
        [SerializeField] private bool enableDataPersistence = true;
        [SerializeField] private string dataExportPath = "Assets/MLData/";
        #pragma warning disable 0414
        [SerializeField] private MLDataFormat exportFormat = MLDataFormat.JSON;
        #pragma warning restore 0414
        
        // 내부 데이터 구조
        private Dictionary<string, MLDataset> treeDatasets = new Dictionary<string, MLDataset>();
        private Dictionary<string, ExperimentSession> activeExperiments = new Dictionary<string, ExperimentSession>();
        private List<OptimizationResult> optimizationHistory = new List<OptimizationResult>();
        
        // 성능 추적
        private Dictionary<string, List<PerformanceMetric>> performanceHistory = new Dictionary<string, List<PerformanceMetric>>();
        private Dictionary<string, ParameterSpace> parameterSpaces = new Dictionary<string, ParameterSpace>();
        
        // 최적화 상태
        private bool isOptimizing = false;
        private float lastDataCollectionTime = 0f;
        private int currentOptimizationIteration = 0;
        
        // 이벤트 시스템
        public System.Action<string, OptimizationResult> OnOptimizationCompleted;
        public System.Action<string, ExperimentResult> OnExperimentCompleted;
        public System.Action<string, float> OnParameterUpdated;
        
        #region Unity 생명주기
        
        private void Start()
        {
            if (enableMLOptimization)
            {
                InitializeMLSystem();
            }
        }
        
        private void Update()
        {
            if (!enableMLOptimization) return;
            
            // 주기적 데이터 수집
            if (enableDataCollection && Time.time - lastDataCollectionTime >= dataCollectionInterval)
            {
                CollectMLData();
                lastDataCollectionTime = Time.time;
            }
            
            // 실행 중인 실험 업데이트
            UpdateActiveExperiments();
        }
        
        private void OnDestroy()
        {
            // 실행 중인 최적화나 실험이 있으면 저장
            if (enableDataPersistence)
            {
                SaveAllData();
            }
        }
        
        #endregion
        
        #region 초기화 및 설정
        
        /// <summary>
        /// ML 시스템 초기화
        /// </summary>
        private void InitializeMLSystem()
        {
            // 데이터 폴더 생성
            if (enableDataPersistence && !Directory.Exists(dataExportPath))
            {
                Directory.CreateDirectory(dataExportPath);
            }
            
            // 기존 데이터 로드
            if (enableDataPersistence)
            {
                LoadExistingData();
            }
            
            Debug.Log("[MLOptimizationManager] ML 최적화 시스템이 초기화되었습니다.");
        }
        
        /// <summary>
        /// BT 트리 등록
        /// </summary>
        public void RegisterTree(string treeName, BTTreeStatistics treeStats, Dictionary<string, INodeParameters> nodeParameters)
        {
            if (string.IsNullOrEmpty(treeName)) return;
            
            // 데이터셋 초기화
            if (!treeDatasets.ContainsKey(treeName))
            {
                treeDatasets[treeName] = new MLDataset(treeName);
            }
            
            // 성능 히스토리 초기화
            if (!performanceHistory.ContainsKey(treeName))
            {
                performanceHistory[treeName] = new List<PerformanceMetric>();
            }
            
            // 파라미터 공간 정의
            DefineParameterSpace(treeName, nodeParameters);
            
            Debug.Log($"[MLOptimizationManager] BT 트리 '{treeName}'가 ML 최적화에 등록되었습니다.");
        }
        
        /// <summary>
        /// 파라미터 공간 정의
        /// </summary>
        private void DefineParameterSpace(string treeName, Dictionary<string, INodeParameters> nodeParameters)
        {
            var parameterSpace = new ParameterSpace(treeName);
            
            foreach (var kvp in nodeParameters)
            {
                var nodeName = kvp.Key;
                var parameters = kvp.Value;
                
                if (parameters != null)
                {
                    var constraints = parameters.GetConstraints();
                    var parameterVector = new ParameterVector
                    {
                        nodeName = nodeName,
                        parameterNames = constraints.paramNames.ToList(),
                        minValues = constraints.minValues.ToList(),
                        maxValues = constraints.maxValues.ToList(),
                        isInteger = constraints.isInteger.ToList(),
                        currentValues = parameters.ToArray().ToList()
                    };
                    
                    parameterSpace.AddParameterVector(parameterVector);
                }
            }
            
            parameterSpaces[treeName] = parameterSpace;
        }
        
        #endregion
        
        #region 데이터 수집
        
        /// <summary>
        /// ML 데이터 수집
        /// </summary>
        private void CollectMLData()
        {
            foreach (var kvp in treeDatasets)
            {
                var treeName = kvp.Key;
                var dataset = kvp.Value;
                
                // PerformanceMonitor에서 현재 성능 데이터 가져오기
                var performanceMonitor = FindFirstObjectByType<PerformanceMonitor>();
                if (performanceMonitor != null)
                {
                    CollectPerformanceData(treeName, performanceMonitor);
                }
            }
        }
        
        /// <summary>
        /// 성능 데이터 수집
        /// </summary>
        private void CollectPerformanceData(string treeName, PerformanceMonitor monitor)
        {
            var performanceStatus = monitor.GetCurrentPerformanceStatus();
            
            var metric = new PerformanceMetric
            {
                timestamp = Time.time,
                successRate = performanceStatus.averageSuccessRate,
                executionTime = performanceStatus.frameTime,
                memoryUsage = performanceStatus.memoryUsage,
                fps = performanceStatus.fps,
                efficiency = performanceStatus.averageSuccessRate / Mathf.Max(performanceStatus.frameTime, 0.001f)
            };
            
            // 성능 히스토리에 추가
            if (!performanceHistory.ContainsKey(treeName))
            {
                performanceHistory[treeName] = new List<PerformanceMetric>();
            }
            
            performanceHistory[treeName].Add(metric);
            
            // 히스토리 크기 제한
            if (performanceHistory[treeName].Count > maxDataPoints)
            {
                performanceHistory[treeName].RemoveAt(0);
            }
            
            // 데이터셋에 추가
            if (parameterSpaces.ContainsKey(treeName))
            {
                var currentParameters = GetCurrentParameters(treeName);
                treeDatasets[treeName].AddDataPoint(currentParameters, metric);
            }
        }
        
        /// <summary>
        /// 현재 파라미터 값 가져오기
        /// </summary>
        private List<float> GetCurrentParameters(string treeName)
        {
            var parameters = new List<float>();
            
            if (parameterSpaces.ContainsKey(treeName))
            {
                var parameterSpace = parameterSpaces[treeName];
                foreach (var vector in parameterSpace.parameterVectors)
                {
                    parameters.AddRange(vector.currentValues);
                }
            }
            
            return parameters;
        }
        
        #endregion
        
        #region 파라미터 최적화
        
        /// <summary>
        /// 파라미터 최적화 시작
        /// </summary>
        public void StartOptimization(string treeName, ObjectiveFunction objectiveFunction = ObjectiveFunction.SuccessRate)
        {
            if (isOptimizing)
            {
                Debug.LogWarning("[MLOptimizationManager] 이미 최적화가 진행 중입니다.");
                return;
            }
            
            if (!treeDatasets.ContainsKey(treeName) || !parameterSpaces.ContainsKey(treeName))
            {
                Debug.LogError($"[MLOptimizationManager] 트리 '{treeName}'가 등록되지 않았습니다.");
                return;
            }
            
            isOptimizing = true;
            currentOptimizationIteration = 0;
            
            Debug.Log($"[MLOptimizationManager] '{treeName}' 트리의 파라미터 최적화를 시작합니다. 알고리즘: {optimizationAlgorithm}");
            
            StartCoroutine(OptimizationCoroutine(treeName, objectiveFunction));
        }
        
        /// <summary>
        /// 최적화 코루틴
        /// </summary>
        private System.Collections.IEnumerator OptimizationCoroutine(string treeName, ObjectiveFunction objectiveFunction)
        {
            var dataset = treeDatasets[treeName];
            var parameterSpace = parameterSpaces[treeName];
            var bestParameters = GetCurrentParameters(treeName);
            var bestScore = EvaluateObjectiveFunction(treeName, objectiveFunction);
            
            for (int iteration = 0; iteration < maxOptimizationIterations; iteration++)
            {
                currentOptimizationIteration = iteration;
                
                // 알고리즘에 따른 다음 파라미터 제안
                var candidateParameters = GenerateCandidateParameters(treeName, iteration);
                
                // 파라미터 적용
                ApplyParameters(treeName, candidateParameters);
                
                // 성능 측정을 위한 대기 (실제 성능 데이터 수집)
                yield return new WaitForSeconds(dataCollectionInterval * 2f);
                
                // 성능 평가
                var currentScore = EvaluateObjectiveFunction(treeName, objectiveFunction);
                
                // 베스트 스코어 업데이트
                if (currentScore > bestScore)
                {
                    bestScore = currentScore;
                    bestParameters = new List<float>(candidateParameters);
                    
                    Debug.Log($"[MLOptimizationManager] 새로운 최적 파라미터 발견! 스코어: {bestScore:F4}");
                }
                
                // 수렴 체크
                if (iteration > 10) // 최소 10회 반복 후 수렴 체크
                {
                    var recentScores = GetRecentScores(treeName, 5);
                    if (HasConverged(recentScores))
                    {
                        Debug.Log($"[MLOptimizationManager] 최적화가 수렴했습니다. (반복: {iteration + 1})");
                        break;
                    }
                }
                
                yield return null; // 프레임 양보
            }
            
            // 최적 파라미터 적용
            ApplyParameters(treeName, bestParameters);
            
            // 최적화 결과 저장
            var result = new OptimizationResult
            {
                treeName = treeName,
                algorithm = optimizationAlgorithm,
                objectiveFunction = objectiveFunction,
                initialScore = EvaluateObjectiveFunction(treeName, objectiveFunction), // 초기 스코어는 별도 계산 필요
                finalScore = bestScore,
                improvementPercentage = 0f, // 계산 필요
                iterations = currentOptimizationIteration + 1,
                optimizedParameters = bestParameters,
                timestamp = System.DateTime.Now
            };
            
            optimizationHistory.Add(result);
            isOptimizing = false;
            
            Debug.Log($"[MLOptimizationManager] 최적화 완료! 최종 스코어: {bestScore:F4}");
            OnOptimizationCompleted?.Invoke(treeName, result);
        }
        
        /// <summary>
        /// 후보 파라미터 생성
        /// </summary>
        private List<float> GenerateCandidateParameters(string treeName, int iteration)
        {
            var parameterSpace = parameterSpaces[treeName];
            var dataset = treeDatasets[treeName];
            
            switch (optimizationAlgorithm)
            {
                case OptimizationAlgorithm.RandomSearch:
                    return GenerateRandomParameters(parameterSpace);
                    
                case OptimizationAlgorithm.GridSearch:
                    return GenerateGridSearchParameters(parameterSpace, iteration);
                    
                case OptimizationAlgorithm.BayesianOptimization:
                    return GenerateBayesianParameters(parameterSpace, dataset);
                    
                case OptimizationAlgorithm.GeneticAlgorithm:
                    return GenerateGeneticParameters(parameterSpace, dataset);
                    
                default:
                    return GenerateRandomParameters(parameterSpace);
            }
        }
        
        /// <summary>
        /// 랜덤 파라미터 생성
        /// </summary>
        private List<float> GenerateRandomParameters(ParameterSpace parameterSpace)
        {
            var parameters = new List<float>();
            
            foreach (var vector in parameterSpace.parameterVectors)
            {
                for (int i = 0; i < vector.parameterNames.Count; i++)
                {
                    float min = vector.minValues[i];
                    float max = vector.maxValues[i];
                    bool isInt = vector.isInteger[i];
                    
                    float value = Random.Range(min, max);
                    if (isInt) value = Mathf.Round(value);
                    
                    parameters.Add(value);
                }
            }
            
            return parameters;
        }
        
        /// <summary>
        /// 그리드 서치 파라미터 생성
        /// </summary>
        private List<float> GenerateGridSearchParameters(ParameterSpace parameterSpace, int iteration)
        {
            // 간단한 그리드 서치 구현
            var parameters = new List<float>();
            int totalParams = parameterSpace.GetTotalParameterCount();
            int gridSize = Mathf.FloorToInt(Mathf.Pow(maxOptimizationIterations, 1f / totalParams));
            
            foreach (var vector in parameterSpace.parameterVectors)
            {
                for (int i = 0; i < vector.parameterNames.Count; i++)
                {
                    float min = vector.minValues[i];
                    float max = vector.maxValues[i];
                    bool isInt = vector.isInteger[i];
                    
                    int gridIndex = (iteration / (int)Mathf.Pow(gridSize, parameters.Count)) % gridSize;
                    float value = min + (max - min) * gridIndex / (gridSize - 1);
                    if (isInt) value = Mathf.Round(value);
                    
                    parameters.Add(value);
                }
            }
            
            return parameters;
        }
        
        /// <summary>
        /// 베이지안 최적화 파라미터 생성 (단순화된 버전)
        /// </summary>
        private List<float> GenerateBayesianParameters(ParameterSpace parameterSpace, MLDataset dataset)
        {
            // 실제 베이지안 최적화는 복잡하므로 단순화된 버전 구현
            // 과거 성공적인 파라미터를 기반으로 주변 탐색
            
            if (dataset.dataPoints.Count < 5)
            {
                return GenerateRandomParameters(parameterSpace);
            }
            
            // 성능이 좋은 상위 20% 데이터 포인트 선택
            var sortedPoints = dataset.dataPoints
                .OrderByDescending(dp => dp.performanceMetric.efficiency)
                .Take(Mathf.Max(1, dataset.dataPoints.Count / 5))
                .ToList();
            
            // 평균 파라미터 계산
            var avgParameters = new List<float>();
            int paramCount = sortedPoints.First().parameters.Count;
            
            for (int i = 0; i < paramCount; i++)
            {
                float avg = sortedPoints.Average(dp => dp.parameters[i]);
                avgParameters.Add(avg);
            }
            
            // 평균 주변에서 노이즈를 추가하여 새로운 파라미터 생성
            var parameters = new List<float>();
            int paramIndex = 0;
            
            foreach (var vector in parameterSpace.parameterVectors)
            {
                for (int i = 0; i < vector.parameterNames.Count; i++)
                {
                    float baseValue = avgParameters[paramIndex];
                    float min = vector.minValues[i];
                    float max = vector.maxValues[i];
                    bool isInt = vector.isInteger[i];
                    
                    // 가우시안 노이즈 추가 (표준편차는 범위의 10%)
                    float noise = Random.Range(-0.1f, 0.1f) * (max - min);
                    float value = Mathf.Clamp(baseValue + noise, min, max);
                    if (isInt) value = Mathf.Round(value);
                    
                    parameters.Add(value);
                    paramIndex++;
                }
            }
            
            return parameters;
        }
        
        /// <summary>
        /// 유전 알고리즘 파라미터 생성 (단순화된 버전)
        /// </summary>
        private List<float> GenerateGeneticParameters(ParameterSpace parameterSpace, MLDataset dataset)
        {
            // 단순화된 유전 알고리즘 구현
            if (dataset.dataPoints.Count < 10)
            {
                return GenerateRandomParameters(parameterSpace);
            }
            
            // 엘리트 선택 (상위 50%)
            var elites = dataset.dataPoints
                .OrderByDescending(dp => dp.performanceMetric.efficiency)
                .Take(dataset.dataPoints.Count / 2)
                .ToList();
            
            // 두 부모 선택
            var parent1 = elites[Random.Range(0, elites.Count)];
            var parent2 = elites[Random.Range(0, elites.Count)];
            
            // 교차 (단일점 교차)
            var crossoverPoint = Random.Range(1, parent1.parameters.Count);
            var offspring = new List<float>();
            
            for (int i = 0; i < parent1.parameters.Count; i++)
            {
                offspring.Add(i < crossoverPoint ? parent1.parameters[i] : parent2.parameters[i]);
            }
            
            // 돌연변이 (10% 확률)
            int paramIndex = 0;
            foreach (var vector in parameterSpace.parameterVectors)
            {
                for (int i = 0; i < vector.parameterNames.Count; i++)
                {
                    if (Random.Range(0f, 1f) < 0.1f) // 10% 돌연변이 확률
                    {
                        float min = vector.minValues[i];
                        float max = vector.maxValues[i];
                        bool isInt = vector.isInteger[i];
                        
                        float value = Random.Range(min, max);
                        if (isInt) value = Mathf.Round(value);
                        
                        offspring[paramIndex] = value;
                    }
                    paramIndex++;
                }
            }
            
            return offspring;
        }
        
        /// <summary>
        /// 파라미터 적용
        /// </summary>
        private void ApplyParameters(string treeName, List<float> parameters)
        {
            if (!parameterSpaces.ContainsKey(treeName)) return;
            
            var parameterSpace = parameterSpaces[treeName];
            int paramIndex = 0;
            
            foreach (var vector in parameterSpace.parameterVectors)
            {
                var nodeParameters = new List<float>();
                for (int i = 0; i < vector.parameterNames.Count; i++)
                {
                    if (paramIndex < parameters.Count)
                    {
                        nodeParameters.Add(parameters[paramIndex]);
                        vector.currentValues[i] = parameters[paramIndex];
                        paramIndex++;
                    }
                }
                
                // 실제 노드에 파라미터 적용하는 로직이 필요
                // 이 부분은 BTNodeBase의 SetParameters 메서드를 통해 구현
                OnParameterUpdated?.Invoke($"{treeName}.{vector.nodeName}", nodeParameters.Average());
            }
        }
        
        /// <summary>
        /// 목적 함수 평가
        /// </summary>
        private float EvaluateObjectiveFunction(string treeName, ObjectiveFunction objectiveFunction)
        {
            if (!performanceHistory.ContainsKey(treeName) || performanceHistory[treeName].Count == 0)
            {
                return 0f;
            }
            
            var recentMetrics = performanceHistory[treeName].TakeLast(5).ToList();
            
            switch (objectiveFunction)
            {
                case ObjectiveFunction.SuccessRate:
                    return recentMetrics.Average(m => m.successRate);
                    
                case ObjectiveFunction.ExecutionSpeed:
                    return 1f / Mathf.Max(recentMetrics.Average(m => m.executionTime), 0.001f);
                    
                case ObjectiveFunction.Efficiency:
                    return recentMetrics.Average(m => m.efficiency);
                    
                case ObjectiveFunction.MemoryEfficiency:
                    return 1f / Mathf.Max(recentMetrics.Average(m => m.memoryUsage), 0.001f);
                    
                case ObjectiveFunction.OverallPerformance:
                    var successRate = recentMetrics.Average(m => m.successRate);
                    var speed = 1f / Mathf.Max(recentMetrics.Average(m => m.executionTime), 0.001f);
                    var memEfficiency = 1f / Mathf.Max(recentMetrics.Average(m => m.memoryUsage), 0.001f);
                    return (successRate * 0.5f + speed * 0.3f + memEfficiency * 0.2f);
                    
                default:
                    return recentMetrics.Average(m => m.efficiency);
            }
        }
        
        /// <summary>
        /// 최근 스코어 가져오기
        /// </summary>
        private List<float> GetRecentScores(string treeName, int count)
        {
            if (!performanceHistory.ContainsKey(treeName))
            {
                return new List<float>();
            }
            
            return performanceHistory[treeName]
                .TakeLast(count)
                .Select(m => m.efficiency)
                .ToList();
        }
        
        /// <summary>
        /// 수렴 체크
        /// </summary>
        private bool HasConverged(List<float> recentScores)
        {
            if (recentScores.Count < 3) return false;
            
            var variance = CalculateVariance(recentScores);
            return variance < convergenceThreshold;
        }
        
        /// <summary>
        /// 분산 계산
        /// </summary>
        private float CalculateVariance(List<float> values)
        {
            if (values.Count == 0) return 0f;
            
            float mean = values.Average();
            float sumSquaredDifferences = values.Sum(v => (v - mean) * (v - mean));
            return sumSquaredDifferences / values.Count;
        }
        
        #endregion
        
        #region A/B 테스트
        
        /// <summary>
        /// A/B 테스트 시작
        /// </summary>
        public void StartABTest(string treeName, string testName, List<float> parametersA, List<float> parametersB)
        {
            if (activeExperiments.ContainsKey(testName))
            {
                Debug.LogWarning($"[MLOptimizationManager] A/B 테스트 '{testName}'가 이미 진행 중입니다.");
                return;
            }
            
            var experiment = new ExperimentSession
            {
                testName = testName,
                treeName = treeName,
                startTime = Time.time,
                duration = experimentDuration,
                groupAParameters = parametersA,
                groupBParameters = parametersB,
                groupAMetrics = new List<PerformanceMetric>(),
                groupBMetrics = new List<PerformanceMetric>(),
                currentGroup = ExperimentGroup.GroupA,
                switchInterval = experimentDuration / 4f // 4번 그룹 전환
            };
            
            activeExperiments[testName] = experiment;
            
            // 그룹 A 파라미터로 시작
            ApplyParameters(treeName, parametersA);
            
            Debug.Log($"[MLOptimizationManager] A/B 테스트 '{testName}'가 시작되었습니다.");
        }
        
        /// <summary>
        /// 활성 실험 업데이트
        /// </summary>
        private void UpdateActiveExperiments()
        {
            var completedExperiments = new List<string>();
            
            foreach (var kvp in activeExperiments)
            {
                var experiment = kvp.Value;
                var elapsedTime = Time.time - experiment.startTime;
                
                // 그룹 전환 체크
                if (elapsedTime > experiment.nextSwitchTime)
                {
                    SwitchExperimentGroup(experiment);
                }
                
                // 현재 성능 데이터 수집
                CollectExperimentData(experiment);
                
                // 실험 완료 체크
                if (elapsedTime >= experiment.duration)
                {
                    CompleteExperiment(kvp.Key, experiment);
                    completedExperiments.Add(kvp.Key);
                }
            }
            
            // 완료된 실험 제거
            foreach (var testName in completedExperiments)
            {
                activeExperiments.Remove(testName);
            }
        }
        
        /// <summary>
        /// 실험 그룹 전환
        /// </summary>
        private void SwitchExperimentGroup(ExperimentSession experiment)
        {
            experiment.currentGroup = experiment.currentGroup == ExperimentGroup.GroupA ? 
                ExperimentGroup.GroupB : ExperimentGroup.GroupA;
            
            experiment.nextSwitchTime = Time.time + experiment.switchInterval;
            
            // 해당 그룹의 파라미터 적용
            var parameters = experiment.currentGroup == ExperimentGroup.GroupA ? 
                experiment.groupAParameters : experiment.groupBParameters;
            
            ApplyParameters(experiment.treeName, parameters);
            
            Debug.Log($"[MLOptimizationManager] A/B 테스트 그룹 전환: {experiment.currentGroup}");
        }
        
        /// <summary>
        /// 실험 데이터 수집
        /// </summary>
        private void CollectExperimentData(ExperimentSession experiment)
        {
            if (!performanceHistory.ContainsKey(experiment.treeName) || 
                performanceHistory[experiment.treeName].Count == 0)
            {
                return;
            }
            
            var latestMetric = performanceHistory[experiment.treeName].Last();
            
            if (experiment.currentGroup == ExperimentGroup.GroupA)
            {
                experiment.groupAMetrics.Add(latestMetric);
            }
            else
            {
                experiment.groupBMetrics.Add(latestMetric);
            }
        }
        
        /// <summary>
        /// 실험 완료
        /// </summary>
        private void CompleteExperiment(string testName, ExperimentSession experiment)
        {
            var result = AnalyzeExperimentResults(experiment);
            
            Debug.Log($"[MLOptimizationManager] A/B 테스트 '{testName}' 완료!");
            Debug.Log($"그룹 A 평균 효율성: {result.groupAAverage:F4}");
            Debug.Log($"그룹 B 평균 효율성: {result.groupBAverage:F4}");
            Debug.Log($"통계적 유의성: {(result.isStatisticallySignificant ? "유의함" : "유의하지 않음")}");
            
            // 더 좋은 파라미터 적용
            if (result.winningGroup == ExperimentGroup.GroupA)
            {
                ApplyParameters(experiment.treeName, experiment.groupAParameters);
            }
            else if (result.winningGroup == ExperimentGroup.GroupB)
            {
                ApplyParameters(experiment.treeName, experiment.groupBParameters);
            }
            
            OnExperimentCompleted?.Invoke(testName, result);
        }
        
        /// <summary>
        /// 실험 결과 분석
        /// </summary>
        private ExperimentResult AnalyzeExperimentResults(ExperimentSession experiment)
        {
            var result = new ExperimentResult
            {
                testName = experiment.testName,
                groupAAverage = experiment.groupAMetrics.Count > 0 ? 
                    experiment.groupAMetrics.Average(m => m.efficiency) : 0f,
                groupBAverage = experiment.groupBMetrics.Count > 0 ? 
                    experiment.groupBMetrics.Average(m => m.efficiency) : 0f,
                groupAVariance = CalculateVariance(experiment.groupAMetrics.Select(m => m.efficiency).ToList()),
                groupBVariance = CalculateVariance(experiment.groupBMetrics.Select(m => m.efficiency).ToList()),
                sampleSizeA = experiment.groupAMetrics.Count,
                sampleSizeB = experiment.groupBMetrics.Count
            };
            
            // 승리 그룹 결정
            if (result.groupAAverage > result.groupBAverage)
            {
                result.winningGroup = ExperimentGroup.GroupA;
                result.improvementPercentage = (result.groupAAverage - result.groupBAverage) / result.groupBAverage * 100f;
            }
            else if (result.groupBAverage > result.groupAAverage)
            {
                result.winningGroup = ExperimentGroup.GroupB;
                result.improvementPercentage = (result.groupBAverage - result.groupAAverage) / result.groupAAverage * 100f;
            }
            else
            {
                result.winningGroup = ExperimentGroup.None;
                result.improvementPercentage = 0f;
            }
            
            // 통계적 유의성 검정 (단순화된 t-검정)
            result.isStatisticallySignificant = PerformTTest(
                experiment.groupAMetrics.Select(m => m.efficiency).ToList(),
                experiment.groupBMetrics.Select(m => m.efficiency).ToList()
            );
            
            return result;
        }
        
        /// <summary>
        /// 단순화된 t-검정
        /// </summary>
        private bool PerformTTest(List<float> groupA, List<float> groupB)
        {
            if (groupA.Count < 2 || groupB.Count < 2) return false;
            
            float meanA = groupA.Average();
            float meanB = groupB.Average();
            float varA = CalculateVariance(groupA);
            float varB = CalculateVariance(groupB);
            
            // 풀드 분산 계산
            float pooledVariance = ((groupA.Count - 1) * varA + (groupB.Count - 1) * varB) / 
                                  (groupA.Count + groupB.Count - 2);
            
            // t 통계량 계산
            float standardError = Mathf.Sqrt(pooledVariance * (1f / groupA.Count + 1f / groupB.Count));
            float tStatistic = Mathf.Abs(meanA - meanB) / standardError;
            
            // 자유도
            int degreesOfFreedom = groupA.Count + groupB.Count - 2;
            
            // 단순화된 임계값 사용 (더 정확한 구현에서는 t-분포표 사용)
            float criticalValue = 2.0f; // 대략적인 0.05 유의수준 임계값
            
            return tStatistic > criticalValue;
        }
        
        #endregion
        
        #region 데이터 저장/로드
        
        /// <summary>
        /// 모든 데이터 저장
        /// </summary>
        public void SaveAllData()
        {
            if (!enableDataPersistence) return;
            
            try
            {
                // 데이터셋 저장
                foreach (var kvp in treeDatasets)
                {
                    SaveDataset(kvp.Key, kvp.Value);
                }
                
                // 최적화 히스토리 저장
                SaveOptimizationHistory();
                
                Debug.Log("[MLOptimizationManager] 모든 ML 데이터가 저장되었습니다.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MLOptimizationManager] 데이터 저장 중 오류 발생: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 데이터셋 저장
        /// </summary>
        private void SaveDataset(string treeName, MLDataset dataset)
        {
            string filename = $"MLDataset_{treeName}_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filepath = Path.Combine(dataExportPath, filename);
            
            string jsonData = JsonUtility.ToJson(dataset, true);
            File.WriteAllText(filepath, jsonData);
        }
        
        /// <summary>
        /// 최적화 히스토리 저장
        /// </summary>
        private void SaveOptimizationHistory()
        {
            string filename = $"OptimizationHistory_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
            string filepath = Path.Combine(dataExportPath, filename);
            
            var historyData = new OptimizationHistoryData { results = optimizationHistory };
            string jsonData = JsonUtility.ToJson(historyData, true);
            File.WriteAllText(filepath, jsonData);
        }
        
        /// <summary>
        /// 기존 데이터 로드
        /// </summary>
        private void LoadExistingData()
        {
            try
            {
                if (Directory.Exists(dataExportPath))
                {
                    // 최신 최적화 히스토리 파일 로드
                    var historyFiles = Directory.GetFiles(dataExportPath, "OptimizationHistory_*.json");
                    if (historyFiles.Length > 0)
                    {
                        var latestFile = historyFiles.OrderByDescending(f => File.GetCreationTime(f)).First();
                        LoadOptimizationHistory(latestFile);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MLOptimizationManager] 기존 데이터 로드 중 오류 발생: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 최적화 히스토리 로드
        /// </summary>
        private void LoadOptimizationHistory(string filepath)
        {
            string jsonData = File.ReadAllText(filepath);
            var historyData = JsonUtility.FromJson<OptimizationHistoryData>(jsonData);
            optimizationHistory = historyData.results;
            
            Debug.Log($"[MLOptimizationManager] 최적화 히스토리 로드 완료: {optimizationHistory.Count}개 결과");
        }
        
        #endregion
        
        #region 공개 API
        
        /// <summary>
        /// 최적화 상태 확인
        /// </summary>
        public bool IsOptimizing() => isOptimizing;
        
        /// <summary>
        /// 최적화 히스토리 반환
        /// </summary>
        public List<OptimizationResult> GetOptimizationHistory() => new List<OptimizationResult>(optimizationHistory);
        
        /// <summary>
        /// 활성 실험 목록 반환
        /// </summary>
        public List<ExperimentSession> GetActiveExperiments() => activeExperiments.Values.ToList();
        
        /// <summary>
        /// 특정 트리의 성능 히스토리 반환
        /// </summary>
        public List<PerformanceMetric> GetPerformanceHistory(string treeName, int count = -1)
        {
            if (!performanceHistory.ContainsKey(treeName))
            {
                return new List<PerformanceMetric>();
            }
            
            var history = performanceHistory[treeName];
            if (count <= 0 || count > history.Count)
            {
                return new List<PerformanceMetric>(history);
            }
            
            return history.TakeLast(count).ToList();
        }
        
        /// <summary>
        /// ML 리포트 생성
        /// </summary>
        public string GenerateMLReport()
        {
            var report = new StringBuilder();
            
            report.AppendLine("=== ML 최적화 리포트 ===");
            report.AppendLine($"등록된 트리: {treeDatasets.Count}개");
            report.AppendLine($"총 데이터 포인트: {treeDatasets.Values.Sum(d => d.dataPoints.Count)}개");
            report.AppendLine($"완료된 최적화: {optimizationHistory.Count}개");
            report.AppendLine($"활성 실험: {activeExperiments.Count}개");
            report.AppendLine();
            
            // 트리별 상태
            foreach (var kvp in treeDatasets)
            {
                var treeName = kvp.Key;
                var dataset = kvp.Value;
                
                report.AppendLine($"=== {treeName} ===");
                report.AppendLine($"데이터 포인트: {dataset.dataPoints.Count}개");
                
                if (performanceHistory.ContainsKey(treeName) && performanceHistory[treeName].Count > 0)
                {
                    var latest = performanceHistory[treeName].Last();
                    report.AppendLine($"최신 성능: 효율성 {latest.efficiency:F4}, 성공률 {latest.successRate:P1}");
                }
                
                report.AppendLine();
            }
            
            // 최적화 결과
            if (optimizationHistory.Count > 0)
            {
                report.AppendLine("=== 최신 최적화 결과 ===");
                var latest = optimizationHistory.Last();
                report.AppendLine($"트리: {latest.treeName}");
                report.AppendLine($"알고리즘: {latest.algorithm}");
                report.AppendLine($"개선율: {latest.improvementPercentage:F1}%");
                report.AppendLine($"반복 횟수: {latest.iterations}");
            }
            
            return report.ToString();
        }
        
        #endregion
    }
    
    #region 데이터 구조체들
    
    [System.Serializable]
    public class MLDataset
    {
        public string treeName;
        public List<MLDataPoint> dataPoints = new List<MLDataPoint>();
        public System.DateTime createdTime;
        
        public MLDataset(string name)
        {
            treeName = name;
            createdTime = System.DateTime.Now;
        }
        
        public void AddDataPoint(List<float> parameters, PerformanceMetric metric)
        {
            dataPoints.Add(new MLDataPoint
            {
                parameters = parameters,
                performanceMetric = metric,
                timestamp = Time.time
            });
        }
    }
    
    [System.Serializable]
    public class MLDataPoint
    {
        public List<float> parameters;
        public PerformanceMetric performanceMetric;
        public float timestamp;
    }
    
    [System.Serializable]
    public struct PerformanceMetric
    {
        public float timestamp;
        public float successRate;
        public float executionTime;
        public float memoryUsage;
        public float fps;
        public float efficiency;
    }
    
    [System.Serializable]
    public class ParameterSpace
    {
        public string treeName;
        public List<ParameterVector> parameterVectors = new List<ParameterVector>();
        
        public ParameterSpace(string name)
        {
            treeName = name;
        }
        
        public void AddParameterVector(ParameterVector vector)
        {
            parameterVectors.Add(vector);
        }
        
        public int GetTotalParameterCount()
        {
            return parameterVectors.Sum(v => v.parameterNames.Count);
        }
    }
    
    [System.Serializable]
    public class ParameterVector
    {
        public string nodeName;
        public List<string> parameterNames;
        public List<float> minValues;
        public List<float> maxValues;
        public List<bool> isInteger;
        public List<float> currentValues;
    }
    
    [System.Serializable]
    public class OptimizationResult
    {
        public string treeName;
        public OptimizationAlgorithm algorithm;
        public ObjectiveFunction objectiveFunction;
        public float initialScore;
        public float finalScore;
        public float improvementPercentage;
        public int iterations;
        public List<float> optimizedParameters;
        public System.DateTime timestamp;
    }
    
    [System.Serializable]
    public class ExperimentSession
    {
        public string testName;
        public string treeName;
        public float startTime;
        public float duration;
        public List<float> groupAParameters;
        public List<float> groupBParameters;
        public List<PerformanceMetric> groupAMetrics;
        public List<PerformanceMetric> groupBMetrics;
        public ExperimentGroup currentGroup;
        public float switchInterval;
        public float nextSwitchTime;
    }
    
    [System.Serializable]
    public class ExperimentResult
    {
        public string testName;
        public float groupAAverage;
        public float groupBAverage;
        public float groupAVariance;
        public float groupBVariance;
        public int sampleSizeA;
        public int sampleSizeB;
        public ExperimentGroup winningGroup;
        public float improvementPercentage;
        public bool isStatisticallySignificant;
    }
    
    [System.Serializable]
    public class OptimizationHistoryData
    {
        public List<OptimizationResult> results;
    }
    
    public enum OptimizationAlgorithm
    {
        RandomSearch,
        GridSearch,
        BayesianOptimization,
        GeneticAlgorithm
    }
    
    public enum ObjectiveFunction
    {
        SuccessRate,
        ExecutionSpeed,
        Efficiency,
        MemoryEfficiency,
        OverallPerformance
    }
    
    public enum ExperimentGroup
    {
        None,
        GroupA,
        GroupB
    }
    
    public enum MLDataFormat
    {
        JSON,
        CSV,
        Binary
    }
    
    #endregion
}
