using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;

namespace LJH.BT
{
    /// <summary>
    /// BT 시스템의 고급 성능 분석기
    /// 통계적 분석, 성능 회귀 탐지, 최적화 우선순위 제안, 시각화 데이터 준비 기능 제공
    /// 다른 메타데이터 시스템들과 연동하여 종합적인 성능 인사이트 제공
    /// </summary>
    public class PerformanceAnalyzer : MonoBehaviour
    {
        [Header("분석 설정")]
        [SerializeField] private bool enableAnalysis = true;
        [SerializeField] private float analysisInterval = 60f; // 분석 주기 (초)
        [SerializeField] private int minimumDataPoints = 10; // 분석에 필요한 최소 데이터 포인트
        [SerializeField] private int analysisWindowSize = 100; // 분석 윈도우 크기
        
        [Header("통계 분석 설정")]
        [SerializeField] private float confidenceLevel = 0.95f; // 신뢰구간 (95%)
        [SerializeField] private int movingAverageWindow = 20; // 이동평균 윈도우
        [SerializeField] private float outlierThreshold = 2.5f; // 이상치 임계값 (표준편차 배수)
        [SerializeField] private int seasonalityPeriod = 24; // 계절성 주기 (시간)
        
        [Header("회귀 탐지 설정")]
        [SerializeField] private bool enableRegressionDetection = true;
        [SerializeField] private float regressionThreshold = 0.1f; // 10% 성능 저하시 회귀로 판단
        [SerializeField] private int regressionLookbackPeriod = 50; // 회귀 탐지 기간
        [SerializeField] private float regressionSensitivity = 0.8f; // 회귀 탐지 민감도
        
        [Header("최적화 분석 설정")]
        [SerializeField] private bool enableOptimizationAnalysis = true;
        [SerializeField] private int maxOptimizationSuggestions = 10;
        #pragma warning disable 0414
        [SerializeField] private float impactThreshold = 0.05f; // 5% 이상 영향도시 제안
        #pragma warning restore 0414
        [SerializeField] private OptimizationStrategy defaultStrategy = OptimizationStrategy.BalancedApproach;
        
        [Header("시각화 설정")]
        [SerializeField] private bool enableVisualizationData = true;
        [SerializeField] private int chartDataPoints = 50; // 차트용 데이터 포인트 수
        #pragma warning disable 0414
        [SerializeField] private VisualizationResolution resolution = VisualizationResolution.Medium;
        #pragma warning restore 0414
        
        // 의존성 컴포넌트들
        private BTTreeStatistics treeStatistics;
        private PerformanceMonitor performanceMonitor;
        private MLOptimizationManager mlOptimizationManager;
        private DataPersistenceManager dataPersistenceManager;
        
        // 분석 결과 저장
        private Dictionary<string, StatisticalAnalysisResult> statisticalResults = new Dictionary<string, StatisticalAnalysisResult>();
        private Dictionary<string, RegressionAnalysisResult> regressionResults = new Dictionary<string, RegressionAnalysisResult>();
        private Dictionary<string, OptimizationRecommendation> optimizationRecommendations = new Dictionary<string, OptimizationRecommendation>();
        private Dictionary<string, VisualizationDataSet> visualizationData = new Dictionary<string, VisualizationDataSet>();
        
        // 분석 상태
        private float lastAnalysisTime = 0f;
        private bool isAnalyzing = false;
        private List<AnalysisTask> pendingTasks = new List<AnalysisTask>();
        
        // 이벤트 시스템
        public System.Action<string, StatisticalAnalysisResult> OnStatisticalAnalysisCompleted;
        public System.Action<string, RegressionDetectionResult> OnRegressionDetected;
        public System.Action<string, OptimizationRecommendation> OnOptimizationRecommendationReady;
        public System.Action<string, VisualizationDataSet> OnVisualizationDataUpdated;
        public System.Action<AnalysisReport> OnAnalysisReportGenerated;
        
        #region Unity 생명주기
        
        private void Start()
        {
            InitializeAnalyzer();
        }
        
        private void Update()
        {
            if (!enableAnalysis) return;
            
            // 주기적 분석
            if (Time.time - lastAnalysisTime >= analysisInterval)
            {
                PerformAnalysisCycle();
                lastAnalysisTime = Time.time;
            }
            
            // 대기 중인 분석 작업 처리
            ProcessPendingTasks();
        }
        
        #endregion
        
        #region 초기화
        
        /// <summary>
        /// 분석기 초기화
        /// </summary>
        private void InitializeAnalyzer()
        {
            // 의존성 컴포넌트 찾기
            treeStatistics = FindFirstObjectByType<BTTreeStatistics>();
            performanceMonitor = FindFirstObjectByType<PerformanceMonitor>();
            mlOptimizationManager = FindFirstObjectByType<MLOptimizationManager>();
            dataPersistenceManager = FindFirstObjectByType<DataPersistenceManager>();
            
            // 이벤트 구독
            if (performanceMonitor != null)
            {
                performanceMonitor.OnPerformanceAlert += HandlePerformanceAlert;
            }
            
            if (mlOptimizationManager != null)
            {
                mlOptimizationManager.OnOptimizationCompleted += HandleOptimizationCompleted;
            }
            
            Debug.Log("[PerformanceAnalyzer] 성능 분석기가 초기화되었습니다.");
        }
        
        #endregion
        
        #region 분석 사이클
        
        /// <summary>
        /// 분석 사이클 수행
        /// </summary>
        private void PerformAnalysisCycle()
        {
            if (isAnalyzing) return;
            
            isAnalyzing = true;
            
            try
            {
                // 등록된 모든 트리에 대해 분석 수행
                var registeredTrees = GetRegisteredTrees();
                
                foreach (var treeName in registeredTrees)
                {
                    PerformTreeAnalysis(treeName);
                }
                
                // 종합 분석 리포트 생성
                GenerateAnalysisReport();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PerformanceAnalyzer] 분석 사이클 중 오류: {ex.Message}");
            }
            finally
            {
                isAnalyzing = false;
            }
        }
        
        /// <summary>
        /// 개별 트리 분석
        /// </summary>
        private void PerformTreeAnalysis(string treeName)
        {
            // 데이터 수집
            var performanceData = CollectPerformanceData(treeName);
            if (performanceData.Count < minimumDataPoints)
            {
                return; // 충분한 데이터가 없으면 스킵
            }
            
            // 1. 통계적 분석
            var statisticalResult = PerformStatisticalAnalysis(treeName, performanceData);
            statisticalResults[treeName] = statisticalResult;
            OnStatisticalAnalysisCompleted?.Invoke(treeName, statisticalResult);
            
            // 2. 회귀 분석
            if (enableRegressionDetection)
            {
                var regressionResult = PerformRegressionAnalysis(treeName, performanceData);
                regressionResults[treeName] = regressionResult;
                
                if (regressionResult.hasRegression)
                {
                    OnRegressionDetected?.Invoke(treeName, regressionResult.detectionResult);
                }
            }
            
            // 3. 최적화 분석
            if (enableOptimizationAnalysis)
            {
                var optimizationRecommendation = GenerateOptimizationRecommendation(treeName, statisticalResult);
                optimizationRecommendations[treeName] = optimizationRecommendation;
                OnOptimizationRecommendationReady?.Invoke(treeName, optimizationRecommendation);
            }
            
            // 4. 시각화 데이터 준비
            if (enableVisualizationData)
            {
                var visualizationDataSet = PrepareVisualizationData(treeName, performanceData, statisticalResult);
                visualizationData[treeName] = visualizationDataSet;
                OnVisualizationDataUpdated?.Invoke(treeName, visualizationDataSet);
            }
        }
        
        /// <summary>
        /// 대기 중인 분석 작업 처리
        /// </summary>
        private void ProcessPendingTasks()
        {
            if (pendingTasks.Count == 0) return;
            
            var task = pendingTasks[0];
            pendingTasks.RemoveAt(0);
            
            switch (task.taskType)
            {
                case AnalysisTaskType.StatisticalAnalysis:
                    PerformTreeAnalysis(task.treeName);
                    break;
                case AnalysisTaskType.RegressionDetection:
                    // 특정 회귀 분석 수행
                    break;
                case AnalysisTaskType.OptimizationAnalysis:
                    // 특정 최적화 분석 수행
                    break;
            }
        }
        
        #endregion
        
        #region 데이터 수집
        
        /// <summary>
        /// 성능 데이터 수집
        /// </summary>
        private List<PerformanceDataPoint> CollectPerformanceData(string treeName)
        {
            var dataPoints = new List<PerformanceDataPoint>();
            
            // PerformanceMonitor에서 데이터 수집
            if (performanceMonitor != null)
            {
                var history = performanceMonitor.GetPerformanceHistory(analysisWindowSize);
                foreach (var snapshot in history)
                {
                    dataPoints.Add(new PerformanceDataPoint
                    {
                        timestamp = snapshot.timestamp,
                        successRate = snapshot.averageSuccessRate,
                        executionTime = snapshot.frameTime,
                        memoryUsage = snapshot.memoryUsage,
                        fps = snapshot.fps
                    });
                }
            }
            
            // MLOptimizationManager에서 데이터 수집
            if (mlOptimizationManager != null)
            {
                var mlHistory = mlOptimizationManager.GetPerformanceHistory(treeName, analysisWindowSize);
                foreach (var metric in mlHistory)
                {
                    // 기존 데이터와 병합 또는 추가
                    var existingPoint = dataPoints.FirstOrDefault(dp => Math.Abs(dp.timestamp - metric.timestamp) < 1f);
                    if (existingPoint.Equals(default(PerformanceDataPoint)))
                    {
                        dataPoints.Add(new PerformanceDataPoint
                        {
                            timestamp = metric.timestamp,
                            successRate = metric.successRate,
                            executionTime = metric.executionTime,
                            memoryUsage = metric.memoryUsage,
                            fps = metric.fps,
                            efficiency = metric.efficiency
                        });
                    }
                }
            }
            
            // DataPersistenceManager에서 트렌드 데이터 수집
            if (dataPersistenceManager != null)
            {
                var trendAnalysis = dataPersistenceManager.GetTrendAnalysis(treeName, 7); // 최근 7일
                if (trendAnalysis.hasData)
                {
                    // 트렌드 데이터를 성능 데이터로 변환
                    // 실제 구현에서는 더 정교한 데이터 매핑 필요
                }
            }
            
            return dataPoints.OrderBy(dp => dp.timestamp).ToList();
        }
        
        /// <summary>
        /// 등록된 트리 목록 반환
        /// </summary>
        private List<string> GetRegisteredTrees()
        {
            var treeNames = new List<string>();
            
            // 각 시스템에서 등록된 트리 목록 수집
            if (dataPersistenceManager != null)
            {
                var stats = dataPersistenceManager.GetDataStatistics();
                treeNames.AddRange(stats.treeNames);
            }
            
            // 중복 제거
            return treeNames.Distinct().ToList();
        }
        
        #endregion
        
        #region 통계적 분석
        
        /// <summary>
        /// 통계적 분석 수행
        /// </summary>
        private StatisticalAnalysisResult PerformStatisticalAnalysis(string treeName, List<PerformanceDataPoint> data)
        {
            var result = new StatisticalAnalysisResult
            {
                treeName = treeName,
                analysisTime = DateTime.Now,
                dataPointCount = data.Count
            };
            
            if (data.Count == 0)
            {
                return result;
            }
            
            // 기본 통계량 계산
            result.successRateStats = CalculateBasicStatistics(data.Select(d => d.successRate).ToList());
            result.executionTimeStats = CalculateBasicStatistics(data.Select(d => d.executionTime).ToList());
            result.memoryUsageStats = CalculateBasicStatistics(data.Select(d => d.memoryUsage).ToList());
            result.fpsStats = CalculateBasicStatistics(data.Select(d => d.fps).ToList());
            result.efficiencyStats = CalculateBasicStatistics(data.Select(d => d.efficiency).ToList());
            
            // 신뢰구간 계산
            result.successRateConfidenceInterval = CalculateConfidenceInterval(data.Select(d => d.successRate).ToList());
            result.executionTimeConfidenceInterval = CalculateConfidenceInterval(data.Select(d => d.executionTime).ToList());
            
            // 이동평균 계산
            result.successRateMovingAverage = CalculateMovingAverage(data.Select(d => d.successRate).ToList(), movingAverageWindow);
            result.executionTimeMovingAverage = CalculateMovingAverage(data.Select(d => d.executionTime).ToList(), movingAverageWindow);
            
            // 상관관계 분석
            result.correlationMatrix = CalculateCorrelationMatrix(data);
            
            // 분포 분석
            result.distributionAnalysis = AnalyzeDistribution(data);
            
            // 이상치 탐지
            result.outliers = DetectOutliers(data);
            
            // 계절성 분석
            result.seasonalityAnalysis = AnalyzeSeasonality(data);
            
            // 트렌드 분석
            result.trendAnalysis = AnalyzeTrend(data);
            
            return result;
        }
        
        /// <summary>
        /// 기본 통계량 계산
        /// </summary>
        private BasicStatistics CalculateBasicStatistics(List<float> values)
        {
            if (values.Count == 0)
            {
                return new BasicStatistics();
            }
            
            var sortedValues = values.OrderBy(v => v).ToList();
            
            return new BasicStatistics
            {
                count = values.Count,
                mean = values.Average(),
                median = CalculateMedian(sortedValues),
                mode = CalculateMode(values),
                standardDeviation = CalculateStandardDeviation(values),
                variance = CalculateVariance(values),
                minimum = values.Min(),
                maximum = values.Max(),
                range = values.Max() - values.Min(),
                quartile1 = CalculatePercentile(sortedValues, 0.25f),
                quartile3 = CalculatePercentile(sortedValues, 0.75f),
                interquartileRange = CalculatePercentile(sortedValues, 0.75f) - CalculatePercentile(sortedValues, 0.25f),
                skewness = CalculateSkewness(values),
                kurtosis = CalculateKurtosis(values)
            };
        }
        
        /// <summary>
        /// 중앙값 계산
        /// </summary>
        private float CalculateMedian(List<float> sortedValues)
        {
            if (sortedValues.Count == 0) return 0f;
            
            int middle = sortedValues.Count / 2;
            if (sortedValues.Count % 2 == 0)
            {
                return (sortedValues[middle - 1] + sortedValues[middle]) / 2f;
            }
            return sortedValues[middle];
        }
        
        /// <summary>
        /// 최빈값 계산
        /// </summary>
        private float CalculateMode(List<float> values)
        {
            if (values.Count == 0) return 0f;
            
            var frequency = values.GroupBy(v => Math.Round(v, 2))
                                 .ToDictionary(g => g.Key, g => g.Count());
            
            var maxFreq = frequency.Values.Max();
            var mode = frequency.Where(kvp => kvp.Value == maxFreq).First().Key;
            
            return (float)mode;
        }
        
        /// <summary>
        /// 표준편차 계산
        /// </summary>
        private float CalculateStandardDeviation(List<float> values)
        {
            if (values.Count <= 1) return 0f;
            
            var variance = CalculateVariance(values);
            return Mathf.Sqrt(variance);
        }
        
        /// <summary>
        /// 분산 계산
        /// </summary>
        private float CalculateVariance(List<float> values)
        {
            if (values.Count <= 1) return 0f;
            
            var mean = values.Average();
            var sumSquaredDifferences = values.Sum(v => (v - mean) * (v - mean));
            return sumSquaredDifferences / (values.Count - 1);
        }
        
        /// <summary>
        /// 백분위수 계산
        /// </summary>
        private float CalculatePercentile(List<float> sortedValues, float percentile)
        {
            if (sortedValues.Count == 0) return 0f;
            
            var index = percentile * (sortedValues.Count - 1);
            var lower = Mathf.FloorToInt(index);
            var upper = Mathf.CeilToInt(index);
            
            if (lower == upper)
            {
                return sortedValues[lower];
            }
            
            var weight = index - lower;
            return sortedValues[lower] * (1 - weight) + sortedValues[upper] * weight;
        }
        
        /// <summary>
        /// 왜도 계산
        /// </summary>
        private float CalculateSkewness(List<float> values)
        {
            if (values.Count < 3) return 0f;
            
            var mean = values.Average();
            var stdDev = CalculateStandardDeviation(values);
            
            if (stdDev == 0) return 0f;
            
            var sumCubedDeviations = values.Sum(v => Math.Pow((v - mean) / stdDev, 3));
            return (float)(sumCubedDeviations / values.Count);
        }
        
        /// <summary>
        /// 첨도 계산
        /// </summary>
        private float CalculateKurtosis(List<float> values)
        {
            if (values.Count < 4) return 0f;
            
            var mean = values.Average();
            var stdDev = CalculateStandardDeviation(values);
            
            if (stdDev == 0) return 0f;
            
            var sumQuartedDeviations = values.Sum(v => Math.Pow((v - mean) / stdDev, 4));
            return (float)(sumQuartedDeviations / values.Count - 3); // 정규분포의 첨도 3을 빼서 excess kurtosis 계산
        }
        
        /// <summary>
        /// 신뢰구간 계산
        /// </summary>
        private ConfidenceInterval CalculateConfidenceInterval(List<float> values)
        {
            if (values.Count < 2)
            {
                return new ConfidenceInterval();
            }
            
            var mean = values.Average();
            var stdDev = CalculateStandardDeviation(values);
            var n = values.Count;
            
            // t-분포 임계값 (간단화를 위해 정규분포 근사 사용)
            var alpha = 1 - confidenceLevel;
            var tValue = GetTValue(alpha / 2, n - 1);
            
            var marginOfError = tValue * stdDev / Mathf.Sqrt(n);
            
            return new ConfidenceInterval
            {
                mean = mean,
                lowerBound = mean - marginOfError,
                upperBound = mean + marginOfError,
                confidenceLevel = confidenceLevel,
                marginOfError = marginOfError
            };
        }
        
        /// <summary>
        /// t-분포 임계값 근사 계산
        /// </summary>
        private float GetTValue(float alpha, int degreesOfFreedom)
        {
            // 간단화를 위한 근사값 (실제로는 t-분포표 사용)
            if (degreesOfFreedom >= 30)
            {
                return 1.96f; // 정규분포 95% 임계값
            }
            else if (degreesOfFreedom >= 10)
            {
                return 2.2f;
            }
            else
            {
                return 2.5f;
            }
        }
        
        /// <summary>
        /// 이동평균 계산
        /// </summary>
        private List<float> CalculateMovingAverage(List<float> values, int windowSize)
        {
            var movingAverage = new List<float>();
            
            for (int i = 0; i < values.Count; i++)
            {
                int start = Math.Max(0, i - windowSize + 1);
                int count = i - start + 1;
                
                float average = values.Skip(start).Take(count).Average();
                movingAverage.Add(average);
            }
            
            return movingAverage;
        }
        
        /// <summary>
        /// 상관관계 매트릭스 계산
        /// </summary>
        private CorrelationMatrix CalculateCorrelationMatrix(List<PerformanceDataPoint> data)
        {
            if (data.Count < 2)
            {
                return new CorrelationMatrix();
            }
            
            var successRates = data.Select(d => d.successRate).ToList();
            var executionTimes = data.Select(d => d.executionTime).ToList();
            var memoryUsages = data.Select(d => d.memoryUsage).ToList();
            var fpsValues = data.Select(d => d.fps).ToList();
            var efficiencies = data.Select(d => d.efficiency).ToList();
            
            return new CorrelationMatrix
            {
                successRateVsExecutionTime = CalculateCorrelation(successRates, executionTimes),
                successRateVsMemoryUsage = CalculateCorrelation(successRates, memoryUsages),
                successRateVsFps = CalculateCorrelation(successRates, fpsValues),
                executionTimeVsMemoryUsage = CalculateCorrelation(executionTimes, memoryUsages),
                executionTimeVsFps = CalculateCorrelation(executionTimes, fpsValues),
                memoryUsageVsFps = CalculateCorrelation(memoryUsages, fpsValues),
                efficiencyVsSuccessRate = CalculateCorrelation(efficiencies, successRates),
                efficiencyVsExecutionTime = CalculateCorrelation(efficiencies, executionTimes)
            };
        }
        
        /// <summary>
        /// 피어슨 상관계수 계산
        /// </summary>
        private float CalculateCorrelation(List<float> x, List<float> y)
        {
            if (x.Count != y.Count || x.Count < 2) return 0f;
            
            var meanX = x.Average();
            var meanY = y.Average();
            
            var numerator = x.Zip(y, (xi, yi) => (xi - meanX) * (yi - meanY)).Sum();
            var denomX = Math.Sqrt(x.Sum(xi => (xi - meanX) * (xi - meanX)));
            var denomY = Math.Sqrt(y.Sum(yi => (yi - meanY) * (yi - meanY)));
            
            var denominator = denomX * denomY;
            
            return denominator > 0 ? (float)(numerator / denominator) : 0f;
        }
        
        /// <summary>
        /// 분포 분석
        /// </summary>
        private DistributionAnalysis AnalyzeDistribution(List<PerformanceDataPoint> data)
        {
            if (data.Count == 0)
            {
                return new DistributionAnalysis();
            }
            
            var successRates = data.Select(d => d.successRate).ToList();
            
            return new DistributionAnalysis
            {
                distributionType = DetermineDistributionType(successRates),
                isNormal = IsNormalDistribution(successRates),
                histogram = CreateHistogram(successRates, 10), // 10개 구간
                normalityTestPValue = PerformNormalityTest(successRates)
            };
        }
        
        /// <summary>
        /// 분포 타입 결정
        /// </summary>
        private DistributionType DetermineDistributionType(List<float> values)
        {
            var stats = CalculateBasicStatistics(values);
            
            // 간단한 휴리스틱을 사용한 분포 타입 판정
            if (Math.Abs(stats.skewness) < 0.5f && Math.Abs(stats.kurtosis) < 0.5f)
            {
                return DistributionType.Normal;
            }
            else if (stats.skewness > 1f)
            {
                return DistributionType.RightSkewed;
            }
            else if (stats.skewness < -1f)
            {
                return DistributionType.LeftSkewed;
            }
            else if (stats.kurtosis > 2f)
            {
                return DistributionType.Leptokurtic; // 뾰족한 분포
            }
            else if (stats.kurtosis < -1f)
            {
                return DistributionType.Platykurtic; // 평평한 분포
            }
            
            return DistributionType.Unknown;
        }
        
        /// <summary>
        /// 정규분포 여부 판정
        /// </summary>
        private bool IsNormalDistribution(List<float> values)
        {
            var pValue = PerformNormalityTest(values);
            return pValue > 0.05f; // 5% 유의수준
        }
        
        /// <summary>
        /// 정규성 검정 (샤피로-윌크 테스트 간소화 버전)
        /// </summary>
        private float PerformNormalityTest(List<float> values)
        {
            if (values.Count < 3) return 1f;
            
            // 간소화된 정규성 검정 (실제로는 복잡한 수학적 계산 필요)
            var stats = CalculateBasicStatistics(values);
            
            // 왜도와 첨도를 기반으로 한 간단한 정규성 점수
            var skewnessScore = Math.Abs(stats.skewness);
            var kurtosisScore = Math.Abs(stats.kurtosis);
            
            var normalityScore = 1f - (skewnessScore + kurtosisScore) / 4f;
            return Math.Max(0f, Math.Min(1f, normalityScore));
        }
        
        /// <summary>
        /// 히스토그램 생성
        /// </summary>
        private List<HistogramBin> CreateHistogram(List<float> values, int binCount)
        {
            if (values.Count == 0) return new List<HistogramBin>();
            
            var min = values.Min();
            var max = values.Max();
            var binWidth = (max - min) / binCount;
            
            var bins = new List<HistogramBin>();
            
            for (int i = 0; i < binCount; i++)
            {
                var binMin = min + i * binWidth;
                var binMax = min + (i + 1) * binWidth;
                var count = values.Count(v => v >= binMin && (i == binCount - 1 ? v <= binMax : v < binMax));
                
                bins.Add(new HistogramBin
                {
                    lowerBound = binMin,
                    upperBound = binMax,
                    count = count,
                    frequency = (float)count / values.Count,
                    midpoint = (binMin + binMax) / 2f
                });
            }
            
            return bins;
        }
        
        /// <summary>
        /// 이상치 탐지
        /// </summary>
        private List<OutlierInfo> DetectOutliers(List<PerformanceDataPoint> data)
        {
            var outliers = new List<OutlierInfo>();
            
            if (data.Count < 3) return outliers;
            
            // IQR 방법을 사용한 이상치 탐지
            var successRates = data.Select(d => d.successRate).OrderBy(v => v).ToList();
            var q1 = CalculatePercentile(successRates, 0.25f);
            var q3 = CalculatePercentile(successRates, 0.75f);
            var iqr = q3 - q1;
            var lowerBound = q1 - 1.5f * iqr;
            var upperBound = q3 + 1.5f * iqr;
            
            for (int i = 0; i < data.Count; i++)
            {
                var point = data[i];
                if (point.successRate < lowerBound || point.successRate > upperBound)
                {
                    outliers.Add(new OutlierInfo
                    {
                        dataPointIndex = i,
                        timestamp = point.timestamp,
                        value = point.successRate,
                        metric = "SuccessRate",
                        outlierType = point.successRate < lowerBound ? OutlierType.Low : OutlierType.High,
                        severity = CalculateOutlierSeverity(point.successRate, lowerBound, upperBound)
                    });
                }
            }
            
            return outliers;
        }
        
        /// <summary>
        /// 이상치 심각도 계산
        /// </summary>
        private float CalculateOutlierSeverity(float value, float lowerBound, float upperBound)
        {
            // outlierThreshold 필드 사용
            float adjustedThreshold = outlierThreshold;
            
            if (value < lowerBound)
            {
                return (lowerBound - value) / Math.Abs(lowerBound) * adjustedThreshold;
            }
            else if (value > upperBound)
            {
                return (value - upperBound) / upperBound * adjustedThreshold;
            }
            return 0f;
        }
        
        /// <summary>
        /// 계절성 분석
        /// </summary>
        private SeasonalityAnalysis AnalyzeSeasonality(List<PerformanceDataPoint> data)
        {
            if (data.Count < seasonalityPeriod * 2)
            {
                return new SeasonalityAnalysis { hasSeasonality = false };
            }
            
            // 자기상관함수를 사용한 계절성 탐지 (간소화 버전)
            var values = data.Select(d => d.successRate).ToList();
            var autocorrelations = CalculateAutocorrelations(values, seasonalityPeriod);
            
            var maxAutocorr = autocorrelations.Max();
            var hasSeasonality = maxAutocorr > 0.3f; // 임계값
            
            return new SeasonalityAnalysis
            {
                hasSeasonality = hasSeasonality,
                period = hasSeasonality ? Array.IndexOf(autocorrelations, maxAutocorr) + 1 : 0,
                strength = maxAutocorr,
                autocorrelations = autocorrelations.ToList()
            };
        }
        
        /// <summary>
        /// 자기상관 계산
        /// </summary>
        private float[] CalculateAutocorrelations(List<float> values, int maxLag)
        {
            var autocorr = new float[maxLag];
            var mean = values.Average();
            
            for (int lag = 1; lag <= maxLag; lag++)
            {
                float numerator = 0f;
                float denominator = 0f;
                
                for (int i = 0; i < values.Count - lag; i++)
                {
                    numerator += (values[i] - mean) * (values[i + lag] - mean);
                }
                
                for (int i = 0; i < values.Count; i++)
                {
                    denominator += (values[i] - mean) * (values[i] - mean);
                }
                
                autocorr[lag - 1] = denominator > 0 ? numerator / denominator : 0f;
            }
            
            return autocorr;
        }
        
        /// <summary>
        /// 트렌드 분석
        /// </summary>
        private TrendAnalysis AnalyzeTrend(List<PerformanceDataPoint> data)
        {
            if (data.Count < 3)
            {
                return new TrendAnalysis();
            }
            
            // 선형 회귀를 사용한 트렌드 분석
            var x = Enumerable.Range(0, data.Count).Select(i => (double)i).ToList();
            var y = data.Select(d => (double)d.successRate).ToList();
            
            var slope = CalculateLinearRegressionSlope(x, y);
            var rSquared = CalculateRSquared(x, y, slope);
            
            return new TrendAnalysis
            {
                trendDirection = slope > 0.001 ? TrendDirection.Increasing : 
                               slope < -0.001 ? TrendDirection.Decreasing : TrendDirection.Stable,
                slope = (float)slope,
                rSquared = (float)rSquared,
                trendStrength = Math.Abs((float)slope),
                isSignificant = rSquared > 0.5 // R² > 0.5를 유의한 트렌드로 판정
            };
        }
        
        /// <summary>
        /// 선형 회귀 기울기 계산
        /// </summary>
        private double CalculateLinearRegressionSlope(List<double> x, List<double> y)
        {
            var n = x.Count;
            var sumX = x.Sum();
            var sumY = y.Sum();
            var sumXY = x.Zip(y, (xi, yi) => xi * yi).Sum();
            var sumX2 = x.Sum(xi => xi * xi);
            
            var denominator = n * sumX2 - sumX * sumX;
            if (Math.Abs(denominator) < 1e-10) return 0;
            
            return (n * sumXY - sumX * sumY) / denominator;
        }
        
        /// <summary>
        /// R² 계산
        /// </summary>
        private double CalculateRSquared(List<double> x, List<double> y, double slope)
        {
            var meanY = y.Average();
            var meanX = x.Average();
            var intercept = meanY - slope * meanX;
            
            var totalSumSquares = y.Sum(yi => (yi - meanY) * (yi - meanY));
            var residualSumSquares = x.Zip(y, (xi, yi) => {
                var predicted = slope * xi + intercept;
                return (yi - predicted) * (yi - predicted);
            }).Sum();
            
            return totalSumSquares > 0 ? 1 - (residualSumSquares / totalSumSquares) : 0;
        }
        
        #endregion
        
        #region 회귀 분석
        
        /// <summary>
        /// 회귀 분석 수행
        /// </summary>
        private RegressionAnalysisResult PerformRegressionAnalysis(string treeName, List<PerformanceDataPoint> data)
        {
            var result = new RegressionAnalysisResult
            {
                treeName = treeName,
                analysisTime = DateTime.Now,
                hasRegression = false
            };
            
            if (data.Count < regressionLookbackPeriod)
            {
                return result;
            }
            
            // 최근 데이터와 이전 데이터 비교
            var recentData = data.TakeLast(regressionLookbackPeriod / 2).ToList();
            var previousData = data.Skip(data.Count - regressionLookbackPeriod)
                                  .Take(regressionLookbackPeriod / 2).ToList();
            
            if (recentData.Count == 0 || previousData.Count == 0)
            {
                return result;
            }
            
            // 성능 메트릭별 회귀 탐지
            var regressionDetection = new RegressionDetectionResult
            {
                treeName = treeName,
                detectionTime = DateTime.Now,
                regressions = new List<MetricRegression>()
            };
            
            // 성공률 회귀 탐지
            DetectMetricRegression(recentData, previousData, "SuccessRate", 
                d => d.successRate, regressionDetection.regressions);
            
            // 실행 시간 회귀 탐지 (실행 시간 증가는 회귀)
            DetectMetricRegression(recentData, previousData, "ExecutionTime", 
                d => -d.executionTime, regressionDetection.regressions); // 음수로 변환하여 증가를 감소로 처리
            
            // FPS 회귀 탐지
            DetectMetricRegression(recentData, previousData, "FPS", 
                d => d.fps, regressionDetection.regressions);
            
            // 전체 회귀 상태 결정
            result.hasRegression = regressionDetection.regressions.Any(r => r.severity > regressionThreshold);
            result.detectionResult = regressionDetection;
            
            return result;
        }
        
        /// <summary>
        /// 메트릭별 회귀 탐지
        /// </summary>
        private void DetectMetricRegression(List<PerformanceDataPoint> recentData, 
                                          List<PerformanceDataPoint> previousData,
                                          string metricName,
                                          Func<PerformanceDataPoint, float> valueSelector,
                                          List<MetricRegression> regressions)
        {
            var recentMean = recentData.Select(valueSelector).Average();
            var previousMean = previousData.Select(valueSelector).Average();
            
            if (previousMean == 0) return;
            
            var changeRatio = (recentMean - previousMean) / Math.Abs(previousMean);
            
            // regressionSensitivity 필드 사용
            var adjustedThreshold = regressionThreshold * regressionSensitivity;
            
            if (changeRatio < -adjustedThreshold) // 성능 저하
            {
                var regression = new MetricRegression
                {
                    metricName = metricName,
                    previousValue = previousMean,
                    currentValue = recentMean,
                    changePercentage = changeRatio * 100f,
                    severity = Math.Abs(changeRatio),
                    detectionTime = DateTime.Now
                };
                
                // 통계적 유의성 검정
                regression.isStatisticallySignificant = PerformTTest(
                    recentData.Select(valueSelector).ToList(),
                    previousData.Select(valueSelector).ToList()
                );
                
                regressions.Add(regression);
            }
        }
        
        /// <summary>
        /// t-검정 수행
        /// </summary>
        private bool PerformTTest(List<float> sample1, List<float> sample2)
        {
            if (sample1.Count < 2 || sample2.Count < 2) return false;
            
            var mean1 = sample1.Average();
            var mean2 = sample2.Average();
            var var1 = CalculateVariance(sample1);
            var var2 = CalculateVariance(sample2);
            
            // 풀드 분산 계산
            var pooledVariance = ((sample1.Count - 1) * var1 + (sample2.Count - 1) * var2) / 
                                (sample1.Count + sample2.Count - 2);
            
            // t 통계량 계산
            var standardError = Math.Sqrt(pooledVariance * (1.0 / sample1.Count + 1.0 / sample2.Count));
            if (standardError == 0) return false;
            
            var tStatistic = Math.Abs(mean1 - mean2) / standardError;
            
            // 임계값 (자유도와 유의수준 고려한 간소화 버전)
            var criticalValue = 2.0; // 대략적인 0.05 유의수준 임계값
            
            return tStatistic > criticalValue;
        }
        
        #endregion
        
        #region 최적화 분석
        
        /// <summary>
        /// 최적화 권장사항 생성
        /// </summary>
        private OptimizationRecommendation GenerateOptimizationRecommendation(string treeName, StatisticalAnalysisResult statisticalResult)
        {
            var recommendation = new OptimizationRecommendation
            {
                treeName = treeName,
                generationTime = DateTime.Now,
                strategy = defaultStrategy,
                suggestions = new List<OptimizationSuggestion>()
            };
            
            // 성능 메트릭 기반 제안 생성
            GeneratePerformanceSuggestions(statisticalResult, recommendation.suggestions);
            
            // 통계적 분석 기반 제안 생성
            GenerateStatisticalSuggestions(statisticalResult, recommendation.suggestions);
            
            // 상관관계 기반 제안 생성
            GenerateCorrelationSuggestions(statisticalResult, recommendation.suggestions);
            
            // 우선순위 계산
            CalculateOptimizationPriorities(recommendation.suggestions);
            
            // 상위 제안만 선택
            recommendation.suggestions = recommendation.suggestions
                .OrderByDescending(s => s.priority)
                .Take(maxOptimizationSuggestions)
                .ToList();
            
            return recommendation;
        }
        
        /// <summary>
        /// 성능 기반 제안 생성
        /// </summary>
        private void GeneratePerformanceSuggestions(StatisticalAnalysisResult result, List<OptimizationSuggestion> suggestions)
        {
            // 성공률이 낮은 경우
            if (result.successRateStats.mean < 0.7f)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    type = OptimizationType.ParameterTuning,
                    title = "성공률 개선",
                    description = $"현재 성공률({result.successRateStats.mean:P1})이 낮습니다. 공격 범위나 조건 노드의 임계값을 조정해보세요.",
                    expectedImpact = (0.8f - result.successRateStats.mean) * 100f,
                    implementationEffort = ImplementationEffort.Medium,
                    affectedComponents = new List<string> { "AttackNode", "CheckHPNode", "CheckCooldownNode" }
                });
            }
            
            // 실행 시간이 긴 경우
            if (result.executionTimeStats.mean > 10f)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    type = OptimizationType.PerformanceOptimization,
                    title = "실행 시간 최적화",
                    description = $"평균 실행 시간({result.executionTimeStats.mean:F2}ms)이 높습니다. 노드 캐싱이나 조기 종료 로직을 추가해보세요.",
                    expectedImpact = (result.executionTimeStats.mean - 5f) / result.executionTimeStats.mean * 100f,
                    implementationEffort = ImplementationEffort.High,
                    affectedComponents = new List<string> { "BTNodeBase", "SequenceNode", "SelectorNode" }
                });
            }
            
            // 변동성이 큰 경우
            if (result.successRateStats.standardDeviation > 0.2f)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    type = OptimizationType.StabilityImprovement,
                    title = "성능 안정성 개선",
                    description = $"성공률의 변동성({result.successRateStats.standardDeviation:F3})이 큽니다. 조건 체크를 더 안정적으로 만들어보세요.",
                    expectedImpact = result.successRateStats.standardDeviation * 50f,
                    implementationEffort = ImplementationEffort.Medium,
                    affectedComponents = new List<string> { "DetectEnemyNode", "CheckArenaBoundaryNode" }
                });
            }
        }
        
        /// <summary>
        /// 통계적 분석 기반 제안 생성
        /// </summary>
        private void GenerateStatisticalSuggestions(StatisticalAnalysisResult result, List<OptimizationSuggestion> suggestions)
        {
            // 이상치가 많은 경우
            if (result.outliers.Count > result.dataPointCount * 0.1f) // 10% 이상이 이상치
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    type = OptimizationType.OutlierHandling,
                    title = "이상치 처리",
                    description = $"{result.outliers.Count}개의 이상치가 발견되었습니다. 예외 상황에 대한 처리 로직을 추가해보세요.",
                    expectedImpact = result.outliers.Count / (float)result.dataPointCount * 30f,
                    implementationEffort = ImplementationEffort.Low,
                    affectedComponents = new List<string> { "ErrorHandling", "ValidationLogic" }
                });
            }
            
            // 분포가 정규분포가 아닌 경우
            if (!result.distributionAnalysis.isNormal)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    type = OptimizationType.DistributionOptimization,
                    title = "성능 분포 정규화",
                    description = "성능 분포가 비정규적입니다. 파라미터 범위를 조정하여 더 예측 가능한 성능을 만들어보세요.",
                    expectedImpact = 20f,
                    implementationEffort = ImplementationEffort.Medium,
                    affectedComponents = new List<string> { "ParameterConfiguration" }
                });
            }
            
            // 트렌드 분석 기반 제안
            if (result.trendAnalysis.isSignificant)
            {
                if (result.trendAnalysis.trendDirection == TrendDirection.Decreasing)
                {
                    suggestions.Add(new OptimizationSuggestion
                    {
                        type = OptimizationType.TrendCorrection,
                        title = "성능 하락 트렌드 대응",
                        description = "성능이 지속적으로 하락하고 있습니다. 근본 원인을 파악하고 대응책을 마련해보세요.",
                        expectedImpact = Math.Abs(result.trendAnalysis.slope) * 100f,
                        implementationEffort = ImplementationEffort.High,
                        affectedComponents = new List<string> { "SystemMonitoring", "PreventiveMaintenance" }
                    });
                }
            }
        }
        
        /// <summary>
        /// 상관관계 기반 제안 생성
        /// </summary>
        private void GenerateCorrelationSuggestions(StatisticalAnalysisResult result, List<OptimizationSuggestion> suggestions)
        {
            var correlations = result.correlationMatrix;
            
            // 성공률과 실행시간의 강한 음의 상관관계
            if (correlations.successRateVsExecutionTime < -0.6f)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    type = OptimizationType.CorrelationOptimization,
                    title = "실행시간-성공률 최적화",
                    description = "실행시간과 성공률 간에 강한 음의 상관관계가 있습니다. 더 빠른 판단 로직을 구현해보세요.",
                    expectedImpact = Math.Abs(correlations.successRateVsExecutionTime) * 25f,
                    implementationEffort = ImplementationEffort.Medium,
                    affectedComponents = new List<string> { "DecisionLogic", "FastPath" }
                });
            }
            
            // 메모리 사용량과 FPS의 강한 음의 상관관계
            if (correlations.memoryUsageVsFps < -0.5f)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    type = OptimizationType.MemoryOptimization,
                    title = "메모리 사용량 최적화",
                    description = "메모리 사용량이 FPS에 부정적 영향을 미치고 있습니다. 메모리 할당을 최적화해보세요.",
                    expectedImpact = Math.Abs(correlations.memoryUsageVsFps) * 20f,
                    implementationEffort = ImplementationEffort.High,
                    affectedComponents = new List<string> { "MemoryManagement", "ObjectPooling" }
                });
            }
        }
        
        /// <summary>
        /// 최적화 우선순위 계산
        /// </summary>
        private void CalculateOptimizationPriorities(List<OptimizationSuggestion> suggestions)
        {
            foreach (var suggestion in suggestions)
            {
                // 우선순위 = (기대효과 / 구현노력) × 가중치
                float effortWeight = suggestion.implementationEffort switch
                {
                    ImplementationEffort.Low => 1.0f,
                    ImplementationEffort.Medium => 0.7f,
                    ImplementationEffort.High => 0.4f,
                    _ => 0.5f
                };
                
                float typeWeight = suggestion.type switch
                {
                    OptimizationType.PerformanceOptimization => 1.2f,
                    OptimizationType.StabilityImprovement => 1.1f,
                    OptimizationType.ParameterTuning => 1.0f,
                    _ => 0.8f
                };
                
                suggestion.priority = (suggestion.expectedImpact * effortWeight * typeWeight) / 100f;
            }
        }
        
        #endregion
        
        #region 시각화 데이터 준비
        
        /// <summary>
        /// 시각화 데이터 준비
        /// </summary>
        private VisualizationDataSet PrepareVisualizationData(string treeName, List<PerformanceDataPoint> data, StatisticalAnalysisResult stats)
        {
            var dataSet = new VisualizationDataSet
            {
                treeName = treeName,
                lastUpdated = DateTime.Now
            };
            
            // 시계열 차트 데이터
            dataSet.timeSeriesData = PrepareTimeSeriesData(data);
            
            // 히스토그램 데이터
            dataSet.histogramData = PrepareHistogramData(stats.distributionAnalysis.histogram);
            
            // 상관관계 히트맵 데이터
            dataSet.correlationHeatmapData = PrepareCorrelationHeatmapData(stats.correlationMatrix);
            
            // 박스플롯 데이터
            dataSet.boxPlotData = PrepareBoxPlotData(stats);
            
            // 트렌드 라인 데이터
            dataSet.trendLineData = PrepareTrendLineData(data, stats.trendAnalysis);
            
            // 산점도 데이터
            dataSet.scatterPlotData = PrepareScatterPlotData(data);
            
            return dataSet;
        }
        
        /// <summary>
        /// 시계열 차트 데이터 준비
        /// </summary>
        private TimeSeriesChartData PrepareTimeSeriesData(List<PerformanceDataPoint> data)
        {
            var sampledData = SampleData(data, chartDataPoints);
            
            return new TimeSeriesChartData
            {
                timestamps = sampledData.Select(d => d.timestamp).ToList(),
                successRates = sampledData.Select(d => d.successRate).ToList(),
                executionTimes = sampledData.Select(d => d.executionTime).ToList(),
                memoryUsages = sampledData.Select(d => d.memoryUsage).ToList(),
                fpsValues = sampledData.Select(d => d.fps).ToList(),
                efficiencies = sampledData.Select(d => d.efficiency).ToList()
            };
        }
        
        /// <summary>
        /// 데이터 샘플링
        /// </summary>
        private List<PerformanceDataPoint> SampleData(List<PerformanceDataPoint> data, int targetCount)
        {
            if (data.Count <= targetCount)
            {
                return data;
            }
            
            var step = (float)data.Count / targetCount;
            var sampledData = new List<PerformanceDataPoint>();
            
            for (int i = 0; i < targetCount; i++)
            {
                int index = (int)(i * step);
                if (index < data.Count)
                {
                    sampledData.Add(data[index]);
                }
            }
            
            return sampledData;
        }
        
        /// <summary>
        /// 히스토그램 데이터 준비
        /// </summary>
        private HistogramChartData PrepareHistogramData(List<HistogramBin> histogram)
        {
            return new HistogramChartData
            {
                binLabels = histogram.Select(b => $"{b.lowerBound:F2}-{b.upperBound:F2}").ToList(),
                frequencies = histogram.Select(b => b.frequency).ToList(),
                counts = histogram.Select(b => b.count).ToList()
            };
        }
        
        /// <summary>
        /// 상관관계 히트맵 데이터 준비
        /// </summary>
        private HeatmapData PrepareCorrelationHeatmapData(CorrelationMatrix correlations)
        {
            var metrics = new List<string> { "SuccessRate", "ExecutionTime", "MemoryUsage", "FPS", "Efficiency" };
            var correlationMatrix = new float[metrics.Count, metrics.Count];
            
            // 대칭 행렬 생성
            correlationMatrix[0, 0] = 1f; // SuccessRate vs SuccessRate
            correlationMatrix[0, 1] = correlations.successRateVsExecutionTime;
            correlationMatrix[0, 2] = correlations.successRateVsMemoryUsage;
            correlationMatrix[0, 3] = correlations.successRateVsFps;
            correlationMatrix[0, 4] = correlations.efficiencyVsSuccessRate;
            
            correlationMatrix[1, 0] = correlations.successRateVsExecutionTime;
            correlationMatrix[1, 1] = 1f; // ExecutionTime vs ExecutionTime
            correlationMatrix[1, 2] = correlations.executionTimeVsMemoryUsage;
            correlationMatrix[1, 3] = correlations.executionTimeVsFps;
            correlationMatrix[1, 4] = correlations.efficiencyVsExecutionTime;
            
            correlationMatrix[2, 0] = correlations.successRateVsMemoryUsage;
            correlationMatrix[2, 1] = correlations.executionTimeVsMemoryUsage;
            correlationMatrix[2, 2] = 1f; // MemoryUsage vs MemoryUsage
            correlationMatrix[2, 3] = correlations.memoryUsageVsFps;
            correlationMatrix[2, 4] = 0f; // 임시값
            
            correlationMatrix[3, 0] = correlations.successRateVsFps;
            correlationMatrix[3, 1] = correlations.executionTimeVsFps;
            correlationMatrix[3, 2] = correlations.memoryUsageVsFps;
            correlationMatrix[3, 3] = 1f; // FPS vs FPS
            correlationMatrix[3, 4] = 0f; // 임시값
            
            correlationMatrix[4, 0] = correlations.efficiencyVsSuccessRate;
            correlationMatrix[4, 1] = correlations.efficiencyVsExecutionTime;
            correlationMatrix[4, 2] = 0f; // 임시값
            correlationMatrix[4, 3] = 0f; // 임시값
            correlationMatrix[4, 4] = 1f; // Efficiency vs Efficiency
            
            return new HeatmapData
            {
                xLabels = metrics,
                yLabels = metrics,
                values = correlationMatrix,
                minValue = -1f,
                maxValue = 1f
            };
        }
        
        /// <summary>
        /// 박스플롯 데이터 준비
        /// </summary>
        private BoxPlotData PrepareBoxPlotData(StatisticalAnalysisResult stats)
        {
            return new BoxPlotData
            {
                metricNames = new List<string> { "SuccessRate", "ExecutionTime", "MemoryUsage", "FPS", "Efficiency" },
                minimums = new List<float> { stats.successRateStats.minimum, stats.executionTimeStats.minimum, 
                                           stats.memoryUsageStats.minimum, stats.fpsStats.minimum, stats.efficiencyStats.minimum },
                quartile1s = new List<float> { stats.successRateStats.quartile1, stats.executionTimeStats.quartile1, 
                                             stats.memoryUsageStats.quartile1, stats.fpsStats.quartile1, stats.efficiencyStats.quartile1 },
                medians = new List<float> { stats.successRateStats.median, stats.executionTimeStats.median, 
                                          stats.memoryUsageStats.median, stats.fpsStats.median, stats.efficiencyStats.median },
                quartile3s = new List<float> { stats.successRateStats.quartile3, stats.executionTimeStats.quartile3, 
                                             stats.memoryUsageStats.quartile3, stats.fpsStats.quartile3, stats.efficiencyStats.quartile3 },
                maximums = new List<float> { stats.successRateStats.maximum, stats.executionTimeStats.maximum, 
                                           stats.memoryUsageStats.maximum, stats.fpsStats.maximum, stats.efficiencyStats.maximum }
            };
        }
        
        /// <summary>
        /// 트렌드 라인 데이터 준비
        /// </summary>
        private TrendLineData PrepareTrendLineData(List<PerformanceDataPoint> data, TrendAnalysis trendAnalysis)
        {
            if (!trendAnalysis.isSignificant || data.Count < 2)
            {
                return new TrendLineData { hasValidTrend = false };
            }
            
            var x = Enumerable.Range(0, data.Count).Select(i => (float)i).ToList();
            var meanX = x.Average();
            var meanY = data.Select(d => d.successRate).Average();
            var intercept = meanY - trendAnalysis.slope * meanX;
            
            var trendLine = x.Select(xi => trendAnalysis.slope * xi + intercept).ToList();
            
            return new TrendLineData
            {
                hasValidTrend = true,
                xValues = x,
                trendValues = trendLine,
                slope = trendAnalysis.slope,
                rSquared = trendAnalysis.rSquared,
                direction = trendAnalysis.trendDirection
            };
        }
        
        /// <summary>
        /// 산점도 데이터 준비
        /// </summary>
        private ScatterPlotData PrepareScatterPlotData(List<PerformanceDataPoint> data)
        {
            return new ScatterPlotData
            {
                successRateVsExecutionTime = new ScatterSeries
                {
                    xValues = data.Select(d => d.successRate).ToList(),
                    yValues = data.Select(d => d.executionTime).ToList(),
                    xLabel = "Success Rate",
                    yLabel = "Execution Time (ms)"
                },
                memoryUsageVsFps = new ScatterSeries
                {
                    xValues = data.Select(d => d.memoryUsage).ToList(),
                    yValues = data.Select(d => d.fps).ToList(),
                    xLabel = "Memory Usage (MB)",
                    yLabel = "FPS"
                },
                efficiencyVsSuccessRate = new ScatterSeries
                {
                    xValues = data.Select(d => d.efficiency).ToList(),
                    yValues = data.Select(d => d.successRate).ToList(),
                    xLabel = "Efficiency",
                    yLabel = "Success Rate"
                }
            };
        }
        
        #endregion
        
        #region 이벤트 핸들러
        
        /// <summary>
        /// 성능 경고 처리
        /// </summary>
        private void HandlePerformanceAlert(PerformanceAlert alert)
        {
            // 즉시 분석 작업 스케줄링
            var task = new AnalysisTask
            {
                taskType = AnalysisTaskType.StatisticalAnalysis,
                treeName = alert.source,
                priority = alert.severity == AlertSeverity.High ? 1 : 2,
                scheduledTime = Time.time
            };
            
            pendingTasks.Add(task);
            pendingTasks.Sort((a, b) => a.priority.CompareTo(b.priority));
        }
        
        /// <summary>
        /// 최적화 완료 처리
        /// </summary>
        private void HandleOptimizationCompleted(string treeName, OptimizationResult result)
        {
            // 최적화 후 성능 분석 스케줄링
            var task = new AnalysisTask
            {
                taskType = AnalysisTaskType.StatisticalAnalysis,
                treeName = treeName,
                priority = 1,
                scheduledTime = Time.time + 60f // 1분 후 분석
            };
            
            pendingTasks.Add(task);
        }
        
        #endregion
        
        #region 리포트 생성
        
        /// <summary>
        /// 종합 분석 리포트 생성
        /// </summary>
        private void GenerateAnalysisReport()
        {
            var report = new AnalysisReport
            {
                generationTime = DateTime.Now,
                analyzedTrees = statisticalResults.Keys.ToList(),
                overallStats = CalculateOverallStatistics(),
                keyFindings = GenerateKeyFindings(),
                recommendations = GenerateTopRecommendations()
            };
            
            OnAnalysisReportGenerated?.Invoke(report);
        }
        
        /// <summary>
        /// 전체 통계 계산
        /// </summary>
        private OverallStatistics CalculateOverallStatistics()
        {
            if (statisticalResults.Count == 0)
            {
                return new OverallStatistics();
            }
            
            return new OverallStatistics
            {
                totalTreesAnalyzed = statisticalResults.Count,
                averageSuccessRate = statisticalResults.Values.Average(r => r.successRateStats.mean),
                averageExecutionTime = statisticalResults.Values.Average(r => r.executionTimeStats.mean),
                treesWithRegressions = regressionResults.Values.Count(r => r.hasRegression),
                totalOptimizationSuggestions = optimizationRecommendations.Values.Sum(r => r.suggestions.Count),
                highPriorityIssues = optimizationRecommendations.Values
                    .SelectMany(r => r.suggestions)
                    .Count(s => s.priority > 0.8f)
            };
        }
        
        /// <summary>
        /// 주요 발견사항 생성
        /// </summary>
        private List<string> GenerateKeyFindings()
        {
            var findings = new List<string>();
            
            // 성능 이슈 요약
            var lowPerformanceTrees = statisticalResults.Where(kvp => kvp.Value.successRateStats.mean < 0.6f);
            if (lowPerformanceTrees.Any())
            {
                findings.Add($"{lowPerformanceTrees.Count()}개 트리에서 성공률이 60% 미만입니다.");
            }
            
            // 회귀 발견사항
            var regressedTrees = regressionResults.Where(kvp => kvp.Value.hasRegression);
            if (regressedTrees.Any())
            {
                findings.Add($"{regressedTrees.Count()}개 트리에서 성능 회귀가 감지되었습니다.");
            }
            
            // 최적화 기회
            var highImpactSuggestions = optimizationRecommendations.Values
                .SelectMany(r => r.suggestions)
                .Where(s => s.expectedImpact > 20f);
            
            if (highImpactSuggestions.Any())
            {
                findings.Add($"{highImpactSuggestions.Count()}개의 고효과 최적화 기회가 발견되었습니다.");
            }
            
            return findings;
        }
        
        /// <summary>
        /// 상위 권장사항 생성
        /// </summary>
        private List<string> GenerateTopRecommendations()
        {
            var topRecommendations = optimizationRecommendations.Values
                .SelectMany(r => r.suggestions)
                .OrderByDescending(s => s.priority)
                .Take(5)
                .Select(s => $"{s.title}: {s.description}")
                .ToList();
            
            return topRecommendations;
        }
        
        #endregion
        
        #region 공개 API
        
        /// <summary>
        /// 특정 트리의 통계 분석 결과 반환
        /// </summary>
        public StatisticalAnalysisResult GetStatisticalAnalysis(string treeName)
        {
            return statisticalResults.ContainsKey(treeName) ? statisticalResults[treeName] : null;
        }
        
        /// <summary>
        /// 특정 트리의 회귀 분석 결과 반환
        /// </summary>
        public RegressionAnalysisResult GetRegressionAnalysis(string treeName)
        {
            return regressionResults.ContainsKey(treeName) ? regressionResults[treeName] : null;
        }
        
        /// <summary>
        /// 특정 트리의 최적화 권장사항 반환
        /// </summary>
        public OptimizationRecommendation GetOptimizationRecommendation(string treeName)
        {
            return optimizationRecommendations.ContainsKey(treeName) ? optimizationRecommendations[treeName] : null;
        }
        
        /// <summary>
        /// 특정 트리의 시각화 데이터 반환
        /// </summary>
        public VisualizationDataSet GetVisualizationData(string treeName)
        {
            return visualizationData.ContainsKey(treeName) ? visualizationData[treeName] : null;
        }
        
        /// <summary>
        /// 모든 분석 결과 요약 반환
        /// </summary>
        public AnalysisSummary GetAnalysisSummary()
        {
            return new AnalysisSummary
            {
                lastAnalysisTime = lastAnalysisTime,
                analyzedTreeCount = statisticalResults.Count,
                totalIssuesFound = regressionResults.Values.Count(r => r.hasRegression) +
                                 optimizationRecommendations.Values.SelectMany(r => r.suggestions).Count(s => s.priority > 0.5f),
                averageSystemPerformance = statisticalResults.Count > 0 ? 
                    statisticalResults.Values.Average(r => r.successRateStats.mean) : 0f
            };
        }
        
        /// <summary>
        /// 즉시 분석 수행
        /// </summary>
        public void PerformImmediateAnalysis(string treeName = null)
        {
            if (string.IsNullOrEmpty(treeName))
            {
                PerformAnalysisCycle();
            }
            else
            {
                PerformTreeAnalysis(treeName);
            }
        }
        
        #endregion
    }
    
    #region 데이터 구조체들
    
    /// <summary>
    /// 성능 데이터 포인트
    /// </summary>
    [System.Serializable]
    public struct PerformanceDataPoint
    {
        public float timestamp;
        public float successRate;
        public float executionTime;
        public float memoryUsage;
        public float fps;
        public float efficiency;
    }
    
    /// <summary>
    /// 통계 분석 결과
    /// </summary>
    [System.Serializable]
    public class StatisticalAnalysisResult
    {
        public string treeName;
        public DateTime analysisTime;
        public int dataPointCount;
        
        public BasicStatistics successRateStats;
        public BasicStatistics executionTimeStats;
        public BasicStatistics memoryUsageStats;
        public BasicStatistics fpsStats;
        public BasicStatistics efficiencyStats;
        
        public ConfidenceInterval successRateConfidenceInterval;
        public ConfidenceInterval executionTimeConfidenceInterval;
        
        public List<float> successRateMovingAverage;
        public List<float> executionTimeMovingAverage;
        
        public CorrelationMatrix correlationMatrix;
        public DistributionAnalysis distributionAnalysis;
        public List<OutlierInfo> outliers;
        public SeasonalityAnalysis seasonalityAnalysis;
        public TrendAnalysis trendAnalysis;
    }
    
    /// <summary>
    /// 기본 통계량
    /// </summary>
    [System.Serializable]
    public struct BasicStatistics
    {
        public int count;
        public float mean;
        public float median;
        public float mode;
        public float standardDeviation;
        public float variance;
        public float minimum;
        public float maximum;
        public float range;
        public float quartile1;
        public float quartile3;
        public float interquartileRange;
        public float skewness;
        public float kurtosis;
    }
    
    /// <summary>
    /// 신뢰구간
    /// </summary>
    [System.Serializable]
    public struct ConfidenceInterval
    {
        public float mean;
        public float lowerBound;
        public float upperBound;
        public float confidenceLevel;
        public float marginOfError;
    }
    
    /// <summary>
    /// 상관관계 매트릭스
    /// </summary>
    [System.Serializable]
    public struct CorrelationMatrix
    {
        public float successRateVsExecutionTime;
        public float successRateVsMemoryUsage;
        public float successRateVsFps;
        public float executionTimeVsMemoryUsage;
        public float executionTimeVsFps;
        public float memoryUsageVsFps;
        public float efficiencyVsSuccessRate;
        public float efficiencyVsExecutionTime;
    }
    
    /// <summary>
    /// 분포 분석
    /// </summary>
    [System.Serializable]
    public class DistributionAnalysis
    {
        public DistributionType distributionType;
        public bool isNormal;
        public List<HistogramBin> histogram;
        public float normalityTestPValue;
    }
    
    /// <summary>
    /// 히스토그램 구간
    /// </summary>
    [System.Serializable]
    public struct HistogramBin
    {
        public float lowerBound;
        public float upperBound;
        public int count;
        public float frequency;
        public float midpoint;
    }
    
    /// <summary>
    /// 이상치 정보
    /// </summary>
    [System.Serializable]
    public struct OutlierInfo
    {
        public int dataPointIndex;
        public float timestamp;
        public float value;
        public string metric;
        public OutlierType outlierType;
        public float severity;
    }
    
    /// <summary>
    /// 계절성 분석
    /// </summary>
    [System.Serializable]
    public struct SeasonalityAnalysis
    {
        public bool hasSeasonality;
        public int period;
        public float strength;
        public List<float> autocorrelations;
    }
    
    /// <summary>
    /// 트렌드 분석
    /// </summary>
    [System.Serializable]
    public struct TrendAnalysis
    {
        public TrendDirection trendDirection;
        public float slope;
        public float rSquared;
        public float trendStrength;
        public bool isSignificant;
    }
    
    /// <summary>
    /// 회귀 분석 결과
    /// </summary>
    [System.Serializable]
    public class RegressionAnalysisResult
    {
        public string treeName;
        public DateTime analysisTime;
        public bool hasRegression;
        public RegressionDetectionResult detectionResult;
    }
    
    /// <summary>
    /// 회귀 탐지 결과
    /// </summary>
    [System.Serializable]
    public class RegressionDetectionResult
    {
        public string treeName;
        public DateTime detectionTime;
        public List<MetricRegression> regressions;
    }
    
    /// <summary>
    /// 메트릭 회귀 정보
    /// </summary>
    [System.Serializable]
    public struct MetricRegression
    {
        public string metricName;
        public float previousValue;
        public float currentValue;
        public float changePercentage;
        public float severity;
        public bool isStatisticallySignificant;
        public DateTime detectionTime;
    }
    
    /// <summary>
    /// 최적화 권장사항
    /// </summary>
    [System.Serializable]
    public class OptimizationRecommendation
    {
        public string treeName;
        public DateTime generationTime;
        public OptimizationStrategy strategy;
        public List<OptimizationSuggestion> suggestions;
    }
    
    /// <summary>
    /// 최적화 제안
    /// </summary>
    [System.Serializable]
    public class OptimizationSuggestion
    {
        public OptimizationType type;
        public string title;
        public string description;
        public float expectedImpact;
        public ImplementationEffort implementationEffort;
        public float priority;
        public List<string> affectedComponents;
    }
    
    /// <summary>
    /// 시각화 데이터셋
    /// </summary>
    [System.Serializable]
    public class VisualizationDataSet
    {
        public string treeName;
        public DateTime lastUpdated;
        public TimeSeriesChartData timeSeriesData;
        public HistogramChartData histogramData;
        public HeatmapData correlationHeatmapData;
        public BoxPlotData boxPlotData;
        public TrendLineData trendLineData;
        public ScatterPlotData scatterPlotData;
    }
    
    /// <summary>
    /// 시계열 차트 데이터
    /// </summary>
    [System.Serializable]
    public class TimeSeriesChartData
    {
        public List<float> timestamps;
        public List<float> successRates;
        public List<float> executionTimes;
        public List<float> memoryUsages;
        public List<float> fpsValues;
        public List<float> efficiencies;
    }
    
    /// <summary>
    /// 히스토그램 차트 데이터
    /// </summary>
    [System.Serializable]
    public class HistogramChartData
    {
        public List<string> binLabels;
        public List<float> frequencies;
        public List<int> counts;
    }
    
    /// <summary>
    /// 히트맵 데이터
    /// </summary>
    [System.Serializable]
    public class HeatmapData
    {
        public List<string> xLabels;
        public List<string> yLabels;
        public float[,] values;
        public float minValue;
        public float maxValue;
    }
    
    /// <summary>
    /// 박스플롯 데이터
    /// </summary>
    [System.Serializable]
    public class BoxPlotData
    {
        public List<string> metricNames;
        public List<float> minimums;
        public List<float> quartile1s;
        public List<float> medians;
        public List<float> quartile3s;
        public List<float> maximums;
    }
    
    /// <summary>
    /// 트렌드 라인 데이터
    /// </summary>
    [System.Serializable]
    public class TrendLineData
    {
        public bool hasValidTrend;
        public List<float> xValues;
        public List<float> trendValues;
        public float slope;
        public float rSquared;
        public TrendDirection direction;
    }
    
    /// <summary>
    /// 산점도 데이터
    /// </summary>
    [System.Serializable]
    public class ScatterPlotData
    {
        public ScatterSeries successRateVsExecutionTime;
        public ScatterSeries memoryUsageVsFps;
        public ScatterSeries efficiencyVsSuccessRate;
    }
    
    /// <summary>
    /// 산점도 시리즈
    /// </summary>
    [System.Serializable]
    public class ScatterSeries
    {
        public List<float> xValues;
        public List<float> yValues;
        public string xLabel;
        public string yLabel;
    }
    
    /// <summary>
    /// 분석 작업
    /// </summary>
    [System.Serializable]
    public class AnalysisTask
    {
        public AnalysisTaskType taskType;
        public string treeName;
        public int priority;
        public float scheduledTime;
    }
    
    /// <summary>
    /// 분석 리포트
    /// </summary>
    [System.Serializable]
    public class AnalysisReport
    {
        public DateTime generationTime;
        public List<string> analyzedTrees;
        public OverallStatistics overallStats;
        public List<string> keyFindings;
        public List<string> recommendations;
    }
    
    /// <summary>
    /// 전체 통계
    /// </summary>
    [System.Serializable]
    public struct OverallStatistics
    {
        public int totalTreesAnalyzed;
        public float averageSuccessRate;
        public float averageExecutionTime;
        public int treesWithRegressions;
        public int totalOptimizationSuggestions;
        public int highPriorityIssues;
    }
    
    /// <summary>
    /// 분석 요약
    /// </summary>
    [System.Serializable]
    public struct AnalysisSummary
    {
        public float lastAnalysisTime;
        public int analyzedTreeCount;
        public int totalIssuesFound;
        public float averageSystemPerformance;
    }
    
    #region 열거형들
    
    public enum DistributionType
    {
        Normal,
        RightSkewed,
        LeftSkewed,
        Leptokurtic,
        Platykurtic,
        Unknown
    }
    
    public enum OutlierType
    {
        Low,
        High,
        Both
    }
    
    public enum TrendDirection
    {
        Increasing,
        Decreasing,
        Stable
    }
    
    public enum OptimizationType
    {
        ParameterTuning,
        PerformanceOptimization,
        StabilityImprovement,
        OutlierHandling,
        DistributionOptimization,
        TrendCorrection,
        CorrelationOptimization,
        MemoryOptimization
    }
    
    public enum ImplementationEffort
    {
        Low,
        Medium,
        High
    }
    
    public enum OptimizationStrategy
    {
        PerformanceFocused,
        StabilityFocused,
        BalancedApproach,
        QuickWins
    }
    
    public enum VisualizationResolution
    {
        Low,
        Medium,
        High
    }
    
    public enum AnalysisTaskType
    {
        StatisticalAnalysis,
        RegressionDetection,
        OptimizationAnalysis
    }
    
    #endregion
    
    #endregion
}
